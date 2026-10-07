using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[ApiController, Route("api/[controller]")]
public class FoodController(AppDbContext db) : ControllerBase
{
    private MenuCatalog Catalog => new(db);
    [HttpGet] public async Task<IActionResult> GetAll()
    {
        var held = await new StockReservations(db).Held();
        return Ok((await Catalog.Query().ToListAsync()).Select(f => MenuCatalog.Dto(f, held)));
    }
    [HttpPost] public async Task<IActionResult> Create(CreateFoodDto dto)
    {
        var error = await Catalog.Validate(dto); if (error != null) return BadRequest(error);
        await using var tx = await db.Database.BeginTransactionAsync();
        var food = new Food(); db.Foods.Add(food); await Catalog.Save(food, dto); await tx.CommitAsync();
        return Ok(new { id = food.Id, message = "Đã thêm món." });
    }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, UpdateFoodDto dto)
    {
        var food = await Catalog.Query().FirstOrDefaultAsync(f => f.Id == id); if (food == null) return NotFound();
        var error = await Catalog.Validate(dto, id); if (error != null) return BadRequest(error);
        await using var tx = await db.Database.BeginTransactionAsync(); await Catalog.Save(food, dto); await tx.CommitAsync(); return Ok();
    }
    [HttpPut("{id}/status")] public async Task<IActionResult> Status(int id, FoodStatusDto dto)
    { var food = await db.Foods.FindAsync(id); if (food == null) return NotFound(); food.IsActive = dto.IsActive; await db.SaveChangesAsync(); return Ok(); }
    [HttpPut("{id}/favorite")] public async Task<IActionResult> Favorite(int id, FavoriteDto dto)
    { var food = await db.Foods.FindAsync(id); if (food == null) return NotFound(); food.IsFavorite = dto.IsFavorite; await db.SaveChangesAsync(); return Ok(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id)
    {
        var food = await Catalog.Query().FirstOrDefaultAsync(f => f.Id == id); if (food == null) return NotFound();
        if (await db.BillInfos.AnyAsync(i => i.IdFood == id) || await db.KitchenOrderDetails.AnyAsync(i => i.IdFood == id) || await db.FoodToppings.AnyAsync(t => t.IdTopping == id))
        { food.IsActive = false; await db.SaveChangesAsync(); return Ok(new { message = "Món đã có lịch sử hoặc đang dùng làm topping: đã ngừng bán để giữ dữ liệu." }); }
        await using var tx = await db.Database.BeginTransactionAsync();
        db.Recipes.RemoveRange(food.Recipes); db.FoodToppings.RemoveRange(food.AllowedToppings); await db.SaveChangesAsync();
        db.FoodVariants.RemoveRange(food.Variants); db.Foods.Remove(food); await db.SaveChangesAsync(); await tx.CommitAsync();
        return Ok(new { message = "Đã xóa món." });
    }
    [HttpPost("import")] public async Task<IActionResult> Import(FoodImportDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0 || dto.Items.Count > 500) return BadRequest("Mỗi lần nhập từ 1 đến 500 món.");
        var errors = new List<object>(); var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < dto.Items.Count; i++)
        { var item = dto.Items[i]; var error = await Catalog.Validate(item);
          if (item != null && !string.IsNullOrWhiteSpace(item.Code) && !codes.Add(item.Code.Trim())) error = "Mã món lặp trong file.";
          if (error != null) errors.Add(new { row = i + 2, message = error }); }
        if (dto.Preview || errors.Count > 0) return Ok(new { valid = errors.Count == 0, errors, count = dto.Items.Count });
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var item in dto.Items) { var food = new Food(); db.Foods.Add(food); await Catalog.Save(food, item); }
        await tx.CommitAsync(); return Ok(new { valid = true, count = dto.Items.Count, errors });
    }
}
