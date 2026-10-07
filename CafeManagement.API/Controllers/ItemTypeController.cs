using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemTypeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemTypeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var types = await _context.ItemTypes
                .OrderBy(t => t.Id)
                .Select(t => new ItemTypeDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();

            return Ok(types);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateItemTypeDto dto)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100)
                return BadRequest("Vui lòng nhập tên loại món!");

            var exists = await _context.ItemTypes.AnyAsync(t => t.Name.ToLower() == name.ToLower());
            if (exists)
                return BadRequest("Loại món này đã tồn tại!");

            var itemType = new ItemType { Name = name };
            _context.ItemTypes.Add(itemType);
            await _context.SaveChangesAsync();

            return Ok(new ItemTypeDto { Id = itemType.Id, Name = itemType.Name });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(int id, CreateItemTypeDto dto)
        {
            var item = await _context.ItemTypes.FindAsync(id); if (item == null) return NotFound();
            var name = dto.Name.Trim();
            if (name.Length == 0 || name.Length > 100 || await _context.ItemTypes.AnyAsync(t => t.Id != id && t.Name == name)) return BadRequest("Tên loại không hợp lệ hoặc trùng.");
            await using var tx = await _context.Database.BeginTransactionAsync();
            var foods = await _context.Foods.Where(f => f.ItemType == item.Name).ToListAsync();
            foreach (var food in foods) food.ItemType = name;
            item.Name = name; await _context.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var itemType = await _context.ItemTypes.FindAsync(id);
            if (itemType == null) return NotFound("Không tìm thấy loại món!");

            var inUse = await _context.Foods.AnyAsync(f => f.ItemType == itemType.Name);
            if (inUse)
                return BadRequest("Loại món đang được dùng bởi một số món, không thể xóa!");

            _context.ItemTypes.Remove(itemType);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa loại món thành công!" });
        }
    }
}

