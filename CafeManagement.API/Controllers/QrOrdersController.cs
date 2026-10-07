using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[ApiController, Authorize, Route("api/[controller]")]
public class QrOrdersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rows = await db.QrOrderRequests.AsNoTracking().Where(r => r.Status == "Pending" && r.ExpiresAt > DateTime.UtcNow).OrderBy(r => r.CreatedAt).Take(200).ToListAsync();
        return Ok(rows.Select(QrOrdering.PublicDto));
    }
    [HttpPost("{id}/decision")]
    public async Task<IActionResult> Decision(int id, QrDecision dto)
    {
        try { var row = await new QrOrdering(db).Decide(id, dto, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value); return Ok(new { request = QrOrdering.PublicDto(row), row.IdBill, row.IdTable }); }
        catch (InvalidOperationException e) { return BadRequest(e.Message); }
    }
}
