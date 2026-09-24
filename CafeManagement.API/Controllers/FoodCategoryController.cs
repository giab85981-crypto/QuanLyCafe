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
            var category = new FoodCategory { Name = dto.Name };
            _context.FoodCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new FoodCategoryDto { Id = category.Id, Name = category.Name });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.FoodCategories.FindAsync(id);
            if (category == null) return NotFound("Không tìm thấy danh mục!");

            _context.FoodCategories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa danh mục thành công!" });
        }
    }
}