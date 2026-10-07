using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
namespace CafeManagement.API.Services;
public class ShiftFlow(AppDbContext db)
{
    // Financial writers and closing use the same transaction lock as checkout/stock.
    public async Task<int?> ActiveId(string actor, bool required = false)
    {
        if (db.Database.CurrentTransaction == null) throw new InvalidOperationException("Giao dịch ca cần thực hiện trong transaction.");
        await new StockReservations(db).Lock();
        var id = await db.CashierShifts.Where(s => s.UserName == actor && s.ClosedAt == null).Select(s => (int?)s.Id).SingleOrDefaultAsync();
        if (required && id == null) throw new InvalidOperationException("Bạn chưa mở ca. Vào Ca làm việc để mở ca trước khi thanh toán.");
        return id;
    }
    public async Task Attach(CashEntry entry, string actor) => entry.IdShift = await ActiveId(actor);
    static void Amount(decimal value) { if (value < 0 || value > 999999999999m || decimal.Round(value, 2) != value) throw new InvalidOperationException("Số tiền cần từ 0 đến 999.999.999.999, tối đa 2 chữ số thập phân."); }
    static void Note(string? note) { if (note == null || note.Length > 500) throw new InvalidOperationException("Ghi chú tối đa 500 ký tự."); }
    public async Task<int> Open(string actor, decimal openingCash, string note, string requestKey)
    {
        Amount(openingCash); Note(note);
        if (requestKey == null || requestKey.Length > 64 || !Guid.TryParse(requestKey, out _)) throw new InvalidOperationException("Mã yêu cầu mở ca không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var old = await db.CashierShifts.SingleOrDefaultAsync(s => s.RequestKey == requestKey);
        if (old != null) { if (old.UserName != actor) throw new InvalidOperationException("Mã mở ca đã được dùng."); return old.Id; }
        if (!await db.Accounts.AnyAsync(a => a.UserName == actor && a.IsActive)) throw new InvalidOperationException("Tài khoản không còn hoạt động.");
        if (await db.CashierShifts.AnyAsync(s => s.UserName == actor && s.ClosedAt == null)) throw new InvalidOperationException("Bạn đang có ca mở. Hãy chốt ca hiện tại trước.");
        var shift = new CashierShift { UserName = actor, OpeningCash = openingCash, OpeningNote = note.Trim(), RequestKey = requestKey };
        db.CashierShifts.Add(shift); await db.SaveChangesAsync(); await tx.CommitAsync(); return shift.Id;
    }
    public async Task<ShiftTotals> Totals(CashierShift shift)
    {
        if (shift.ClosedAt != null && shift.ClosingSummaryJson != "") return JsonSerializer.Deserialize<ShiftTotals>(shift.ClosingSummaryJson)!;
        var entries = await db.CashEntries.AsNoTracking().Where(c => c.IdShift == shift.Id).ToListAsync();
        var methods = entries.GroupBy(c => c.PaymentMethod).Select(g => new ShiftMethod(g.Key, g.Where(c => c.Direction == "In").Sum(c => c.Amount), g.Where(c => c.Direction == "Out").Sum(c => c.Amount))).OrderBy(m => m.PaymentMethod).ToList();
        var sales = await db.Bills.AsNoTracking().Where(b => b.IdShift == shift.Id && (b.Status == 1 || b.Status == 3)).Select(b => b.TotalPrice).ToListAsync();
        var cash = methods.FirstOrDefault(m => m.PaymentMethod == "Cash");
        return new(methods, sales.Count, sales.Sum(), entries.Where(c => c.IdBill != null && c.Direction == "Out").Sum(c => c.Amount), shift.OpeningCash + (cash?.TotalIn ?? 0) - (cash?.TotalOut ?? 0));
    }
    public async Task Close(int id, string actor, bool canCloseOthers, decimal countedCash, string note, decimal expectedCash)
    {
        Amount(countedCash); Note(note);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var shift = await db.CashierShifts.SingleOrDefaultAsync(s => s.Id == id) ?? throw new InvalidOperationException("Không tìm thấy ca.");
        if (shift.UserName != actor && !canCloseOthers) throw new UnauthorizedAccessException("Bạn không được chốt ca của người khác.");
        if (shift.ClosedAt != null) {
            if (shift.CountedCash == countedCash && shift.ClosingNote == note.Trim() && shift.ClosedBy == actor) return;
            throw new InvalidOperationException("Ca đã chốt, không thể thay đổi số liệu.");
        }
        var totals = await Totals(shift);
        if (totals.ExpectedCash != expectedCash) throw new InvalidOperationException("Ca vừa phát sinh giao dịch. Tải lại số liệu và kiểm đếm lại trước khi chốt.");
        var diff = countedCash - totals.ExpectedCash;
        if (diff != 0 && string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Tiền thực đếm bị lệch. Hãy ghi lý do chênh lệch.");
        shift.CountedCash = countedCash; shift.ExpectedCash = totals.ExpectedCash; shift.Difference = diff;
        shift.ClosingNote = note.Trim(); shift.ClosedBy = actor; shift.ClosedAt = DateTime.Now; shift.ClosingSummaryJson = JsonSerializer.Serialize(totals);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
public record ShiftMethod(string PaymentMethod, decimal TotalIn, decimal TotalOut);
public record ShiftTotals(List<ShiftMethod> Methods, int Bills, decimal Sales, decimal Refunds, decimal ExpectedCash);
