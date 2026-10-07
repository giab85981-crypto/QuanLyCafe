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
public class OrdersController(AppDbContext db) : ControllerBase
{
    string Actor => HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    IQueryable<Bill> Filter(DateTime? from, DateTime? to, int? status, string? type, string? search, int? tableId, string? payment)
    {
        var query = db.Bills.AsNoTracking();
        if (from.HasValue) query = query.Where(b => (b.DateCheckOut ?? b.DateCheckIn) >= from);
        if (to.HasValue) query = query.Where(b => (b.DateCheckOut ?? b.DateCheckIn) < to);
        if (status.HasValue) query = query.Where(b => b.Status == status);
        if (!string.IsNullOrEmpty(type)) query = query.Where(b => b.OrderType == type);
        if (tableId.HasValue) query = query.Where(b => b.IdTable == tableId);
        if (!string.IsNullOrEmpty(payment)) query = query.Where(b => b.PaymentMethod == payment);
        if (!string.IsNullOrWhiteSpace(search)) {
            var term = search.Trim(); var numeric = term.StartsWith("HD", StringComparison.OrdinalIgnoreCase) ? term[2..] : term;
            int.TryParse(numeric, out var id);
            query = query.Where(b => b.Id == id || b.TableNameSnapshot.Contains(term) || (b.TableFood != null && b.TableFood.Name.Contains(term)) || (b.Customer != null && (b.Customer.Name.Contains(term) || b.Customer.Phone.Contains(term))) || b.Note.Contains(term));
        }
        return query;
    }
    [HttpGet]
    public async Task<IActionResult> List(DateTime? from = null, DateTime? to = null, int? status = null, string? type = null, string? search = null, int? tableId = null, string? payment = null, int page = 1, int pageSize = 15)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || (from.HasValue && to.HasValue && from >= to) || (status.HasValue && (status < 0 || status > 3)) || (type != null && type != "DineIn" && type != "Takeaway") || (payment != null && !new[] { "Cash", "Transfer", "Unknown" }.Contains(payment))) return BadRequest("Bộ lọc không hợp lệ.");
        var query = Filter(from, to, status, type, search, tableId, payment);
        var total = await query.CountAsync();
        var paidTotals = await query.Where(b => b.Status == 1).Select(b => new { b.TotalPrice, b.Discount, b.HasRecordedPayment, Gross = b.BillInfos.Sum(i => i.Count * (i.UnitPrice ?? i.Food.Price)) }).ToListAsync();
        var revenue = paidTotals.Sum(b => !b.HasRecordedPayment && b.TotalPrice == 0 && b.Discount < 100 && b.Gross > 0 ? Math.Round(b.Gross * (100 - b.Discount) / 100, 2, MidpointRounding.AwayFromZero) : b.TotalPrice);
        var estimated = paidTotals.Count(b => !b.HasRecordedPayment && b.TotalPrice == 0 && b.Discount < 100 && b.Gross > 0);
        var refunded = await query.Where(b => b.Status == 3).SumAsync(b => (decimal?)b.RefundAmount) ?? 0;
        var serving = await query.CountAsync(b => b.Status == 0);
        var rows = await query.OrderByDescending(b => b.DateCheckOut ?? b.DateCheckIn).ThenByDescending(b => b.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new { b.Id, b.IdTable, b.OrderType, TableName = b.TableNameSnapshot != "" ? b.TableNameSnapshot : b.TableFood != null ? b.TableFood.Name : "Mang về", CustomerName = b.CustomerNameSnapshot != "" ? b.CustomerNameSnapshot : b.Customer != null ? b.Customer.Name : "Khách lẻ", b.DateCheckIn, b.DateCheckOut, b.Status, b.Discount, Gross = b.BillInfos.Sum(i => i.Count * (i.UnitPrice ?? i.Food.Price)), Estimated = b.Status == 1 && !b.HasRecordedPayment && b.TotalPrice == 0 && b.Discount < 100 && b.BillInfos.Any(i => i.Count > 0 && (i.UnitPrice ?? i.Food.Price) > 0), Total = b.Status == 0 || (b.Status == 1 && !b.HasRecordedPayment && b.TotalPrice == 0 && b.Discount < 100) ? b.BillInfos.Sum(i => i.Count * (i.UnitPrice ?? i.Food.Price)) * (100 - b.Discount) / 100 : b.TotalPrice, b.PaymentMethod, b.RefundAmount, b.GuestCount, b.CreatedBy }).ToListAsync();
        return Ok(new { total, page, pageSize, revenue, estimated, refunded, serving, rows });
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var bill = await db.Bills.AsNoTracking().Include(b => b.TableFood).Include(b => b.Customer).Include(b => b.BillInfos).ThenInclude(i => i.Food).SingleOrDefaultAsync(b => b.Id == id);
        if (bill == null) return NotFound("Không tìm thấy hóa đơn.");
        if (HttpContext?.User.Identity?.IsAuthenticated == true && !DynamicAccess.Has(User, "ORDERS_VIEW") && bill.Status != 0) return Forbid();
        var lines = bill.BillInfos.Where(i => i.Count > 0).Select(i => new { IdBillInfo = i.Id, i.IdFood, FoodName = i.FoodNameSnapshot != "" ? i.FoodNameSnapshot : i.Food.Name, Price = i.UnitPrice ?? i.Food.Price, i.Count, i.SentCount, i.OptionLabel }).ToList();
        var gross = lines.Sum(i => i.Price * i.Count);
        var payments = await db.CashEntries.AsNoTracking().Where(c => c.IdBill == id).OrderBy(c => c.Id).Select(c => new { c.Id, c.Direction, c.Amount, c.PaymentMethod, c.CreatedAt, c.CreatedBy, c.Note }).ToListAsync();
        var transfers = await db.TableOperations.AsNoTracking().Where(o => o.SourceBillId == id || o.TargetBillId == id).OrderBy(o => o.Id).ToListAsync();
        return Ok(new { IdBill = bill.Id, bill.IdTable, bill.IdShift, bill.OrderType, TableName = bill.TableNameSnapshot != "" ? bill.TableNameSnapshot : bill.TableFood?.Name ?? "Mang về", CustomerName = bill.CustomerNameSnapshot != "" ? bill.CustomerNameSnapshot : bill.Customer?.Name ?? "Khách lẻ", bill.IdCustomer, Customer = bill.Customer == null ? null : CustomerCatalog.Dto(bill.Customer), bill.PointDiscount, bill.PointsEarned, bill.PointsRedeemed, bill.DateCheckIn, bill.DateCheckOut, bill.Status, bill.Discount, bill.GuestCount, bill.Note, bill.CreatedBy, bill.PaidBy, bill.PaymentMethod, Gross = gross, Estimated = bill.Status == 1 && DashboardCalculator.IsEstimated(bill), TotalAmount = bill.Status == 0 ? Math.Round(gross * (100 - bill.Discount) / 100, 2) : DashboardCalculator.NetRevenue(bill), bill.RefundAmount, bill.RefundMethod, bill.CancelledAt, bill.CancelledBy, bill.CancellationReason, Items = lines, payments, transfers });
    }
    [HttpPost("takeaway")]
    public async Task<IActionResult> Takeaway(TakeawayWrite dto)
    {
        try { return Ok(new { idBill = await new InvoiceFlow(db).Takeaway(dto, Actor) }); } catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, RefundWrite dto)
    {
        try { await new InvoiceFlow(db).Refund(id, dto, Actor); return Ok(new { message = "Đã hủy hóa đơn và ghi chi hoàn tiền. Giữ nguyên lịch sử món và tồn kho." }); } catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
