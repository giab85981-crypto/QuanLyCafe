using System.Data;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class WarehouseCatalog(AppDbContext db)
{
    public async Task<string?> Validate(WarehouseIngredientWrite dto, int? id = null)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100 || string.IsNullOrWhiteSpace(dto.Unit) || dto.Unit.Length > 20) return "Tên hoặc đơn vị nguyên liệu không hợp lệ.";
        if (dto.Code == null || dto.Code.Length > 100 || dto.Units == null || dto.Units.Count > 20) return "Mã hoặc đơn vị quy đổi không hợp lệ.";
        if (!double.IsFinite(dto.Quantity) || dto.Quantity < 0 || dto.Quantity > 1000000000 || !double.IsFinite(dto.MinQuantity) || dto.MinQuantity < 0 || dto.MinQuantity > 1000000000 || dto.UnitCost < 0 || dto.UnitCost > 1000000000) return "Tồn kho, định mức hoặc giá vốn không hợp lệ.";
        if (await db.Ingredients.AnyAsync(i => i.Id != id && i.Name == dto.Name.Trim())) return "Tên nguyên liệu đã tồn tại.";
        if (dto.Code.Trim() != "" && await db.Ingredients.AnyAsync(i => i.Id != id && i.Code == dto.Code.Trim())) return "Mã nguyên liệu đã tồn tại.";
        if (dto.IdGroup.HasValue && !await db.IngredientGroups.AnyAsync(g => g.Id == dto.IdGroup)) return "Nhóm nguyên liệu không tồn tại.";
        if (dto.Units.Any(u => u == null || string.IsNullOrWhiteSpace(u.Name) || u.Name.Length > 30 || u.Name.Trim().Equals(dto.Unit.Trim(), StringComparison.OrdinalIgnoreCase) || u.Factor <= 0 || u.Factor > 1000000 || decimal.Round(u.Factor, 6) != u.Factor) || dto.Units.Select(u => u.Name.Trim().ToLowerInvariant()).Distinct().Count() != dto.Units.Count) return "Đơn vị quy đổi cần tên riêng và hệ số dương, tối đa 6 số lẻ; không trùng đơn vị cơ sở.";
        if (id.HasValue)
        {
            var current = await db.Ingredients.FindAsync(id);
            if (current != null && current.Unit != dto.Unit.Trim() && (await db.Recipes.AnyAsync(r => r.IdIngredient == id) || await db.StockMovements.AnyAsync(m => m.IdIngredient == id))) return "Nguyên liệu đã có công thức/giao dịch: không đổi đơn vị cơ sở.";
        }
        return null;
    }
    public async Task<Ingredient> Save(WarehouseIngredientWrite dto, int? id, string user)
    {
        await using var transaction = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        await new StockReservations(db).Lock();
        var error = await Validate(dto, id); if (error != null) throw new InvalidOperationException(error);
        var ingredient = id.HasValue ? await db.Ingredients.Include(i => i.Units).FirstOrDefaultAsync(i => i.Id == id) : new Ingredient();
        if (ingredient == null) throw new InvalidOperationException("Nguyên liệu không tồn tại.");
        if (id.HasValue && !dto.IsActive && (await new StockReservations(db).Held()).GetValueOrDefault(id.Value) > 0)
            throw new InvalidOperationException("Nguyên liệu đang được giữ cho đơn chưa báo bếp. Hãy xử lý các đơn trước khi ngừng sử dụng.");
        var quantity = dto.Quantity;
        ingredient.Name = dto.Name.Trim(); ingredient.Unit = dto.Unit.Trim(); ingredient.Code = dto.Code.Trim(); ingredient.IdGroup = dto.IdGroup; ingredient.IsActive = dto.IsActive; ingredient.MinQuantity = dto.MinQuantity;
        if (!id.HasValue) { ingredient.Quantity = 0; ingredient.UnitCost = dto.UnitCost; db.Ingredients.Add(ingredient); }
        // Keep unit IDs stable for forms already open; posted receipts retain conversion snapshots.
        foreach (var previous in ingredient.Units.Where(u => dto.Units.All(x => !x.Name.Trim().Equals(u.Name, StringComparison.OrdinalIgnoreCase))).ToList()) db.IngredientUnits.Remove(previous);
        foreach (var input in dto.Units)
        {
            var unit = ingredient.Units.FirstOrDefault(u => u.Name.Equals(input.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (unit == null) { unit = new IngredientUnit { Ingredient = ingredient }; ingredient.Units.Add(unit); }
            unit.Name = input.Name.Trim(); unit.Factor = input.Factor;
        }
        await db.SaveChangesAsync();
        if (ingredient.Code == "") { ingredient.Code = $"NL{ingredient.Id:D4}"; while (await db.Ingredients.AnyAsync(i => i.Id != ingredient.Id && i.Code == ingredient.Code)) ingredient.Code += "A"; await db.SaveChangesAsync(); }
        if (!id.HasValue && quantity > 0) await new StockLots(db).Add(ingredient, quantity, dto.UnitCost, "Opening", "Tồn ban đầu nguyên liệu", user, code: "TON-DAU");
        if (transaction != null) await transaction.CommitAsync();
        return ingredient;
    }
}
