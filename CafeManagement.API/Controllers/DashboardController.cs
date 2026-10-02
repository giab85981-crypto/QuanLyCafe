using CafeManagement.API.Data;
using CafeManagement.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] int days = 7)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var startDate = today.AddDays(-(days - 1));

            // 1. Thống kê KPI Hôm nay
            var todayBills = await _context.Bills
                .Where(b => b.Status == 1 && b.DateCheckIn >= today && b.DateCheckIn < tomorrow)
                .ToListAsync();

            decimal todayRevenue = todayBills.Sum(b => b.TotalPrice);
            int todayOrderCount = todayBills.Count;

            int totalTables = await _context.TableFoods.CountAsync();
            int occupiedTables = await _context.TableFoods.CountAsync(t => t.Status == "Có người" || t.Status == "1");
            double occupancyRate = totalTables > 0 ? Math.Round((double)occupiedTables / totalTables * 100, 1) : 0;

            var summary = new DashboardSummaryDto
            {
                TodayRevenue = todayRevenue,
                TodayOrderCount = todayOrderCount,
                OccupiedTables = occupiedTables,
                TotalTables = totalTables,
                OccupancyRate = occupancyRate
            };

            // 2. Top 10 Món bán chạy nhất (Dùng IdFood chuẩn tên entity)
            var topFoodsQuery = await _context.BillInfos
                .Where(bi => bi.Bill != null && bi.Bill.Status == 1)
                .GroupBy(bi => new { bi.IdFood, bi.Food.Name })
                .Select(g => new
                {
                    FoodName = g.Key.Name,
                    QuantitySold = g.Sum(x => x.Count),
                    TotalRevenue = g.Sum(x => x.Count * x.Food.Price)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10)
                .ToListAsync();

            var topSellingFoods = topFoodsQuery.Select((item, index) => new TopSellingFoodDto
            {
                Stt = index + 1,
                FoodName = item.FoodName,
                QuantitySold = item.QuantitySold,
                TotalRevenue = item.TotalRevenue
            }).ToList();

            // 3. Doanh thu theo Nhóm món (Category)
            var categorySales = await _context.BillInfos
                .Where(bi => bi.Bill != null && bi.Bill.Status == 1)
                .GroupBy(bi => bi.Food.Category.Name)
                .Select(g => new CategorySalesDto
                {
                    CategoryName = g.Key ?? "Khác",
                    QuantitySold = g.Sum(x => x.Count),
                    TotalRevenue = g.Sum(x => x.Count * x.Food.Price)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            // 4. Biểu đồ doanh thu theo chuỗi ngày (mặc định 7 ngày gần nhất)
            var rangeBills = await _context.Bills
                .Where(b => b.Status == 1 && b.DateCheckIn >= startDate && b.DateCheckIn < tomorrow)
                .ToListAsync();

            var revenueChart = new List<DailyRevenueDto>();
            for (int i = 0; i < days; i++)
            {
                var currentDate = startDate.AddDays(i);
                var dayBills = rangeBills.Where(b => b.DateCheckIn.Date == currentDate.Date).ToList();

                revenueChart.Add(new DailyRevenueDto
                {
                    Date = currentDate.ToString("dd/MM"),
                    Revenue = dayBills.Sum(b => b.TotalPrice),
                    OrderCount = dayBills.Count
                });
            }

            return Ok(new DashboardOverviewResponse
            {
                Summary = summary,
                TopSellingFoods = topSellingFoods,
                CategorySales = categorySales,
                RevenueChart = revenueChart
            });
        }
    }
}