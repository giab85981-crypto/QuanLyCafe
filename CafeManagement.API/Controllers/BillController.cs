using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController, Microsoft.AspNetCore.Authorization.Authorize]
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
                .Include(b => b.Customer)
                .Include(b => b.BillInfos)
                .ThenInclude(bi => bi.Food)
                .FirstOrDefaultAsync(b => b.IdTable == tableId && b.Status == 0);

            if (bill == null) return NotFound(new { message = "Bàn hiện chưa có hóa đơn mở." });

            var result = new BillDetailDto
            {
                IdBill = bill.Id,
                Customer = bill.Customer == null ? null : CafeManagement.API.Services.CustomerCatalog.Dto(bill.Customer),
                IdTable = bill.IdTable,
                TableName = bill.TableNameSnapshot != "" ? bill.TableNameSnapshot : bill.TableFood!.Name,
                OrderType = bill.OrderType,
                DateCheckIn = bill.DateCheckIn,
                Status = bill.Status,
                Discount = bill.Discount,
                GuestCount = bill.GuestCount,
                Items = bill.BillInfos.Where(bi => bi.Count > 0).Select(bi => new BillInfoDto
                {
                    IdBillInfo = bi.Id, SentCount = bi.SentCount, IdVariant = bi.IdVariant, OptionLabel = bi.OptionLabel,
                    IdFood = bi.IdFood,
                    FoodName = bi.FoodNameSnapshot != "" ? bi.FoodNameSnapshot : bi.Food.Name,
                    Price = bi.UnitPrice ?? bi.Food.Price,
                    CostPrice = bi.CostPrice,
                    Count = bi.Count
                }).ToList()
            };

            return Ok(result);
        }

        [HttpPost("add-item")]
        public async Task<IActionResult> AddItemToBill(AddFoodToBillDto dto)
        {
            try { return Ok(new { idBill = await new CafeManagement.API.Services.OrderFlow(_context).Add(dto, HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system") }); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }
        [HttpPost("items/{lineId}/cancel")]
        public async Task<IActionResult> CancelItem(int lineId, CancelBillItemDto dto)
        {
            try { await new CafeManagement.API.Services.OrderFlow(_context).Cancel(lineId, dto, HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system"); return Ok(); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }
        [HttpPost("{billId}/close-empty")]
        public async Task<IActionResult> CloseEmpty(int billId)
        {
            await using var transaction = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
            await new StockReservations(_context).Lock();
            var bill = await _context.Bills.Include(b => b.TableFood).Include(b => b.BillInfos).FirstOrDefaultAsync(b => b.Id == billId && b.Status == 0);
            if (bill == null) return NotFound();
            if (bill.BillInfos.Any(i => i.Count > 0)) return BadRequest("Hãy hủy toàn bộ món trước khi đóng bàn trống.");
            bill.Status = 2; bill.DateCheckOut = DateTime.Now; if (bill.TableFood != null) bill.TableFood.Status = "Trống";
            await _context.SaveChangesAsync(); if (transaction != null) await transaction.CommitAsync(); return Ok();
        }
        [HttpPut("{billId}/guests")]
        public async Task<IActionResult> UpdateGuests(int billId, [FromBody] UpdateGuestCountDto dto)
        {
            if (dto.GuestCount < 1 || dto.GuestCount > 1000) return BadRequest("Số khách phải từ 1 đến 1000.");
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await new StockReservations(_context).Lock();
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.Id == billId && b.Status == 0);
            if (bill == null) return NotFound("Không tìm thấy hóa đơn đang phục vụ.");
            bill.GuestCount = dto.GuestCount;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { bill.GuestCount });
        }

        [HttpPut("{billId}/customer")]
        public async Task<IActionResult> SetCustomer(int billId, BillCustomerWrite dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await new StockReservations(_context).Lock();
            var bill = await _context.Bills.SingleOrDefaultAsync(b => b.Id == billId && b.Status == 0);
            if (bill == null) return NotFound("Đơn không còn phục vụ.");
            if (dto.IdCustomer.HasValue && !await _context.Customers.AnyAsync(c => c.Id == dto.IdCustomer && c.IsActive)) return BadRequest("Khách đã ngừng hoạt động hoặc không tồn tại.");
            bill.IdCustomer = dto.IdCustomer; await _context.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
        }
        [HttpPost("checkout/{billId}")]
        public async Task<IActionResult> Checkout(int billId, [FromBody] CheckoutDto dto)
        {
            if (HttpContext?.User.Identity?.IsAuthenticated == true && (dto.Discount > 0 || dto.RedeemPoints > 0) && !DynamicAccess.Has(User, "POS_DISCOUNT")) return Forbid();
            if (dto.Discount < 0 || dto.Discount > 100 || dto.GuestCount < 1 || dto.GuestCount > 1000) return BadRequest("Giảm giá hoặc số khách không hợp lệ.");
            if (dto.PaymentMethod != "Cash" && dto.PaymentMethod != "Transfer") return BadRequest("Phương thức thanh toán không hợp lệ.");
            if (dto.ExpectedTotal.HasValue && (dto.ExpectedTotal < 0 || decimal.Round(dto.ExpectedTotal.Value, 2) != dto.ExpectedTotal)) return BadRequest("Số tiền đối chiếu không hợp lệ.");
            await using var transaction = _context.Database.CurrentTransaction == null ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
            await new StockReservations(_context).Lock();
            var bill = await _context.Bills
                .Include(b => b.TableFood)
                .Include(b => b.BillInfos).ThenInclude(i => i.Food)
                .FirstOrDefaultAsync(b => b.Id == billId && b.Status == 0);

            if (bill == null) return NotFound("Không tìm thấy hóa đơn cần thanh toán!");
            if (!bill.BillInfos.Any(i => i.Count > 0))
                return BadRequest("Hóa đơn chưa có món để thanh toán.");

            // Checkout permission must not become a second path to edit order metadata.
            if (HttpContext?.User.Identity?.IsAuthenticated == true && !DynamicAccess.Has(User, "POS_ORDER")
                && (dto.IdCustomer != bill.IdCustomer || dto.GuestCount.HasValue && dto.GuestCount != (bill.GuestCount ?? 1))) return Forbid();

            try { bill.IdShift = await new ShiftFlow(_context).ActiveId(HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system", HttpContext?.User.Identity?.IsAuthenticated == true); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }

            try { await new OrderFlow(_context).Send(billId, HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system"); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            foreach (var item in bill.BillInfos)
                item.UnitPrice ??= item.Food.Price;
            bill.TotalPrice = Math.Round(bill.BillInfos.Sum(i => i.Count * i.UnitPrice!.Value)
                * (100 - dto.Discount) / 100m, 2, MidpointRounding.AwayFromZero);
            bill.GuestCount = dto.GuestCount ?? bill.GuestCount;

            bill.Status = 1;
            bill.PaymentMethod = dto.PaymentMethod; bill.PaidBy = HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
            if (bill.TableFood != null) bill.TableNameSnapshot = bill.TableFood.Name;
            bill.DateCheckOut = DateTime.Now;
            bill.Discount = dto.Discount;
            try { await new CafeManagement.API.Services.CustomerLoyalty(_context).Checkout(bill, dto.IdCustomer, dto.RedeemPoints, bill.PaidBy); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            if (dto.ExpectedTotal.HasValue && dto.ExpectedTotal.Value != bill.TotalPrice)
                return Conflict(new { code = "BILL_CHANGED", message = "Hóa đơn vừa thay đổi. Hãy kiểm tra lại món và số tiền trước khi thanh toán." });

            if (bill.TableFood != null) bill.TableFood.Status = "Trống";

            if (bill.TotalPrice > 0) _context.CashEntries.Add(new CafeManagement.API.Entities.CashEntry { IdShift = bill.IdShift, IdBill = bill.Id, Direction = "In", Category = "Bán hàng", Amount = bill.TotalPrice, PaymentMethod = dto.PaymentMethod, Note = $"Thanh toán HD{bill.Id:D6}", CreatedBy = HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system" });
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            return Ok(new { message = "Thanh toán hóa đơn thành công!", bill.TotalPrice, bill.PointsEarned, bill.PointsRedeemed });
        }
    }
}
