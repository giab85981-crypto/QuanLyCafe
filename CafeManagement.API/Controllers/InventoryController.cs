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
public class InventoryController(AppDbContext db) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    [HttpGet("ingredients")]
    public async Task<IActionResult> GetIngredients() => Ok(await db.Ingredients.OrderBy(i => i.Name).Select(i => new { i.Id, i.Name, i.Quantity, i.MinQuantity, i.Unit, i.UnitCost, i.IsActive }).ToListAsync());
    [HttpGet("movements")]
    public async Task<IActionResult> Movements() => Ok(await db.StockMovements.OrderByDescending(m => m.Id).Take(200).Select(m => new { m.Id, IngredientName = m.Ingredient.Name, Unit = m.Ingredient.Unit, m.Quantity, m.Kind, m.Note, m.CreatedAt }).ToListAsync());
    [HttpPost("ingredients")] public async Task<IActionResult> CreateIngredient(IngredientWriteDto dto) => await Write(null, dto);
    [HttpPut("ingredients/{id}")] public async Task<IActionResult> EditIngredient(int id, IngredientWriteDto dto) => await Write(id, dto);
    private async Task<IActionResult> Write(int? id, IngredientWriteDto dto)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await new StockReservations(db).Lock();
            var existing = id.HasValue ? await db.Ingredients.Include(i => i.Units).FirstOrDefaultAsync(i => i.Id == id) : null;
            if (id.HasValue && existing == null) return NotFound();
            var input = new WarehouseIngredientWrite { Name = dto.Name, Unit = dto.Unit, Quantity = dto.Quantity, MinQuantity = dto.MinQuantity, UnitCost = dto.UnitCost,
                Code = existing?.Code ?? "", IdGroup = existing?.IdGroup, IsActive = existing?.IsActive ?? true, Units = existing?.Units.Select(u => new IngredientUnitWrite { Name = u.Name, Factor = u.Factor }).ToList() ?? new() };
            var result = await new WarehouseCatalog(db).Save(input, id, Actor); await tx.CommitAsync(); return Ok(new { result.Id });
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
    [HttpPost("import")]
    public async Task<IActionResult> ImportInventory(CreateImportReceiptDto dto)
    {
        try
        {
            var supplierId = dto.IdSupplier ?? (await db.Suppliers.FirstOrDefaultAsync(s => s.Name == dto.SupplierName))?.Id;
            if (!supplierId.HasValue) return BadRequest("Tạo/chọn nhà cung cấp trong Kho hàng trước khi nhập.");
            var input = new WarehouseImportWrite { IdSupplier = supplierId.Value, RequestKey = Guid.NewGuid().ToString("N"), Items = dto.Items.Select(i => new WarehouseImportLine { IdIngredient = i.IdIngredient, Quantity = i.Count, Price = i.Price }).ToList() };
            var id = await new WarehouseFlow(db).Import(input, Actor); return Ok(new { idReceipt = id, message = "Đã nhập kho; phiếu chưa ghi nhận thanh toán." });
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
    [HttpGet("recipe")]
    public async Task<IActionResult> GetRecipes(int? foodId) => Ok(await db.Foods.Where(f => !foodId.HasValue || f.Id == foodId).Select(f => new RecipeDto { IdFood = f.Id, FoodName = f.Name,
        Ingredients = f.Recipes.Where(r => r.IdVariant == null).Select(r => new RecipeItemDto { IdIngredient = r.IdIngredient, IngredientName = r.Ingredient.Name, Amount = r.Amount, Unit = r.Ingredient.Unit }).ToList() }).ToListAsync());
    [HttpPost("recipe")]
    public async Task<IActionResult> SaveRecipe(CreateRecipeDto dto)
    {
        if (!await db.Foods.AnyAsync(f => f.Id == dto.IdFood)) return NotFound();
        if (dto.Items.Any(r => !double.IsFinite(r.Amount) || r.Amount <= 0 || r.Amount > 1000000000) || dto.Items.Select(r => r.IdIngredient).Distinct().Count() != dto.Items.Count) return BadRequest("Định lượng phải dương, không lặp nguyên liệu.");
        var ids = dto.Items.Select(r => r.IdIngredient).ToList(); if (await db.Ingredients.CountAsync(i => ids.Contains(i.Id)) != ids.Count) return BadRequest("Nguyên liệu không tồn tại.");
        await using var tx = await db.Database.BeginTransactionAsync();
        db.Recipes.RemoveRange(await db.Recipes.Where(r => r.IdFood == dto.IdFood && r.IdVariant == null).ToListAsync());
        db.Recipes.AddRange(dto.Items.Select(r => new Recipe { IdFood = dto.IdFood, IdIngredient = r.IdIngredient, Amount = r.Amount }));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
}
