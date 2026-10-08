using CafeManagement.API.DTOs;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Security.Claims;
using CafeManagement.API.Data;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[ApiController, Authorize, Route("api/[controller]")]
public class AssistantController(AssistantInsights insights, GeminiAssistant gemini, AppDbContext db) : ControllerBase {
    private async Task<ClaimsPrincipal?> LiveAccess(CancellationToken ct) {
        var username=User.FindFirstValue(ClaimTypes.NameIdentifier);
        var account=await db.Accounts.AsNoTracking().SingleOrDefaultAsync(a=>a.UserName==username,ct);
        if(account==null||!account.IsActive||account.SecurityVersion.ToString()!=(User.FindFirstValue("SecurityVersion")??"0"))return null;
        var codes=await DynamicAccess.Effective(db,account.UserName);
        return new ClaimsPrincipal(new ClaimsIdentity(codes.Select(code=>new Claim("Permission",code)),"live"));
    }
    [HttpGet("capabilities")]
    public IActionResult Capabilities() => Ok(new {configured=gemini.Configured,topics=AssistantInsights.Topics.Where(t=>AssistantInsights.Allowed(User,t)),periods=AssistantInsights.Periods});
    [HttpPost("ask"), EnableRateLimiting("assistant")]
    public async Task<IActionResult> Ask(AssistantQuestion dto,CancellationToken ct) {
        if(string.IsNullOrWhiteSpace(dto.Question))return BadRequest(new{message="Hãy nhập câu hỏi về tình hình quán."});
        AssistantPlan plan; var mode="basic"; string? warning=gemini.Configured?null:"Chưa kết nối Gemini. Đang trả lời bằng tra cứu cơ bản; có thể dùng câu hỏi gợi ý.";
        try { if(gemini.Configured){plan=await gemini.Plan(dto.Question.Trim(),ct);mode="gemini";}else plan=AssistantInsights.BasicPlan(dto.Question); }
        catch(Exception e) when(e is AssistantProviderException or JsonException) { plan=AssistantInsights.BasicPlan(dto.Question);warning=e is AssistantProviderException?e.Message:"AI chưa hiểu câu hỏi; đang dùng tra cứu cơ bản."; }
        if(!AssistantInsights.Topics.Contains(plan.Topic)||!AssistantInsights.Periods.Contains(plan.Period))return Ok(new AssistantAnswer("Hiện mình hỗ trợ doanh thu, số đơn đã thanh toán, món bán chạy và nguyên liệu sắp hết. Bạn có thể hỏi cho hôm nay, hôm qua, 7/30 ngày qua, tuần này hoặc tháng này. Mỗi câu hỏi nên nêu rõ nội dung và thời gian.",mode,warning,DateTimeOffset.UtcNow,"",[]));
        if(plan.Compare && plan.Topic is "menu" or "stock")return Ok(new AssistantAnswer("Bản này chỉ so sánh doanh thu hoặc số đơn giữa hai kỳ. Món bán chạy được tổng hợp theo kỳ đã chọn; tồn khả dụng được xem tại thời điểm hiện tại.",mode,warning,DateTimeOffset.UtcNow,"",[]));
        var access=await LiveAccess(ct);
        if(access==null)return Unauthorized();
        if(!DynamicAccess.Has(access,"AI_VIEW")||!AssistantInsights.Allowed(access,plan.Topic))return StatusCode(403,new{message="Bạn chưa được cấp quyền xem dữ liệu này. Liên hệ quản trị viên nếu cần."});
        var result=await insights.Read(plan,access,mode,warning,ct);
        if(mode=="gemini")try { result=result with {Insight=await gemini.Insight(result.Sections,ct)}; }
        catch(Exception e) when(e is AssistantProviderException or JsonException) { result=result with {Warning="Đã lấy được số liệu thật. Gemini chưa tạo được nhận xét; bạn có thể xem các kết quả bên dưới."}; }
        var latest=await LiveAccess(ct);
        if(latest==null)return Unauthorized();
        if(!latest.FindAll("Permission").Select(c=>c.Value).ToHashSet().SetEquals(access.FindAll("Permission").Select(c=>c.Value)))return Conflict(new{message="Quyền của bạn vừa thay đổi. Hãy gửi lại câu hỏi để xem dữ liệu theo quyền mới."});
        return Ok(result);
    }
}
