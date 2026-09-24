using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var bills = await _context.Bills
                .Include(b => b.BillInfos)
                .ThenInclude(bi => bi.Food)
                .Where(b => b.Status == 1 && b.DateCheckOut >= fromDate && b.DateCheckOut <= toDate)
                .ToListAsync();

            var report = bills
                .GroupBy(b => b.DateCheckOut!.Value.Date)
                .Select(g =>
                {
                    decimal revenue = g.Sum(b => b.BillInfos.Sum(bi => bi.Count * bi.Food.Price) * (100 - b.Discount) / 100);
                    double cost = g.Sum(b => b.BillInfos.Sum(bi => bi.Count * bi.CostPrice));

                    return new RevenueReportDto
                    {
                        Date = g.Key,
                        TotalBills = g.Count(),
                        TotalRevenue = revenue,
                        TotalCost = cost,
                        TotalProfit = revenue - (decimal)cost
                    };
                })
                .OrderBy(r => r.Date)
                .ToList();

            return Ok(report);
        }

        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSelling([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] int top = 5)
        {
            var topFoods = await _context.BillInfos
                .Include(bi => bi.Bill)
                .Include(bi => bi.Food)
                .Where(bi => bi.Bill.Status == 1 && bi.Bill.DateCheckOut >= fromDate && bi.Bill.DateCheckOut <= toDate)
                .GroupBy(bi => bi.Food.Name)
                .Select(g => new TopFoodReportDto
                {
                    FoodName = g.Key,
                    TotalQuantity = g.Sum(bi => bi.Count),
                    TotalAmount = g.Sum(bi => bi.Count * bi.Food.Price)
                })
                .OrderByDescending(f => f.TotalQuantity)
                .Take(top)
                .ToListAsync();

            return Ok(topFoods);
        }
    }
}