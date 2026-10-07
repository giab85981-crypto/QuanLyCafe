using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CafeManagement.API.Services;

public class StockReservations(AppDbContext db)
{
    // Unsent quantities and their frozen recipes are the durable reservation ledger.
    // A database-scoped transaction lock also coordinates different API processes.
    public async Task Lock()
    {
        var tx = db.Database.CurrentTransaction ?? throw new InvalidOperationException("Giữ nguyên liệu cần giao dịch kho.");
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = N'Cafe.StockReservations', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000; SELECT @result;";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) < 0)
            throw new InvalidOperationException("Kho đang được cập nhật. Vui lòng thử lại.");
    }

    public async Task<Dictionary<int, double>> Held(int? excludeBill = null)
    {
        var lines = await db.BillInfos.AsNoTracking().Where(i => i.Bill.Status == 0 && i.Count > i.SentCount && (!excludeBill.HasValue || i.IdBill != excludeBill))
            .Select(i => new { i.Count, i.SentCount, i.IngredientsJson }).ToListAsync();
        return lines.SelectMany(i => (System.Text.Json.JsonSerializer.Deserialize<List<IngredientPortion>>(i.IngredientsJson) ?? [])
                .Select(p => new IngredientPortion(p.IdIngredient, p.Amount * (i.Count - i.SentCount))))
            .GroupBy(p => p.IdIngredient).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
    }

    public static double Usable(Ingredient ingredient) => ingredient.Lots.Count == 0 ? ingredient.Quantity : ingredient.Lots.Where(StockLots.Usable).Sum(l => l.Quantity);

    public async Task Check(IReadOnlyDictionary<int, double> needed, int? excludeBill = null)
    {
        var held = await Held(excludeBill);
        var ingredients = await db.Ingredients.Include(i => i.Lots).Where(i => needed.Keys.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        foreach (var (id, amount) in needed)
        {
            ingredients.TryGetValue(id, out var ingredient);
            var available = ingredient == null || !ingredient.IsActive ? 0 : Math.Max(0, Usable(ingredient) - held.GetValueOrDefault(id));
            if (ingredient == null || !ingredient.IsActive || available + 0.0000001 < amount)
                throw new InvalidOperationException($"Không đủ nguyên liệu: {ingredient?.Name ?? id.ToString()} (cần {amount:0.###}, còn nhận {available:0.###} {ingredient?.Unit}; đã giữ cho các đơn khác {held.GetValueOrDefault(id):0.###}).");
        }
    }
}
