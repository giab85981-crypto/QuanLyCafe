using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[Microsoft.AspNetCore.Authorization.Authorize, ApiController, Route("api/[controller]")]
public class KitchenController(AppDbContext db) : ControllerBase
{
    [HttpGet("pending-orders")]
    public async Task<IActionResult> Pending(bool completed = false) => Ok(await db.KitchenOrders.Where(o => (completed ? o.Status == "Completed" && o.CreatedAt >= DateTime.Today && o.CreatedAt < DateTime.Today.AddDays(1) : o.Status != "Completed" && o.Status != "Cancelled") && (o.Bill.Status == 0 || o.Bill.Status == 1) && o.KitchenOrderDetails.Any(d => d.Count > d.CancelledCount))
        .OrderBy(o => o.CreatedAt).Select(o => new { o.Id, o.IdBill, TableName = o.Bill.TableNameSnapshot != "" ? o.Bill.TableNameSnapshot : o.Bill.TableFood != null ? o.Bill.TableFood.Name : "Mang về", o.Bill.OrderType, o.Bill.Note, o.CreatedAt, o.Status,
            Details = o.KitchenOrderDetails.Where(d => d.Count > d.CancelledCount).Select(d => new { d.Id, d.IdFood, d.IdBillInfo, FoodName = d.BillInfo != null && d.BillInfo.FoodNameSnapshot != "" ? d.BillInfo.FoodNameSnapshot : d.Food.Name, Count = d.Count - d.CancelledCount, d.OptionLabel, d.Status }).ToList() }).ToListAsync());
    [HttpPost("send-order")]
    public async Task<IActionResult> Send(CreateKitchenOrderDto dto)
    {
        try { var id = await new OrderFlow(db).Send(dto.IdBill, HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system"); return Ok(new { idKitchenOrder = id, message = id == null ? "Không có món mới cần báo bếp." : "Đã báo bếp và xuất nguyên liệu." }); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
    [HttpPut("{id}/status")]
    public async Task<IActionResult> Status(int id, UpdateStatusDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var order = await db.KitchenOrders.Include(o => o.Bill).Include(o => o.KitchenOrderDetails).FirstOrDefaultAsync(o => o.Id == id); if (order == null) return NotFound();
        if (order.Bill.Status != 0 && order.Bill.Status != 1) return BadRequest("Hóa đơn đã hủy/đóng.");
        if (!order.KitchenOrderDetails.Any(d => d.Count > d.CancelledCount)) return BadRequest("Phiếu không còn món cần chế biến.");
        if (!((order.Status == "Pending" && dto.Status == "Cooking") || (order.Status == "Cooking" && dto.Status == "Completed"))) return BadRequest("Trạng thái cần đi từ Chờ → Đang làm → Hoàn thành.");
        order.Status = dto.Status; foreach (var detail in order.KitchenOrderDetails.Where(d => d.Count > d.CancelledCount)) detail.Status = dto.Status;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
}
