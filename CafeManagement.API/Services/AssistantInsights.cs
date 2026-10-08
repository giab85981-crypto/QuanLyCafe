using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;

public class AssistantInsights(AppDbContext db) {
    public static readonly string[] Topics=["sales","orders","menu","stock","overview"];
    public static readonly string[] Periods=["today","yesterday","last7","last30","thisweek","thismonth"];
    public static DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow,"SE Asia Standard Time").Date;
    public static bool Allowed(ClaimsPrincipal user,string topic) => topic switch {
        "sales" or "menu" => DynamicAccess.Has(user,"DASHBOARD_VIEW") || DynamicAccess.Has(user,"REPORT_VIEW"),
        "orders" => DynamicAccess.Has(user,"ORDERS_VIEW") || DynamicAccess.Has(user,"DASHBOARD_VIEW") || DynamicAccess.Has(user,"REPORT_VIEW"),
        "stock" => DynamicAccess.Has(user,"INVENTORY_VIEW") || DynamicAccess.Has(user,"REPORT_VIEW"),
        "overview" => Topics.Where(t=>t!="overview").Any(t=>Allowed(user,t)),
        _ => false
    };
    public static (DateTime Start, DateTime End) Range(string period, DateTime today) => period switch {
        "today" => (today,today.AddDays(1)), "yesterday" => (today.AddDays(-1),today),
        "last7" => (today.AddDays(-6),today.AddDays(1)), "last30" => (today.AddDays(-29),today.AddDays(1)),
        "thisweek" => (today.AddDays(-((int)today.DayOfWeek+6)%7),today.AddDays(1)),
        "thismonth" => (new(today.Year,today.Month,1),today.AddDays(1)),
        _ => throw new InvalidOperationException("Hiện hỗ trợ hôm nay, hôm qua, 7/30 ngày qua, tuần này và tháng này.")
    };
    public static AssistantPlan BasicPlan(string question) {
        var text=new string(question.Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant().Replace('đ','d');
        if(new[]{"loi nhuan","gia von","khach hang","nhan vien","mat khau","cong no","nha cung cap","nam","thang truoc","tuan truoc","xoa","sua","sql","create","drop","insert","update","delete","thanh toan giup"}.Any(text.Contains) || Regex.IsMatch(text,@"\d{1,2}[/.-]\d") || Regex.IsMatch(text,@"\b(?!7\b|30\b)\d+\b")) return new("unsupported","unsupported",false);
        var period=text.Contains("hom qua")&&!text.Contains("hom nay") ? "yesterday" : text.Contains("30 ngay") ? "last30" : text.Contains("7 ngay") ? "last7" : text.Contains("thang nay") ? "thismonth" : text.Contains("tuan nay") ? "thisweek" : "today";
        var compare=text.Contains("so sanh")||text.Contains("tang hay giam")||text.Contains("so voi");
        var matches=new List<string>();
        if(text.Contains("doanh thu")||text.Contains("thu duoc"))matches.Add("sales");
        if(text.Contains("don hang")||text.Contains("bao nhieu don")||text.Contains("hoa don"))matches.Add("orders");
        if(text.Contains("ban chay")||text.Contains("ban nhieu nhat"))matches.Add("menu");
        if(text.Contains("nguyen lieu")||text.Contains("ton kho")||text.Contains("sap het"))matches.Add("stock");
        var topic=matches.Count>1?"overview":matches.FirstOrDefault()??(text.Contains("tinh hinh quan")||text.Contains("tong quan")?"overview":"unsupported");
        return new(topic,period,compare);
    }
    private static string Amount(decimal n) => n.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"))+" đ";
    private static string Count(double n) => n.ToString("0.###",CultureInfo.GetCultureInfo("vi-VN"));
    public async Task<AssistantAnswer> Read(AssistantPlan plan, ClaimsPrincipal user,string mode,string? warning,CancellationToken ct=default) {
        if(!Allowed(user,plan.Topic))throw new UnauthorizedAccessException("Không có quyền dữ liệu được yêu cầu.");
        var (start,end)=Range(plan.Period,Today);
        var label=$"{start:dd/MM/yyyy} – {end.AddDays(-1):dd/MM/yyyy}";
        var sections=new List<AssistantSection>();
        var wanted=plan.Topic=="overview"?Topics.Where(t=>t!="overview"&&Allowed(user,t)).ToList():[plan.Topic];
        if(wanted.Any(t=>t is "sales" or "orders" or "menu")) {
            var previousStart=start.AddDays(-(end-start).Days);
            var queryStart=plan.Compare?previousStart:start;
            var bills=await db.Bills.AsNoTracking().Where(b=>(b.Status==1||b.Status==3)&&(((b.DateCheckOut??b.DateCheckIn)>=queryStart&&(b.DateCheckOut??b.DateCheckIn)<end)||(b.CancelledAt>=queryStart&&b.CancelledAt<end))).Include(b=>b.BillInfos).ThenInclude(i=>i.Food).ToListAsync(ct);
            var report=FinancialReports.Sales(bills,start,end);
            if(wanted.Contains("sales")) {
                var cards=new List<AssistantCard>{new("Doanh thu thuần",Amount(report.NetRevenue)),new("Đã thu sau giảm giá",Amount(report.Sales)),new("Hoàn tiền",Amount(report.Refunds))};
                var note="Doanh thu thuần = đã thu sau giảm giá và đổi điểm, trừ hoàn tiền ghi nhận trong kỳ. Không phải lợi nhuận.";
                if(report.EstimatedBills>0)note+=$" Có {report.EstimatedBills} hóa đơn cũ dùng số tiền ước tính.";
                if(plan.Compare) {
                    var before=FinancialReports.Sales(bills,previousStart,start).NetRevenue;
                    var delta=report.NetRevenue-before;
                    cards.Add(new("Kỳ liền trước",Amount(before)));cards.Add(new("Chênh lệch",Amount(delta)));
                    note+=$" So với {previousStart:dd/MM/yyyy} – {start.AddDays(-1):dd/MM/yyyy}. "+(before>0?$"{(delta>=0?"Tăng":"Giảm")} {Math.Abs(delta/before*100):0.##}%.":"Kỳ trước không có doanh thu dương nên không tính tỷ lệ tăng/giảm.");
                    if(plan.Period is "today" or "thisweek" or "thismonth")note+=" Kỳ hiện tại chưa kết thúc; thận trọng khi so sánh.";
                }
                sections.Add(new("Doanh thu", "Hóa đơn và hoàn tiền · "+label,"/dashboard",cards,[],note));
            }
            if(wanted.Contains("orders")) {
                var orderCards=new List<AssistantCard>{new("Hóa đơn đã thanh toán",report.TotalBills.ToString()),new("Hóa đơn hoàn trong kỳ",bills.Count(b=>b.Status==3&&b.CancelledAt>=start&&b.CancelledAt<end).ToString())};
                var orderNote="Đơn đang phục vụ và đơn đã đóng chưa thanh toán không tính vào số hóa đơn đã thanh toán. Hóa đơn đã thanh toán rồi hoàn vẫn ghi nhận lần bán.";
                if(plan.Compare) {
                    var previous=FinancialReports.Sales(bills,previousStart,start).TotalBills;
                    orderCards.Add(new("Kỳ liền trước",previous.ToString()));
                    orderCards.Add(new("Chênh lệch số đơn",(report.TotalBills-previous).ToString()));
                    orderNote+=$" So với {previousStart:dd/MM/yyyy} – {start.AddDays(-1):dd/MM/yyyy}; kỳ hiện tại có thể chưa kết thúc.";
                }
                sections.Add(new("Đơn hàng","Hóa đơn đã thanh toán · "+label,"/orders",orderCards,[],orderNote));
            }
            if(wanted.Contains("menu")) {
                var top=report.Foods.OrderByDescending(f=>f.TotalQuantity).ThenBy(f=>f.FoodId).Take(5).Select(f=>new AssistantRow(f.FoodName,$"{f.TotalQuantity} phần",Amount(f.TotalAmount)+" doanh thu phân bổ trước hoàn")).ToList();
                sections.Add(new("Món bán chạy","Chi tiết hóa đơn · "+label,"/dashboard",[],top,top.Count==0?"Chưa có món bán trong khoảng thời gian này.":"Top 5 theo số lượng trên hóa đơn đã thanh toán. Doanh thu phân bổ theo số tiền thực thu; chưa phân bổ hoàn tiền cho từng món."));
            }
        }
        if(wanted.Contains("stock")) {
            var ingredients=await db.Ingredients.AsNoTracking().Where(i=>i.IsActive).Include(i=>i.Lots).ToListAsync(ct);
            var held=await new StockReservations(db).Held();
            var stock=ingredients.Select(i=>new {i.Name,i.Unit,i.MinQuantity,Usable=StockReservations.Usable(i),Held=held.GetValueOrDefault(i.Id),Available=Math.Max(0,StockReservations.Usable(i)-held.GetValueOrDefault(i.Id))}).Where(i=>i.Available<=i.MinQuantity).OrderBy(i=>i.Available).ThenBy(i=>i.Name).ToList();
            sections.Add(new("Nguyên liệu sắp hết","Kho và giữ nguyên liệu · hiện tại","/inventory",[new("Nguyên liệu dưới hoặc bằng mức tối thiểu",stock.Count.ToString())],stock.Take(10).Select(i=>new AssistantRow(i.Name,$"{Count(i.Available)} {i.Unit} khả dụng",$"Còn dùng {Count(i.Usable)} · đã giữ {Count(i.Held)} · mức tối thiểu {Count(i.MinQuantity)} {i.Unit}")).ToList(),"Tồn khả dụng trừ lượng đã giữ cho các đơn chưa báo bếp, không tính lô hết hạn. Chỉ là tình trạng hiện tại, không có tồn kho lịch sử. Hiển thị tối đa 10 nguyên liệu."));
        }
        return new("Đây là số liệu của quán trong phạm vi quyền của bạn.",mode,warning,DateTimeOffset.UtcNow,label,sections);
    }
}
