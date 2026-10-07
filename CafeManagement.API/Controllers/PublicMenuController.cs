using CafeManagement.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.API.DTOs;
using CafeManagement.API.Services;
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
        return Ok(new { TableName = table.Name, AreaName = table.Area?.Name, Foods = foods.Select(f => new { f.Id, f.Name, f.Price, f.Description, f.ImageUrl, Category = f.Category.Name, Variants = f.Variants.Where(v => v.IsActive).Select(v => new { v.Id, v.Name, v.Price }), Toppings = f.AllowedToppings.Where(t => t.Topping.IsActive && t.Topping.IsTopping).Select(t => new { t.Topping.Id, t.Topping.Name, t.Topping.Price }) }) });
    }
    [HttpPost("{tableId}/requests"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("qr-submit")]
    public async Task<IActionResult> Submit(int tableId, QrOrderWrite dto)
    {
        try { return Ok(QrOrdering.PublicDto(await new QrOrdering(db).Submit(tableId, dto))); }
        catch (InvalidOperationException e) { return BadRequest(e.Message); }
    }
    // The random request key is the guest's private tracking capability.
    [HttpGet("{tableId}/requests/{requestKey}")]
    public async Task<IActionResult> Track(int tableId, string requestKey)
    {
        if (!Guid.TryParseExact(requestKey, "D", out _)) return NotFound();
        var row = await db.QrOrderRequests.AsNoTracking().SingleOrDefaultAsync(r => r.IdTable == tableId && r.RequestKey == requestKey);
        return row == null ? NotFound() : Ok(QrOrdering.PublicDto(row));
    }
}
