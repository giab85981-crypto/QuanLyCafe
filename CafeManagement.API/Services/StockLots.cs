using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class StockLots(AppDbContext db)
{
    public async Task<List<StockLot>> Ensure(Ingredient ingredient)
    {
        var lots = await db.StockLots.Where(l => l.IdIngredient == ingredient.Id).ToListAsync();
        if (lots.Count == 0 && ingredient.Quantity > 0)
        {
            var lot = new StockLot { IdIngredient = ingredient.Id, Code = "TON-DAU", Quantity = ingredient.Quantity, UnitCost = ingredient.UnitCost };
            db.StockLots.Add(lot); await db.SaveChangesAsync(); lots.Add(lot);
        }
        if (Math.Abs(lots.Sum(l => l.Quantity) - ingredient.Quantity) > 0.00001) throw new InvalidOperationException($"Tồn theo lô của {ingredient.Name} không khớp. Cần kiểm tra dữ liệu trước khi thao tác.");
        return lots;
    }
    public static bool Usable(StockLot lot) => lot.ExpiryDate == null || lot.ExpiryDate.Value.Date >= DateTime.Today;
    public async Task Consume(Ingredient ingredient, double amount, string kind, string note, string user = "", KitchenOrderDetail? kitchen = null, WarehouseDocument? document = null, int? lotId = null, bool allowExpired = false)
    {
        var lots = await Ensure(ingredient);
        var candidates = lots.Where(l => l.Quantity > 0 && (!lotId.HasValue || l.Id == lotId) && (allowExpired || Usable(l)))
            .OrderBy(l => l.ExpiryDate ?? DateTime.MaxValue).ThenBy(l => l.CreatedAt).ThenBy(l => l.Id).ToList();
        if (candidates.Sum(l => l.Quantity) + 0.0000001 < amount) throw new InvalidOperationException($"Không đủ tồn khả dụng của {ingredient.Name}; nguyên liệu hết hạn không được dùng bán hàng.");
        if (kind != "Kitchen")
        {
            // Expired disposal remains possible, but usable stock promised to orders is protected.
            double remainingToTake = amount, usableToTake = 0;
            foreach (var lot in candidates)
            {
                var take = Math.Min(remainingToTake, lot.Quantity);
                if (Usable(lot)) usableToTake += take;
                remainingToTake -= take;
                if (remainingToTake <= 0.00000001) break;
            }
            var held = (await new StockReservations(db).Held()).GetValueOrDefault(ingredient.Id);
            if (usableToTake > 0 && lots.Where(Usable).Sum(l => l.Quantity) - usableToTake + 0.0000001 < held)
                throw new InvalidOperationException($"{ingredient.Name} đang giữ {held:0.###} {ingredient.Unit} cho đơn chưa báo bếp. Không xuất/hủy/kiểm kê giảm vào phần đã giữ; hãy xử lý các đơn trước.");
        }
        double remaining = amount;
        foreach (var lot in candidates)
        {
            if (remaining < 0.00000001) break;
            var take = Math.Min(remaining, lot.Quantity); lot.Quantity = Math.Max(0, lot.Quantity - take); ingredient.Quantity = Math.Max(0, ingredient.Quantity - take); remaining -= take;
            db.StockMovements.Add(new StockMovement { Ingredient = ingredient, Lot = lot, KitchenDetail = kitchen, Document = document, Quantity = -take, UnitCost = lot.UnitCost, Kind = kind, Note = note, CreatedBy = user });
        }
        Average(ingredient, lots);
    }
    public static void Average(Ingredient ingredient, IEnumerable<StockLot> lots)
    {
        var positive = lots.Where(l => l.Quantity > 0).ToList(); var quantity = positive.Sum(l => l.Quantity);
        if (quantity > 0) ingredient.UnitCost = Math.Round(positive.Sum(l => (decimal)l.Quantity * l.UnitCost) / (decimal)quantity, 4);
    }
    public async Task<StockLot> Add(Ingredient ingredient, double amount, decimal unitCost, string kind, string note, string user = "", WarehouseDocument? document = null, ImportReceipt? receipt = null, string code = "", DateTime? expiry = null)
    {
        var lots = await Ensure(ingredient);
        if (ingredient.Quantity + amount > 1000000000) throw new InvalidOperationException("Tổng tồn vượt giới hạn cho phép.");
        var lot = new StockLot { Ingredient = ingredient, Quantity = amount, UnitCost = unitCost, Code = string.IsNullOrWhiteSpace(code) ? $"LO-{DateTime.Now:yyyyMMddHHmmss}" : code.Trim(), ExpiryDate = expiry?.Date, ImportReceipt = receipt };
        db.StockLots.Add(lot); lots.Add(lot); ingredient.Quantity += amount; Average(ingredient, lots);
        db.StockMovements.Add(new StockMovement { Ingredient = ingredient, Lot = lot, Document = document, ImportReceipt = receipt, Quantity = amount, UnitCost = unitCost, Kind = kind, Note = note, CreatedBy = user });
        await db.SaveChangesAsync(); return lot;
    }
    public async Task CancelKitchen(KitchenOrderDetail detail, int count, bool refund, string note, string user = "system")
    {
        var sources = await db.StockMovements.Include(m => m.Ingredient).Include(m => m.Lot).Where(m => m.IdKitchenDetail == detail.Id && m.Kind == "Kitchen").ToListAsync();
        foreach (var source in sources)
        {
            var amount = -source.Quantity * count / detail.Count;
            if (refund)
            {
                var lots = await Ensure(source.Ingredient);
                var lot = source.Lot ?? new StockLot { Ingredient = source.Ingredient, Code = "HOAN-KHO-CU", UnitCost = source.UnitCost ?? source.Ingredient.UnitCost };
                if (source.Lot == null) { db.StockLots.Add(lot); lots.Add(lot); }
                lot.Quantity += amount; source.Ingredient.Quantity += amount; Average(source.Ingredient, lots);
                db.StockMovements.Add(new StockMovement { Ingredient = source.Ingredient, Lot = lot, IdKitchenDetail = detail.Id, Quantity = amount, UnitCost = source.UnitCost, Kind = "Return", Note = note, CreatedBy = user });
            }
            else db.StockMovements.Add(new StockMovement { IdIngredient = source.IdIngredient, IdLot = source.IdLot, IdKitchenDetail = detail.Id, Quantity = -amount, UnitCost = source.UnitCost, Kind = "Waste", Note = note, CreatedBy = user });
        }
    }
}


