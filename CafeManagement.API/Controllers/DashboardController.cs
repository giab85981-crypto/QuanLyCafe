using CafeManagement.API.Data;
using CafeManagement.API.Dtos;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(int days = 7, int? revenueDays = null,
        int? customerDays = null, int? menuDays = null, string revenueGroup = "day",
        string customerGroup = "day", string menuGroup = "category", string? menuFilter = null,
        string sortBy = "quantity", CancellationToken cancellationToken = default)
    {
        var periods = new[] { days, revenueDays ?? days, customerDays ?? days, menuDays ?? days };
        if (periods.Any(p => p < 1 || p > 366))
            return BadRequest(new { message = "Khoảng thời gian phải từ 1 đến 366 ngày." });
        if (!new[] { "hour", "day", "weekday" }.Contains(revenueGroup)
            || !new[] { "hour", "day", "weekday" }.Contains(customerGroup)
            || !new[] { "category", "type" }.Contains(menuGroup)
            || !new[] { "quantity", "revenue" }.Contains(sortBy))
            return BadRequest(new { message = "Bộ lọc thống kê không hợp lệ." });

        // Existing DateCheckIn/Out values are Vietnam local wall-clock time.
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time").Date;
        var start = today.AddDays(1 - periods.Max());
        var end = today.AddDays(1);
        var bills = await context.Bills.AsNoTracking()
            .Where(b => (b.Status == 1 || b.Status == 3) && (((b.DateCheckOut ?? b.DateCheckIn) >= start
                && (b.DateCheckOut ?? b.DateCheckIn) < end) || (b.CancelledAt >= start && b.CancelledAt < end)))
            .Include(b => b.BillInfos).ThenInclude(i => i.Food).ThenInclude(f => f.Category)
            .ToListAsync(cancellationToken);
        var tables = await context.TableFoods.AsNoTracking().Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
        var serving = await context.Bills.AsNoTracking().Where(b => b.Status == 0)
            .Select(b => new { b.IdTable, b.GuestCount }).ToListAsync(cancellationToken);
        var todayBills = bills.Where(b => (b.DateCheckOut ?? b.DateCheckIn).Date == today).ToList();
        var occupied = tables.Count(t => t.Status == "Có người" || t.Status == "1"
            || serving.Any(b => b.IdTable == t.Id));
        var knownGuests = todayBills.Where(b => b.GuestCount.HasValue).ToList();
        var summary = new DashboardSummaryDto
        {
            TodayRevenue = todayBills.Sum(DashboardCalculator.NetRevenue) - bills.Where(b => b.Status == 3 && b.CancelledAt >= today && b.CancelledAt < end).Sum(b => b.RefundAmount),
            TodayRefund = bills.Where(b => b.Status == 3 && b.CancelledAt >= today && b.CancelledAt < end).Sum(b => b.RefundAmount),
            TodayOrderCount = todayBills.Count,
            TodayDiscount = todayBills.Sum(b => DashboardCalculator.GrossRevenue(b)
                * Math.Clamp(b.Discount, 0, 100) / 100m + b.PointDiscount),
            AverageGuestsPerOrder = knownGuests.Count == 0 ? null
                : Math.Round(knownGuests.Average(b => (double)b.GuestCount!.Value), 2),
            TotalTables = tables.Count,
            OccupiedTables = occupied,
            OccupancyRate = tables.Count == 0 ? 0 : Math.Round(occupied * 100d / tables.Count, 1),
            ServingOrders = serving.Count,
            ServingGuests = serving.Sum(b => b.GuestCount ?? 0),
            UnknownServingGuestOrders = serving.Count(b => !b.GuestCount.HasValue)
        };
        summary.AverageOrderValue = todayBills.Count == 0 ? 0 : summary.TodayRevenue / todayBills.Count;
        var revenue = DashboardCalculator.Chart(bills, today, revenueDays ?? days, revenueGroup);
        var customers = DashboardCalculator.Chart(bills, today, customerDays ?? days, customerGroup);
        var menu = DashboardCalculator.Menu(bills, today, menuDays ?? days, menuGroup, menuFilter, sortBy);
        return Ok(new DashboardOverviewResponse
        {
            Summary = summary, Revenue = revenue, Customers = customers, Menu = menu,
            RevenueChart = revenue.Points, TopSellingFoods = menu.TopSellingFoods, CategorySales = menu.Sales
        });
    }
}
