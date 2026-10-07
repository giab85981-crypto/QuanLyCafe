using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodCategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FoodCategoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.FoodCategories
                .Select(c => new FoodCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFoodCategoryDto dto)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0 || name.Length > 100 || await _context.FoodCategories.AnyAsync(c => c.Name == name)) return BadRequest("Tên nhóm trống, quá dài hoặc đã tồn tại.");
            var category = new FoodCategory { Name = name };
            _context.FoodCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new FoodCategoryDto { Id = category.Id, Name = category.Name });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(int id, CreateFoodCategoryDto dto)
        {
            var category = await _context.FoodCategories.FindAsync(id); if (category == null) return NotFound();
            var name = dto.Name.Trim();
            if (name.Length == 0 || name.Length > 100 || await _context.FoodCategories.AnyAsync(c => c.Id != id && c.Name == name)) return BadRequest("Tên nhóm không hợp lệ hoặc trùng.");
            category.Name = name; await _context.SaveChangesAsync(); return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.FoodCategories.FindAsync(id);
            if (category == null) return NotFound("Không tìm thấy danh mục!");

            if (await _context.Foods.AnyAsync(f => f.IdCategory == id)) return BadRequest("Nhóm đang có món, hãy chuyển món sang nhóm khác trước.");
            _context.FoodCategories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa danh mục thành công!" });
        }
    }
}
