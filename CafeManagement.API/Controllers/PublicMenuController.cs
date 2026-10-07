using CafeManagement.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[ApiController, Route("api/[controller]")]
public class PublicMenuController(AppDbContext db) : ControllerBase
{
    [HttpGet("{tableId}")]
    public async Task<IActionResult> Get(int tableId)
    {
        var table = await db.TableFoods.Include(t => t.Area).FirstOrDefaultAsync(t => t.Id == tableId && t.IsActive);
        if (table == null || table.Area?.IsActive == false) return NotFound("Phòng/bàn hiện không hoạt động.");
        var foods = await db.Foods.Include(f => f.Category).Include(f => f.Variants).Include(f => f.AllowedToppings).ThenInclude(t => t.Topping).Where(f => f.IsActive && !f.IsTopping).AsSplitQuery().OrderBy(f => f.Name).ToListAsync();
        return Ok(new { TableName = table.Name, AreaName = table.Area?.Name, Foods = foods.Select(f => new { f.Id, f.Name, f.Price, f.Description, f.ImageUrl, Category = f.Category.Name, Variants = f.Variants.Where(v => v.IsActive).Select(v => new { v.Name, v.Price }), Toppings = f.AllowedToppings.Where(t => t.Topping.IsActive).Select(t => new { t.Topping.Name, t.Topping.Price }) }) });
    }
}
