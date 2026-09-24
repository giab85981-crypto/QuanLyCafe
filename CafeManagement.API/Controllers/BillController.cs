using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BillController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BillController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("table/{tableId}")]
        public async Task<IActionResult> GetActiveBillByTable(int tableId)
        {
            var bill = await _context.Bills
                .Include(b => b.TableFood)
                .Include(b => b.BillInfos)
                .ThenInclude(bi => bi.Food)
                .FirstOrDefaultAsync(b => b.IdTable == tableId && b.Status == 0);

            if (bill == null) return NotFound(new { message = "Bàn hiện chưa có hóa đơn mở." });

            var result = new BillDetailDto
            {
                IdBill = bill.Id,
                IdTable = bill.IdTable,
                TableName = bill.TableFood.Name,
                DateCheckIn = bill.DateCheckIn,
                Status = bill.Status,
                Discount = bill.Discount,
                Items = bill.BillInfos.Select(bi => new BillInfoDto
                {
                    IdFood = bi.IdFood,
                    FoodName = bi.Food.Name,
                    Price = bi.Food.Price,
                    CostPrice = bi.CostPrice,
                    Count = bi.Count
                }).ToList()
            };

            return Ok(result);
        }

        [HttpPost("add-item")]
        public async Task<IActionResult> AddItemToBill([FromBody] AddFoodToBillDto dto)
        {
            var table = await _context.TableFoods.FindAsync(dto.IdTable);
            if (table == null) return NotFound("Không tìm thấy bàn!");

            var food = await _context.Foods.FindAsync(dto.IdFood);
            if (food == null) return NotFound("Không tìm thấy món ăn!");

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.IdTable == dto.IdTable && b.Status == 0);

            if (bill == null)
            {
                bill = new Bill
                {
                    IdTable = dto.IdTable,
                    DateCheckIn = DateTime.Now,
                    Status = 0
                };
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                table.Status = "Có người";
            }

            var billInfo = await _context.BillInfos
                .FirstOrDefaultAsync(bi => bi.IdBill == bill.Id && bi.IdFood == dto.IdFood);

            if (billInfo == null)
            {
                if (dto.Count > 0)
                {
                    billInfo = new BillInfo
                    {
                        IdBill = bill.Id,
                        IdFood = dto.IdFood,
                        Count = dto.Count,
                        CostPrice = food.CostPrice
                    };
                    _context.BillInfos.Add(billInfo);
                }
            }
            else
            {
                int newCount = billInfo.Count + dto.Count;
                if (newCount > 0)
                {
                    billInfo.Count = newCount;
                    billInfo.CostPrice = food.CostPrice;
                }
                else
                {
                    _context.BillInfos.Remove(billInfo);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật món thành công!", idBill = bill.Id });
        }

        [HttpPost("checkout/{billId}")]
        public async Task<IActionResult> Checkout(int billId, [FromBody] CheckoutDto dto)
        {
            var bill = await _context.Bills
                .Include(b => b.TableFood)
                .FirstOrDefaultAsync(b => b.Id == billId && b.Status == 0);

            if (bill == null) return NotFound("Không tìm thấy hóa đơn cần thanh toán!");

            bill.Status = 1;
            bill.DateCheckOut = DateTime.Now;
            bill.Discount = dto.Discount;
            bill.IdCustomer = dto.IdCustomer;

            bill.TableFood.Status = "Trống";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Thanh toán hóa đơn thành công!" });
        }
    }
}