using System.Data;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class InvoiceFlow(AppDbContext db)
{
    public async Task<int> Takeaway(TakeawayWrite dto, string actor)
    {
        if (!Guid.TryParse(dto.RequestKey, out _) || dto.Label == null || dto.Label.Length > 100 || dto.Note == null || dto.Note.Length > 500) throw new InvalidOperationException("Thông tin đơn mang về không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existing = await db.Bills.SingleOrDefaultAsync(b => b.CreationKey == dto.RequestKey);
        if (existing != null) return existing.Id;
        var bill = new Bill { OrderType = "Takeaway", IdTable = null, TableNameSnapshot = string.IsNullOrWhiteSpace(dto.Label) ? "Mang về" : dto.Label.Trim(), Note = dto.Note.Trim(), CreationKey = dto.RequestKey, CreatedBy = actor, GuestCount = 1 };
        db.Bills.Add(bill); await db.SaveChangesAsync(); await tx.CommitAsync(); return bill.Id;
    }
    public async Task Refund(int id, RefundWrite dto, string actor)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500 || !new[] { "Cash", "Transfer" }.Contains(dto.PaymentMethod)) throw new InvalidOperationException("Nhập lý do hủy và phương thức hoàn tiền hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var bill = await db.Bills.Include(b => b.BillInfos).ThenInclude(i => i.Food).Include(b => b.KitchenOrders).ThenInclude(o => o.KitchenOrderDetails).AsSplitQuery().SingleOrDefaultAsync(b => b.Id == id) ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        if (bill.Status == 3) return; // Retrying must never create another refund.
        if (bill.Status != 1) throw new InvalidOperationException("Chỉ hủy hoàn tiền hóa đơn đã thanh toán. Đơn đang phục vụ cần hủy từng món.");
        if (!bill.HasRecordedPayment && bill.TotalPrice == 0 && bill.Discount < 100 && DashboardCalculator.GrossRevenue(bill) > 0) throw new InvalidOperationException("Hóa đơn cũ chưa ghi số tiền thực thu; chưa thể tự động hoàn tiền.");
        if (bill.TotalPrice > 0 && !await db.CashEntries.AnyAsync(c => c.IdBill == id && c.Direction == "In"))
            db.CashEntries.Add(new CashEntry { IdBill = id, Direction = "In", Category = "Bán hàng", Amount = bill.TotalPrice, PaymentMethod = bill.PaymentMethod == "" ? "Unknown" : bill.PaymentMethod, CreatedAt = bill.DateCheckOut ?? bill.DateCheckIn, CreatedBy = bill.PaidBy, Note = $"Ghi nhận phiếu thu hóa đơn cũ HD{id:D6} trước khi hoàn tiền" });
        await new CustomerLoyalty(db).Refund(bill, actor);
        bill.Status = 3; bill.CancelledAt = DateTime.Now; bill.CancelledBy = actor; bill.CancellationReason = dto.Reason.Trim(); bill.RefundAmount = bill.TotalPrice; bill.RefundMethod = dto.PaymentMethod;
        foreach (var order in bill.KitchenOrders) {
            order.Status = "Cancelled";
            foreach (var line in order.KitchenOrderDetails.Where(d => d.Status != "Completed")) line.Status = "Cancelled";
        }
        // Paid drinks are considered consumed: preserve quantity, price and stock history.
        // Refund is a separate cash entry, leaving the original receipt untouched.
        if (bill.TotalPrice > 0) {
            var refund = new CashEntry { IdBill = bill.Id, Direction = "Out", Category = "Hoàn tiền hóa đơn", Amount = bill.TotalPrice, PaymentMethod = dto.PaymentMethod, CreatedBy = actor, Note = $"Hủy HD{bill.Id:D6}: {dto.Reason.Trim()}"[..Math.Min(500, $"Hủy HD{bill.Id:D6}: {dto.Reason.Trim()}".Length)] };
            await new ShiftFlow(db).Attach(refund, actor); db.CashEntries.Add(refund);
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
