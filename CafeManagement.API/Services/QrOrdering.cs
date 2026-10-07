using System.Data;
using System.Text.Json;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class QrOrdering(AppDbContext db)
{
    public static List<QrItemSnapshot> Items(QrOrderRequest r) => JsonSerializer.Deserialize<List<QrItemSnapshot>>(r.ItemsJson) ?? [];
    public static object PublicDto(QrOrderRequest r) => new { r.Id, r.TableName, Status = r.Status == "Pending" && r.ExpiresAt <= DateTime.UtcNow ? "Expired" : r.Status, r.CreatedAt, r.ExpiresAt, r.DecidedAt, r.Reason, r.Total, r.Note, Items = Items(r).Select(i => new { i.Name, i.Options, i.Count, i.Price }) };
    public async Task<List<QrItemSnapshot>> Quote(List<QrItemWrite> items)
    {
        if (items.Count is < 1 or > 30 || items.Any(i => i == null) || items.Sum(i => i.Count) > 100) throw new InvalidOperationException("Mỗi yêu cầu tối đa 30 dòng và 100 phần.");
        var result = new List<QrItemSnapshot>();
        foreach (var item in items)
        {
            if (item.Count is < 1 or > 20 || item.Toppings == null || item.Toppings.Count > 10 || item.Toppings.Any(t => t.Count is < 1 or > 20) || item.Toppings.Select(t => t.IdFood).Distinct().Count() != item.Toppings.Count) throw new InvalidOperationException("Số lượng hoặc topping không hợp lệ.");
            var food = await new MenuCatalog(db).Query().Include(f => f.AllowedToppings).ThenInclude(t => t.Topping).FirstOrDefaultAsync(f => f.Id == item.IdFood && f.IsActive && !f.IsTopping) ?? throw new InvalidOperationException("Món đã ngừng bán hoặc không tồn tại.");
            var variant = food.Variants.FirstOrDefault(v => v.Id == item.IdVariant && v.IsActive);
            if ((food.Variants.Any(v => v.IsActive) || item.IdVariant.HasValue) && variant == null) throw new InvalidOperationException("Vui lòng chọn size đang bán.");
            var price = variant?.Price ?? food.Price;
            var labels = new List<string>(); if (variant != null) labels.Add(variant.Name);
            foreach (var choice in item.Toppings.OrderBy(t => t.IdFood))
            {
                var topping = food.AllowedToppings.FirstOrDefault(t => t.IdTopping == choice.IdFood)?.Topping;
                if (topping == null || !topping.IsActive || !topping.IsTopping) throw new InvalidOperationException("Topping không còn được bán kèm món này.");
                price += topping.Price * choice.Count; labels.Add($"{topping.Name} ×{choice.Count}");
            }
            result.Add(new(item.IdFood, item.IdVariant, item.Count, item.Toppings, food.Name, string.Join(" · ", labels), price));
        }
        return result;
    }
    public async Task<QrOrderRequest> Submit(int tableId, QrOrderWrite dto)
    {
        if (!Guid.TryParseExact(dto.RequestKey, "D", out _)) throw new InvalidOperationException("Mã yêu cầu không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var existing = await db.QrOrderRequests.SingleOrDefaultAsync(r => r.RequestKey == dto.RequestKey);
        if (existing != null) { if (existing.IdTable != tableId) throw new InvalidOperationException("Mã yêu cầu đã dùng ở bàn khác."); return existing; }
        var table = await db.TableFoods.Include(t => t.Area).SingleOrDefaultAsync(t => t.Id == tableId && t.IsActive);
        if (table == null || table.Area?.IsActive == false) throw new InvalidOperationException("Phòng/bàn hiện không hoạt động.");
        if (await db.QrOrderRequests.CountAsync(r => r.IdTable == tableId && r.Status == "Pending" && r.ExpiresAt > DateTime.UtcNow) >= 20) throw new InvalidOperationException("Bàn đang có nhiều yêu cầu chờ. Vui lòng gọi nhân viên.");
        var items = await Quote(dto.Items); var total = items.Sum(i => i.Count * i.Price);
        if (total != dto.ExpectedTotal) throw new InvalidOperationException("Giá món vừa thay đổi. Tải lại thực đơn trước khi gửi.");
        var row = new QrOrderRequest { RequestKey = dto.RequestKey, IdTable = tableId, TableName = table.Name, ItemsJson = JsonSerializer.Serialize(items), Total = total, Note = (dto.Note ?? "").Trim() };
        db.QrOrderRequests.Add(row); await db.SaveChangesAsync(); await tx.CommitAsync(); return row;
    }
    public async Task<QrOrderRequest> Decide(int id, QrDecision dto, string user)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var row = await db.QrOrderRequests.SingleOrDefaultAsync(r => r.Id == id) ?? throw new InvalidOperationException("Không tìm thấy yêu cầu.");
        if (row.Status != "Pending") {
            if (row.Status == (dto.Accept ? "Accepted" : "Rejected")) return row;
            throw new InvalidOperationException("Yêu cầu đã được xử lý. Vui lòng làm mới danh sách.");
        }
        if (row.ExpiresAt <= DateTime.UtcNow) throw new InvalidOperationException("Yêu cầu đã hết hạn. Nhờ khách gửi lại hoặc gọi món trực tiếp.");
        if (!dto.Accept && string.IsNullOrWhiteSpace(dto.Reason)) throw new InvalidOperationException("Nhập lý do từ chối để khách biết.");
        if (dto.Accept) {
            var items = Items(row);
            var current = await Quote(items.Select(i => new QrItemWrite { IdFood = i.IdFood, IdVariant = i.IdVariant, Count = i.Count, Toppings = i.Toppings }).ToList());
            if (items.Where((i, n) => i.Price != current[n].Price || i.Options != current[n].Options || i.Name != current[n].Name).Any()) throw new InvalidOperationException("Món hoặc giá đã thay đổi. Từ chối và nhờ khách gửi lại để xác nhận giá mới.");
            foreach (var item in items) row.IdBill = await new OrderFlow(db).Add(new() { IdTable = row.IdTable, IdFood = item.IdFood, IdVariant = item.IdVariant, Count = item.Count, Toppings = item.Toppings, Note = row.Note }, user);
        }
        row.Status = dto.Accept ? "Accepted" : "Rejected"; row.DecidedAt = DateTime.UtcNow; row.DecidedBy = user; row.Reason = (dto.Reason ?? "").Trim();
        await db.SaveChangesAsync(); await tx.CommitAsync(); return row;
    }
}
