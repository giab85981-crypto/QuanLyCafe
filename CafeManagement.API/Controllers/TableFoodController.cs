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

        // ==================== APIS KHU VỰC (AREA) ====================

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            var areas = await _context.Areas
                .Select(a => new AreaDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    IsActive = a.IsActive,
                    TableCount = a.TableFoods.Count
                })
                .ToListAsync();

            return Ok(areas);
        }

        [HttpPost("areas")]
        public async Task<IActionResult> CreateArea([FromBody] CreateAreaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên khu vực không được để trống!");

            var area = new Area { Name = dto.Name, IsActive = true };
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();

            return Ok(area);
        }

        // ==================== APIS PHÒNG / BÀN (TABLE) ====================

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? areaId, [FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _context.TableFoods.Include(t => t.Area).AsQueryable();

            if (areaId.HasValue && areaId > 0)
                query = query.Where(t => t.IdArea == areaId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Name.Contains(search));

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
                query = query.Where(t => t.Status == status);

            var tables = await query
                .OrderBy(t => t.SortOrder)
                .Select(t => new TableFoodDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Status = t.Status,
                    Seats = t.Seats,
                    Note = t.Note,
                    SortOrder = t.SortOrder,
                    IsActive = t.IsActive,
                    IdArea = t.IdArea,
                    AreaName = t.Area != null ? t.Area.Name : "Chưa xếp"
                })
                .ToListAsync();

            return Ok(tables);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTableFoodDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên phòng/bàn không được để trống!");

            var table = new TableFood
            {
                Name = dto.Name,
                Seats = dto.Seats,
                Note = dto.Note,
                SortOrder = dto.SortOrder,
                IdArea = dto.IdArea,
                Status = "Trống",
                IsActive = true
            };

            _context.TableFoods.Add(table);
            await _context.SaveChangesAsync();

            return Ok(new TableFoodDto
            {
                Id = table.Id,
                Name = table.Name,
                Status = table.Status,
                Seats = table.Seats,
                Note = table.Note,
                SortOrder = table.SortOrder,
                IsActive = table.IsActive,
                IdArea = table.IdArea
            });
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.TableFoods.FindAsync(id);
            if (table == null) return NotFound("Không tìm thấy bàn!");

            _context.TableFoods.Remove(table);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bàn thành công!" });
        }
    }
}