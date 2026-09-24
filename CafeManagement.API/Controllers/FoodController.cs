using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FoodController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var foods = await _context.Foods
                .Include(f => f.Category)
                .Select(f => new FoodDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Price = f.Price,
                    CostPrice = f.CostPrice,
                    IdCategory = f.IdCategory,
                    CategoryName = f.Category.Name
                })
                .ToListAsync();

            return Ok(foods);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFoodDto dto)
        {
            var food = new Food
            {
                Name = dto.Name,
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                IdCategory = dto.IdCategory
            };

            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm món ăn thành công!", id = food.Id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null) return NotFound("Không tìm thấy món ăn!");

            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa món ăn thành công!" });
        }
    }
}