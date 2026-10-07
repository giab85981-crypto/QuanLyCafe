using CafeManagement.API.Dtos;
using CafeManagement.API.Entities;

namespace CafeManagement.API.Services;

public static class DashboardCalculator
{
    public static decimal GrossRevenue(Bill bill) => bill.BillInfos
        .Where(i => i.Count > 0).Sum(i => i.Count * (i.UnitPrice ?? i.Food.Price));

    public static bool IsEstimated(Bill bill) => !bill.HasRecordedPayment && bill.TotalPrice == 0 && bill.Discount < 100
        && GrossRevenue(bill) > 0;

    public static decimal NetRevenue(Bill bill) => IsEstimated(bill)
        ? Math.Round(GrossRevenue(bill) * (100 - Math.Clamp(bill.Discount, 0, 100)) / 100m, 2)
        : bill.TotalPrice;

    private static List<Bill> InRange(IEnumerable<Bill> bills, DateTime today, int days) => bills
        .Where(b => (b.Status == 1 || b.Status == 3) && (b.DateCheckOut ?? b.DateCheckIn) >= today.AddDays(1 - days)
            && (b.DateCheckOut ?? b.DateCheckIn) < today.AddDays(1)).ToList();

    public static ChartSummaryDto Chart(IEnumerable<Bill> bills, DateTime today, int days, string group)
    {
        var range = InRange(bills, today, days);
        var refunds = bills.Where(b => b.Status == 3 && b.CancelledAt >= today.AddDays(1 - days) && b.CancelledAt < today.AddDays(1)).ToList();
        var points = new List<DailyRevenueDto>();
        var labels = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
        var length = group == "hour" ? 24 : group == "weekday" ? 7 : days;
        for (var i = 0; i < length; i++)
        {
            var index = i;
            var date = today.AddDays(1 - days + i);
            var matching = range.Where(b =>
            {
                var time = b.DateCheckOut ?? b.DateCheckIn;
                return group == "hour" ? time.Hour == index : group == "weekday"
                    ? ((int)time.DayOfWeek + 6) % 7 == index : time.Date == date.Date;
            }).ToList();
            var returned = refunds.Where(b => {
                var time = b.CancelledAt!.Value;
                return group == "hour" ? time.Hour == index : group == "weekday" ? ((int)time.DayOfWeek + 6) % 7 == index : time.Date == date.Date;
            }).Sum(b => b.RefundAmount);
            points.Add(new DailyRevenueDto
            {
                Date = group == "hour" ? $"{i:00}:00" : group == "weekday" ? labels[i] : date.ToString("dd/MM"),
                Revenue = matching.Sum(NetRevenue) - returned, OrderCount = matching.Count,
                GuestCount = matching.Sum(b => b.GuestCount ?? 0)
            });
        }
        return new ChartSummaryDto
        {
            Revenue = range.Sum(NetRevenue) - refunds.Sum(b => b.RefundAmount), OrderCount = range.Count,
            GuestCount = range.Sum(b => b.GuestCount ?? 0),
            UnknownGuestOrders = range.Count(b => b.GuestCount == null),
            EstimatedRevenueOrders = range.Count(IsEstimated), Points = points
        };
    }

    public static MenuPerformanceDto Menu(IEnumerable<Bill> bills, DateTime today, int days,
        string group, string? filter, string sortBy)
    {
        // Allocate the recorded net bill total proportionally, including discounts/rounding.
        var lines = InRange(bills, today, days).SelectMany(b =>
        {
            var gross = GrossRevenue(b);
            return b.BillInfos.Where(i => i.Count > 0).Select(i => new
            {
                i.IdFood, i.Food.Name, i.Count, i.Food.MenuKind,
                Key = group == "type" ? (string.IsNullOrWhiteSpace(i.Food.ItemType) ? "Chưa phân loại" : i.Food.ItemType)
                    : i.Food.IdCategory.ToString(),
                Label = group == "type" ? (string.IsNullOrWhiteSpace(i.Food.ItemType) ? "Chưa phân loại" : i.Food.ItemType)
                    : i.Food.Category?.Name ?? "Khác",
                Revenue = gross == 0 ? 0 : NetRevenue(b) * (i.Count * (i.UnitPrice ?? i.Food.Price)) / gross
            });
        }).ToList();
        var sales = lines.GroupBy(i => new { i.Key, i.Label }).Select(g => new CategorySalesDto
        {
            Key = g.Key.Key, CategoryName = g.Key.Label, QuantitySold = g.Sum(i => i.Count),
            TotalRevenue = g.Sum(i => i.Revenue)
        }).OrderByDescending(g => g.TotalRevenue).ThenBy(g => g.Key).ToList();
        decimal Average(string? kind) {
            var items = lines.Where(i => kind == null || i.MenuKind == kind).ToList();
            var count = items.Sum(i => i.Count);
            return count == 0 ? 0 : items.Sum(i => i.Revenue) / count;
        }
        var foods = lines.Where(i => string.IsNullOrEmpty(filter) || i.Key == filter)
            .GroupBy(i => new { i.IdFood, i.Name }).Select(g => new TopSellingFoodDto
            {
                FoodId = g.Key.IdFood, FoodName = g.Key.Name,
                QuantitySold = g.Sum(i => i.Count), TotalRevenue = g.Sum(i => i.Revenue)
            });
        var top = (sortBy == "revenue" ? foods.OrderByDescending(i => i.TotalRevenue).ThenBy(i => i.FoodId)
            : foods.OrderByDescending(i => i.QuantitySold).ThenBy(i => i.FoodId)).Take(10).ToList();
        for (var i = 0; i < top.Count; i++) top[i].Stt = i + 1;
        return new MenuPerformanceDto
        {
            AverageItemValue = Average(null), AverageFoodValue = Average("Đồ ăn"),
            AverageDrinkValue = Average("Đồ uống"),
            UnclassifiedQuantity = lines.Where(i => i.MenuKind != "Đồ ăn" && i.MenuKind != "Đồ uống").Sum(i => i.Count),
            Sales = sales, FilterOptions = sales, TopSellingFoods = top
        };
    }
}
