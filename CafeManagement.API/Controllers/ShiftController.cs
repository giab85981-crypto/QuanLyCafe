using System.Security.Claims;
using CafeManagement.API.Data;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[Authorize, ApiController, Route("api/[controller]")]
public class ShiftController(AppDbContext db) : ControllerBase
{
    string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    bool Manager => DynamicAccess.Has(User, "SHIFT_VIEW");
    [HttpGet("current")]
    public async Task<IActionResult> Current() {
        var shift = await db.CashierShifts.AsNoTracking().SingleOrDefaultAsync(s => s.UserName == Actor && s.ClosedAt == null);
        return Ok(new { Shift = shift == null ? null : new { shift.Id, shift.OpenedAt, shift.OpeningCash }, CanOperate = DynamicAccess.Has(User, "SHIFT_SELF") });
    }
    [HttpGet]
    public async Task<IActionResult> List(DateTime? from = null, DateTime? to = null, string? userName = null, string? status = null, int page = 1)
    {
        if (page < 1 || page > 100000 || status != null && status != "open" && status != "closed") return BadRequest("Bộ lọc ca không hợp lệ.");
        DateTime start, end;
        try { (start, end) = FinancialReports.Range(from ?? DateTime.Today.AddDays(-30), to ?? DateTime.Today); } catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        var query = db.CashierShifts.AsNoTracking().Where(s => s.OpenedAt >= start && s.OpenedAt < end);
        if (!Manager) query = query.Where(s => s.UserName == Actor);
        else if (!string.IsNullOrWhiteSpace(userName)) query = query.Where(s => s.UserName == userName);
        if (status == "open") query = query.Where(s => s.ClosedAt == null);
        if (status == "closed") query = query.Where(s => s.ClosedAt != null);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(s => s.Id).Skip((page - 1) * 20).Take(20).Select(s => new { s.Id, s.UserName, Name = s.Account.DisplayName, s.OpenedAt, s.ClosedAt, s.OpeningCash, s.CountedCash, s.ExpectedCash, s.Difference, s.ClosedBy }).ToListAsync();
        var current = await db.CashierShifts.AsNoTracking().Where(s => s.UserName == Actor && s.ClosedAt == null).Select(s => (int?)s.Id).SingleOrDefaultAsync();
        return Ok(new { rows, total, page, current });
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id) {
        var shift = await db.CashierShifts.AsNoTracking().Include(s => s.Account).SingleOrDefaultAsync(s => s.Id == id);
        if (shift == null) return NotFound("Không tìm thấy ca.");
        if (!Manager && shift.UserName != Actor) return Forbid();
        var totals = await new ShiftFlow(db).Totals(shift);
        var entries = await db.CashEntries.AsNoTracking().Where(c => c.IdShift == id).OrderByDescending(c => c.Id).Select(c => new { c.Id, c.CreatedAt, c.Direction, c.Amount, c.PaymentMethod, c.Category, c.Note, c.CreatedBy, c.IdBill, c.IdImportReceipt }).ToListAsync();
        return Ok(new { shift.Id, shift.UserName, Name = shift.Account.DisplayName, shift.OpenedAt, shift.ClosedAt, shift.OpeningCash, shift.OpeningNote, shift.CountedCash, shift.Difference, shift.ClosingNote, shift.ClosedBy, Totals = totals, entries });
    }
    [HttpPost]
    public async Task<IActionResult> Open(ShiftOpenWrite dto) {
        try { return Ok(new { id = await new ShiftFlow(db).Open(Actor, dto.OpeningCash, dto.Note, dto.RequestKey) }); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, ShiftCloseWrite dto) {
        var owner = await db.CashierShifts.Where(s => s.Id == id).Select(s => s.UserName).SingleOrDefaultAsync();
        if (owner == null) return NotFound("Không tìm thấy ca.");
        var manage = DynamicAccess.Has(User, "SHIFT_CLOSE_OTHER");
        if (!manage && (owner != Actor || !DynamicAccess.Has(User, "SHIFT_SELF"))) return Forbid();
        try { await new ShiftFlow(db).Close(id, Actor, manage, dto.CountedCash, dto.Note, dto.ExpectedCash); return Ok(new { message = "Đã chốt ca và lưu đối chiếu tiền mặt." }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
public class ShiftOpenWrite { public decimal OpeningCash { get; set; } public string Note { get; set; } = ""; public string RequestKey { get; set; } = ""; }
public class ShiftCloseWrite { public decimal CountedCash { get; set; } public decimal ExpectedCash { get; set; } public string Note { get; set; } = ""; }
