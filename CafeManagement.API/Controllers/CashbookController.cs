using System.Security.Claims;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[Authorize, ApiController, Route("api/[controller]")]
public class CashbookController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(DateTime? from, DateTime? to)
    {
        DateTime start, end;
        try { (start, end) = FinancialReports.Range(from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), to ?? DateTime.Today); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        var entries = await db.CashEntries.AsNoTracking().Include(c => c.ImportReceipt).ThenInclude(r => r!.Supplier).Where(c => c.CreatedAt < end).OrderByDescending(c => c.Id).ToListAsync();
        var recordedBillIds = await db.CashEntries.Where(c => c.IdBill != null).Select(c => c.IdBill!.Value).ToListAsync();
        var legacyBills = await db.Bills.Include(b => b.BillInfos).ThenInclude(i => i.Food).Where(b => b.Status == 1 && (!b.HasRecordedPayment || b.TotalPrice > 0) && b.DateCheckOut >= start && b.DateCheckOut < end && !recordedBillIds.Contains(b.Id)).ToListAsync();
        var rows = entries.Where(c => c.CreatedAt >= start).Select(c => new CashbookRow { Id = $"SQ{c.Id:D6}", IdShift = c.IdShift, Date = c.CreatedAt, Direction = c.Direction, Category = c.Category, Note = c.Note, Amount = c.Amount, PaymentMethod = c.PaymentMethod, CreatedBy = c.CreatedBy,
            Reference = c.IdImportReceipt.HasValue ? $"PN{c.IdImportReceipt:D6}" : c.IdBill.HasValue ? $"HD{c.IdBill:D6}" : "", SupplierName = c.ImportReceipt?.Supplier.Name ?? "" }).ToList();
        rows.AddRange(legacyBills.Select(b => new CashbookRow { Id = $"HD{b.Id:D6}", Date = b.DateCheckOut!.Value, Direction = "In", Category = "Bán hàng", Note = "Hóa đơn cũ — chưa có phiếu thu, chỉ để đối chiếu", Amount = DashboardCalculator.NetRevenue(b), PaymentMethod = "Unknown", Reference = $"HD{b.Id:D6}", Estimated = DashboardCalculator.IsEstimated(b), Legacy = true }));
        return Ok(new { Rows = rows.OrderByDescending(r => r.Date), TotalIn = rows.Where(r => !r.Legacy && r.Direction == "In").Sum(r => r.Amount), TotalOut = rows.Where(r => !r.Legacy && r.Direction == "Out").Sum(r => r.Amount), EstimatedEntries = rows.Count(r => r.Estimated), LegacyAmount = rows.Where(r => r.Legacy).Sum(r => r.Amount), Methods = FinancialReports.Cash(entries, start, end) });
    }
    [HttpPost]
    public async Task<IActionResult> Create(CashEntryWrite dto)
    {
        try
        {
            WarehouseFlow.Amount(dto.Amount); WarehouseFlow.Method(dto.PaymentMethod); WarehouseFlow.Text(dto.Category, 100, "Loại thu chi", true); WarehouseFlow.Text(dto.Note, 500, "Nội dung", true); WarehouseFlow.Text(dto.RequestKey, 64, "Mã yêu cầu");
            if (dto.Direction != "In" && dto.Direction != "Out") return BadRequest("Loại thu chi không hợp lệ.");
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await new StockReservations(db).Lock();
            if (dto.RequestKey != "") { var old = await db.CashEntries.FirstOrDefaultAsync(c => c.RequestKey == dto.RequestKey); if (old != null) return Ok(new { old.Id }); }
            var row = new CashEntry { Amount = dto.Amount, Direction = dto.Direction, Category = dto.Category.Trim(), Note = dto.Note.Trim(), PaymentMethod = dto.PaymentMethod, RequestKey = dto.RequestKey, CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system" };
            await new ShiftFlow(db).Attach(row, row.CreatedBy);
            db.CashEntries.Add(row); await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { row.Id });
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
public class CashbookRow
{
    public int? IdShift { get; set; } public string Id { get; set; } = ""; public DateTime Date { get; set; } public string Direction { get; set; } = ""; public string Category { get; set; } = ""; public string Note { get; set; } = ""; public decimal Amount { get; set; } public string PaymentMethod { get; set; } = ""; public string CreatedBy { get; set; } = ""; public string Reference { get; set; } = ""; public string SupplierName { get; set; } = ""; public bool Estimated { get; set; } public bool Legacy { get; set; }
}

