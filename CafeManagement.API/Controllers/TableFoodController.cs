using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableFoodController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TableFoodController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _context.TableFoods
                .Select(t => new TableFoodDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Status = t.Status
                })
                .ToListAsync();

            return Ok(tables);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTableFoodDto dto)
        {
            var table = new TableFood
            {
                Name = dto.Name,
                Status = "Trống"
            };

            _context.TableFoods.Add(table);
            await _context.SaveChangesAsync();

            return Ok(new TableFoodDto { Id = table.Id, Name = table.Name, Status = table.Status });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusDto dto)
        {
            var table = await _context.TableFoods.FindAsync(id);
            if (table == null) return NotFound("Không tìm thấy bàn!");

            table.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái bàn thành công!" });
        }
    }
}