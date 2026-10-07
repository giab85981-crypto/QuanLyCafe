using System.Data;
using System.Text.Json;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public record IngredientPortion(int IdIngredient, double Amount);
public class OrderFlow(AppDbContext db)
{
    public static List<IngredientPortion> Portions(BillInfo line) => JsonSerializer.Deserialize<List<IngredientPortion>>(line.IngredientsJson) ?? [];
    public async Task<int> Add(AddFoodToBillDto dto, string user = "system")
    {
        if (dto.Count < 1 || dto.Count > 1000) throw new InvalidOperationException("Số lượng phải từ 1 đến 1000.");
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        await new StockReservations(db).Lock();
        Bill? selectedBill = null; TableFood? table = null;
        if (dto.IdBill.HasValue) {
            selectedBill = await db.Bills.Include(b => b.BillInfos).Include(b => b.TableFood).SingleOrDefaultAsync(b => b.Id == dto.IdBill && b.Status == 0)
                ?? throw new InvalidOperationException("Đơn đã đóng hoặc không tồn tại.");
            if (dto.IdTable.HasValue && dto.IdTable != selectedBill.IdTable) throw new InvalidOperationException("Bàn không khớp với đơn.");
            table = selectedBill.TableFood;
        } else {
            if (!dto.IdTable.HasValue) throw new InvalidOperationException("Chọn bàn hoặc đơn mang về.");
            table = await db.TableFoods.FindAsync(dto.IdTable) ?? throw new InvalidOperationException("Không tìm thấy bàn.");
        }
        if (table != null && (!table.IsActive || (table.IdArea.HasValue && !await db.Areas.AnyAsync(a => a.Id == table.IdArea && a.IsActive)))) throw new InvalidOperationException("Bàn hoặc khu vực đã ngừng hoạt động.");
        var food = await new MenuCatalog(db).Query().FirstOrDefaultAsync(f => f.Id == dto.IdFood && f.IsActive && !f.IsTopping)
            ?? throw new InvalidOperationException("Món không tồn tại hoặc đã ngừng bán.");
        var variant = food.Variants.FirstOrDefault(v => v.Id == dto.IdVariant && v.IsActive);
        if ((food.Variants.Any(v => v.IsActive) && variant == null) || (dto.IdVariant.HasValue && variant == null)) throw new InvalidOperationException("Vui lòng chọn size đang bán.");
        if (dto.Toppings.Count > 30 || dto.Toppings.Any(t => t.Count < 1 || t.Count > 20) || dto.Toppings.Select(t => t.IdFood).Distinct().Count() != dto.Toppings.Count) throw new InvalidOperationException("Topping không hợp lệ (tối đa 20 phần mỗi loại).");
        var selected = new List<object>(); var recipes = new List<IngredientPortion>(); var labels = new List<string>();
        var baseRecipe = variant != null ? variant.Recipes : food.Recipes.Where(r => r.IdVariant == null);
        recipes.AddRange(baseRecipe.Select(r => new IngredientPortion(r.IdIngredient, r.Amount)));
        decimal price = variant?.Price ?? food.Price; double cost = MenuCatalog.Cost(baseRecipe, variant?.CostPrice ?? food.CostPrice);
        if (variant != null) labels.Add(variant.Name);
        foreach (var choice in dto.Toppings.OrderBy(t => t.IdFood))
        {
            if (!food.AllowedToppings.Any(t => t.IdTopping == choice.IdFood)) throw new InvalidOperationException("Topping chưa được gắn với món.");
            var topping = await new MenuCatalog(db).Query().FirstOrDefaultAsync(f => f.Id == choice.IdFood && f.IsTopping && f.IsActive)
                ?? throw new InvalidOperationException("Topping đã ngừng bán.");
            var recipe = topping.Recipes.Where(r => r.IdVariant == null);
            price += topping.Price * choice.Count; cost += MenuCatalog.Cost(recipe, topping.CostPrice) * choice.Count;
            recipes.AddRange(recipe.Select(r => new IngredientPortion(r.IdIngredient, r.Amount * choice.Count)));
            labels.Add($"{topping.Name} ×{choice.Count}"); selected.Add(new { topping.Id, topping.Name, topping.Price, choice.Count });
        }
        if (dto.Note?.Length > 300) throw new InvalidOperationException("Ghi chú tối đa 300 ký tự.");
        if (!string.IsNullOrWhiteSpace(dto.Note)) labels.Add(dto.Note.Trim());
        var ingredientsJson = JsonSerializer.Serialize(recipes.GroupBy(r => r.IdIngredient).Select(g => new IngredientPortion(g.Key, g.Sum(r => r.Amount))).OrderBy(r => r.IdIngredient));
        await new StockReservations(db).Check(recipes.GroupBy(r => r.IdIngredient).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount) * dto.Count));
        var optionsJson = JsonSerializer.Serialize(selected); var label = string.Join(" · ", labels);
        var bill = selectedBill ?? await db.Bills.Include(b => b.BillInfos).FirstOrDefaultAsync(b => b.IdTable == dto.IdTable && b.Status == 0 && b.OrderType == "DineIn");
        if (bill == null) { bill = new Bill { IdTable = dto.IdTable, TableNameSnapshot = table!.Name, CreatedBy = user }; db.Bills.Add(bill); }
        var line = bill.BillInfos.FirstOrDefault(i => i.IdFood == food.Id && i.IdVariant == variant?.Id && i.UnitPrice == price && i.CostPrice == cost && i.OptionsJson == optionsJson && i.IngredientsJson == ingredientsJson && i.OptionLabel == label);
        if (line == null) bill.BillInfos.Add(new BillInfo { Food = food, FoodNameSnapshot = food.Name, IdVariant = variant?.Id, Count = dto.Count, UnitPrice = price, CostPrice = cost, OptionLabel = label, OptionsJson = optionsJson, IngredientsJson = ingredientsJson });
        else { if (line.Count + dto.Count > 1000) throw new InvalidOperationException("Một dòng tối đa 1000 phần."); line.Count += dto.Count; }
        if (table != null) table.Status = "Có người"; await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return bill.Id;
    }
    public async Task<int?> Send(int billId, string user = "system")
    {
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        await new StockReservations(db).Lock();
        var bill = await db.Bills.Include(b => b.BillInfos).FirstOrDefaultAsync(b => b.Id == billId && b.Status == 0) ?? throw new InvalidOperationException("Hóa đơn không còn phục vụ.");
        var lines = bill.BillInfos.Where(i => i.Count > i.SentCount).ToList();
        if (lines.Count == 0) return null;
        var needed = lines.SelectMany(i => Portions(i).Select(p => new IngredientPortion(p.IdIngredient, p.Amount * (i.Count - i.SentCount))))
            .GroupBy(p => p.IdIngredient).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
        await new StockReservations(db).Check(needed, billId);
        var ingredients = await db.Ingredients.Where(i => needed.Keys.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var order = new KitchenOrder { IdBill = billId }; db.KitchenOrders.Add(order);
        foreach (var line in lines)
        {
            int delta = line.Count - line.SentCount;
            var detail = new KitchenOrderDetail { KitchenOrder = order, IdBillInfo = line.Id, IdFood = line.IdFood, Count = delta, OptionLabel = line.OptionLabel };
            order.KitchenOrderDetails.Add(detail);
            foreach (var portion in Portions(line))
            {
                await new StockLots(db).Consume(ingredients[portion.IdIngredient], portion.Amount * delta, "Kitchen", $"Báo bếp hóa đơn #{billId}", user: user, kitchen: detail);
            }
            line.SentCount = line.Count;
        }
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return order.Id;
    }
    public async Task Cancel(int lineId, CancelBillItemDto dto, string user = "system")
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var line = await db.BillInfos.Include(i => i.Bill).FirstOrDefaultAsync(i => i.Id == lineId && i.Bill.Status == 0) ?? throw new InvalidOperationException("Không tìm thấy dòng món đang phục vụ.");
        if (dto.Count < 1 || dto.Count > line.Count) throw new InvalidOperationException("Số lượng hủy không hợp lệ.");
        var sentCancel = Math.Max(0, dto.Count - (line.Count - line.SentCount));
        if (sentCancel > 0 && (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 300)) throw new InvalidOperationException("Món đã báo bếp cần lý do hủy (tối đa 300 ký tự).");
        var remaining = sentCancel;
        var details = await db.KitchenOrderDetails.Include(d => d.KitchenOrder).Where(d => d.IdBillInfo == lineId && d.Count > d.CancelledCount).OrderByDescending(d => d.Id).ToListAsync();
        foreach (var detail in details)
        {
            if (remaining == 0) break;
            int count = Math.Min(remaining, detail.Count - detail.CancelledCount);
            await new StockLots(db).CancelKitchen(detail, count, detail.Status == "Pending", dto.Reason.Trim(), user);
            detail.CancellationNote = dto.Reason.Trim();
            detail.CancelledCount += count; remaining -= count;
            if (detail.CancelledCount == detail.Count) detail.Status = "Cancelled";
        }
        if (remaining > 0) throw new InvalidOperationException("Lịch sử báo bếp cũ thiếu dòng món. Vui lòng xử lý phiếu bếp trước.");
        line.Count -= dto.Count; line.SentCount -= sentCancel;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}



