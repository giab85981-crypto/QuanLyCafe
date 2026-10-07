using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Authorize]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var (start, end) = FinancialReports.Range(fromDate, toDate);
                var bills = await _context.Bills.AsNoTracking().Include(b => b.BillInfos).ThenInclude(i => i.Food)
                    .Where(b => (b.Status == 1 || b.Status == 3) && (((b.DateCheckOut ?? b.DateCheckIn) >= start && (b.DateCheckOut ?? b.DateCheckIn) < end) || (b.CancelledAt >= start && b.CancelledAt < end))).ToListAsync();
                var cash = await _context.CashEntries.AsNoTracking().Where(c => c.CreatedAt < end).ToListAsync();
                var ingredients = await _context.Ingredients.AsNoTracking().Include(i => i.Lots).ToListAsync();
                var imports = await _context.ImportReceipts.AsNoTracking().Include(r => r.Supplier).ToListAsync();
                var payments = await _context.CashEntries.AsNoTracking().Where(c => c.IdImportReceipt != null)
                    .GroupBy(c => c.IdImportReceipt).Select(g => new { Id = g.Key, Amount = g.Sum(c => c.Direction == "Out" ? c.Amount : -c.Amount) }).ToListAsync();
                var debt = imports.Where(r => r.HasPaymentTracking).GroupBy(r => new { r.IdSupplier, r.Supplier.Name }).Select(g => new
                {
                    SupplierId = g.Key.IdSupplier, SupplierName = g.Key.Name,
                    Total = g.Sum(r => r.TotalAmount), Paid = g.Sum(r => payments.FirstOrDefault(p => p.Id == r.Id)?.Amount ?? 0),
                    Debt = g.Sum(r => Math.Max(0, r.TotalAmount - (payments.FirstOrDefault(p => p.Id == r.Id)?.Amount ?? 0)))
                }).OrderByDescending(r => r.Debt).ToList();
                var today = DateTime.Today;
                var stock = ingredients.Select(i => new
                {
                    i.Id, i.Code, i.Name, i.Unit, i.Quantity, i.MinQuantity,
                    Value = i.Lots.Sum(l => (decimal)l.Quantity * l.UnitCost),
                    Expired = i.Lots.Where(l => l.ExpiryDate != null && l.ExpiryDate < today).Sum(l => l.Quantity),
                    Usable = i.Lots.Where(l => l.ExpiryDate == null || l.ExpiryDate >= today).Sum(l => l.Quantity)
                }).OrderBy(i => i.Name).ToList();
                return Ok(new { Sales = FinancialReports.Sales(bills, start, end), Cash = FinancialReports.Cash(cash, start, end), Stock = stock, Suppliers = debt,
                    SnapshotAt = DateTime.Now, UntrackedImports = imports.Count(r => !r.HasPaymentTracking) });
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            DateTime start, end;
            try { (start, end) = FinancialReports.Range(fromDate, toDate); } catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            var bills = await _context.Bills
                .Include(b => b.BillInfos)
                .ThenInclude(bi => bi.Food)
                .Where(b => (b.Status == 1 || b.Status == 3) && (((b.DateCheckOut ?? b.DateCheckIn) >= start && (b.DateCheckOut ?? b.DateCheckIn) < end) || (b.CancelledAt >= start && b.CancelledAt < end)))
                .ToListAsync();

            var report = FinancialReports.Sales(bills, start, end).Daily.Select(d => new RevenueReportDto
            { Date = d.Date, TotalBills = d.Bills, TotalRevenue = d.NetRevenue, TotalCost = (double)d.Cost, TotalProfit = d.GrossProfit });
            return Ok(report);
        }

        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSelling([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] int top = 5)
        {
            DateTime start, end;
            try { (start, end) = FinancialReports.Range(fromDate, toDate); } catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            if (top < 1 || top > 100) return BadRequest("Số món cần từ 1 đến 100.");
            var bills = await _context.Bills.AsNoTracking().Include(b => b.BillInfos).ThenInclude(i => i.Food)
                .Where(b => (b.Status == 1 || b.Status == 3) && (b.DateCheckOut ?? b.DateCheckIn) >= start && (b.DateCheckOut ?? b.DateCheckIn) < end).ToListAsync();
            var topFoods = FinancialReports.Sales(bills, start, end).Foods.OrderByDescending(f => f.TotalQuantity).ThenBy(f => f.FoodId).Take(top);
            return Ok(topFoods);
        }
    }
}
