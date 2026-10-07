using CafeManagement.API.Entities;

namespace CafeManagement.API.Services;

public static class FinancialReports
{
    public static (DateTime Start, DateTime End) Range(DateTime from, DateTime to)
    {
        if (from == default || to == default || to.Date < from.Date || (to.Date - from.Date).TotalDays >= 366 || to.Date == DateTime.MaxValue.Date)
            throw new InvalidOperationException("Chọn khoảng ngày hợp lệ, tối đa 366 ngày.");
        return (from.Date, to.Date.AddDays(1));
    }

    // A refund is recognized on its own date. Consumed ingredients are not returned.
    public static SalesReport Sales(IEnumerable<Bill> source, DateTime start, DateTime end)
    {
        var bills = source.Where(b => b.Status is 1 or 3).ToList();
        var sold = bills.Where(b => (b.DateCheckOut ?? b.DateCheckIn) >= start && (b.DateCheckOut ?? b.DateCheckIn) < end).ToList();
        var refunds = bills.Where(b => b.Status == 3 && b.CancelledAt >= start && b.CancelledAt < end).ToList();
        var daily = Enumerable.Range(0, (end - start).Days).Select(offset =>
        {
            var date = start.AddDays(offset);
            var sales = sold.Where(b => (b.DateCheckOut ?? b.DateCheckIn).Date == date).ToList();
            var returns = refunds.Where(b => b.CancelledAt!.Value.Date == date).ToList();
            var revenue = sales.Sum(DashboardCalculator.NetRevenue);
            var refund = returns.Sum(b => b.RefundAmount);
            var cost = sales.Sum(b => b.BillInfos.Where(i => i.Count > 0).Sum(i => (decimal)i.CostPrice * i.Count));
            return new SalesDay(date, sales.Count, revenue, refund, cost, revenue - refund, revenue - refund - cost);
        }).ToList();
        var foods = sold.SelectMany(b =>
        {
            var gross = DashboardCalculator.GrossRevenue(b);
            return b.BillInfos.Where(i => i.Count > 0).Select(i => new
            {
                i.IdFood, Name = i.FoodNameSnapshot != "" ? i.FoodNameSnapshot : i.Food.Name,
                i.Count, Amount = gross == 0 ? 0 : DashboardCalculator.NetRevenue(b) * i.Count * (i.UnitPrice ?? i.Food.Price) / gross
            });
        }).GroupBy(i => i.IdFood).Select(g => new FoodSales(g.Key, g.First().Name, g.Sum(i => i.Count), g.Sum(i => i.Amount)))
            .OrderByDescending(i => i.TotalAmount).ThenBy(i => i.FoodId).ToList();
        return new(daily, foods, sold.Count, daily.Sum(d => d.Sales), daily.Sum(d => d.Refunds), daily.Sum(d => d.NetRevenue), daily.Sum(d => d.Cost), daily.Sum(d => d.GrossProfit), sold.Count(DashboardCalculator.IsEstimated));
    }

    public static object Cash(IEnumerable<CashEntry> source, DateTime start, DateTime end)
    {
        var entries = source.Where(c => c.CreatedAt < end).ToList();
        decimal Signed(CashEntry c) => c.Direction == "In" ? c.Amount : -c.Amount;
        return entries.GroupBy(c => c.PaymentMethod).OrderBy(g => g.Key).Select(g => new
        {
            PaymentMethod = g.Key, Opening = g.Where(c => c.CreatedAt < start).Sum(Signed),
            TotalIn = g.Where(c => c.CreatedAt >= start && c.Direction == "In").Sum(c => c.Amount),
            TotalOut = g.Where(c => c.CreatedAt >= start && c.Direction == "Out").Sum(c => c.Amount),
            Closing = g.Sum(Signed)
        }).ToList();
    }
}
public record SalesDay(DateTime Date, int Bills, decimal Sales, decimal Refunds, decimal Cost, decimal NetRevenue, decimal GrossProfit);
public record FoodSales(int FoodId, string FoodName, int TotalQuantity, decimal TotalAmount);
public record SalesReport(List<SalesDay> Daily, List<FoodSales> Foods, int TotalBills, decimal Sales, decimal Refunds, decimal NetRevenue, decimal Cost, decimal GrossProfit, int EstimatedBills);
