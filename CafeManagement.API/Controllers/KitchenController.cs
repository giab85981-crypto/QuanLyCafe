using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KitchenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KitchenController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách order chưa hoàn thành cho màn hình Bếp
        [HttpGet("pending-orders")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var orders = await _context.KitchenOrders
                .Include(ko => ko.Bill)
                .ThenInclude(b => b.TableFood)
                .Include(ko => ko.KitchenOrderDetails)
                .ThenInclude(kod => kod.Food)
                .Where(ko => ko.Status != "Completed")
                .OrderBy(ko => ko.CreatedAt)
                .Select(ko => new KitchenOrderDto
                {
                    Id = ko.Id,
                    IdBill = ko.IdBill,
                    TableName = ko.Bill.TableFood.Name,
                    CreatedAt = ko.CreatedAt,
                    Status = ko.Status,
                    Details = ko.KitchenOrderDetails.Select(kod => new KitchenOrderDetailDto
                    {
                        Id = kod.Id,
                        IdFood = kod.IdFood,
                        FoodName = kod.Food.Name,
                        Count = kod.Count,
                        Status = kod.Status
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // Gửi thông báo order món xuống Bếp
        [HttpPost("send-order")]
        public async Task<IActionResult> SendOrder([FromBody] CreateKitchenOrderDto dto)
        {
            var bill = await _context.Bills.FindAsync(dto.IdBill);
            if (bill == null) return NotFound("Không tìm thấy hóa đơn!");

            var kitchenOrder = new KitchenOrder
            {
                IdBill = dto.IdBill,
                CreatedAt = DateTime.Now,
                Status = "Pending"
            };

            foreach (var item in dto.Items)
            {
                kitchenOrder.KitchenOrderDetails.Add(new KitchenOrderDetail
                {
                    IdFood = item.IdFood,
                    Count = item.Count,
                    Status = "Pending"
                });
            }

            _context.KitchenOrders.Add(kitchenOrder);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gửi báo bếp thành công!", idKitchenOrder = kitchenOrder.Id });
        }

        // Cập nhật trạng thái chế biến (Pending -> Cooking -> Completed)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.KitchenOrders.FindAsync(id);
            if (order == null) return NotFound("Không tìm thấy phiếu bếp!");

            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái phiếu bếp thành công!" });
        }
    }
}