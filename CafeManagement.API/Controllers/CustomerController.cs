using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        // Tìm kiếm khách hàng theo SĐT hoặc Tên (cho ô F4 tại POS)
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            try
            {
                var query = _context.Customers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(c => c.Phone.ToLower().Contains(search) || c.Name.ToLower().Contains(search));
                }

                var customers = await query
                    .Select(c => new CustomerDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Phone = c.Phone,
                        Points = c.Points
                    })
                    .Take(10)
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tìm kiếm khách hàng!", detail = ex.Message });
            }
        }

        // Tạo khách hàng mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Tên và Số điện thoại không được để trống!" });
                }

                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == dto.Phone);
                if (existingCustomer != null)
                {
                    return BadRequest(new { message = "Số điện thoại này đã được đăng ký!" });
                }

                var customer = new Customer
                {
                    Name = dto.Name,
                    Phone = dto.Phone,
                    Points = 0
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return Ok(new CustomerDto
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Phone = customer.Phone,
                    Points = customer.Points
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm khách hàng!", detail = ex.Message });
            }
        }
    }
}