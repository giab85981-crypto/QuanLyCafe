using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách nguyên liệu tồn kho
        [HttpGet("ingredients")]
        public async Task<IActionResult> GetIngredients()
        {
            try
            {
                var ingredients = await _context.Ingredients
                    .Select(static i => new IngredientDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Quantity = i.Quantity,
                        MinQuantity = i.MinQuantity,
                        Unit = i.Unit
                    })
                    .ToListAsync();

                return Ok(ingredients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nguyên liệu!", detail = ex.Message });
            }
        }

        // 2. Tạo phiếu nhập kho (ImportReceipt & ImportDetail)
        [HttpPost("import")]
        public async Task<IActionResult> ImportInventory([FromBody] CreateImportReceiptDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto.Items == null || !dto.Items.Any())
                {
                    return BadRequest(new { message = "Danh sách chi tiết nhập kho không được để trống!" });
                }

                // Xử lý Nhà cung cấp (Supplier)
                int supplierId;
                if (dto.IdSupplier.HasValue && dto.IdSupplier.Value > 0)
                {
                    var supplier = await _context.Suppliers.FindAsync(dto.IdSupplier.Value);
                    if (supplier == null)
                    {
                        return BadRequest(new { message = "Nhà cung cấp không tồn tại!" });
                    }
                    supplierId = supplier.Id;
                }
                else if (!string.IsNullOrWhiteSpace(dto.SupplierName))
                {
                    var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Name == dto.SupplierName);
                    if (supplier == null)
                    {
                        supplier = new Supplier { Name = dto.SupplierName };
                        _context.Suppliers.Add(supplier);
                        await _context.SaveChangesAsync();
                    }
                    supplierId = supplier.Id;
                }
                else
                {
                    return BadRequest(new { message = "Vui lòng chọn hoặc nhập tên Nhà cung cấp!" });
                }

                // Kiểm tra tài khoản nhân viên
                if (string.IsNullOrWhiteSpace(dto.UserName))
                {
                    return BadRequest(new { message = "Vui lòng cung cấp UserName người lập phiếu!" });
                }
                var account = await _context.Accounts.FindAsync(dto.UserName);
                if (account == null)
                {
                    return BadRequest(new { message = "Tài khoản người lập phiếu không tồn tại!" });
                }

                // Tính tổng tiền phiếu nhập
                decimal totalAmount = (decimal)dto.Items.Sum(item => item.Count * (double)item.Price);

                var receipt = new ImportReceipt
                {
                    IdSupplier = supplierId,
                    UserName = dto.UserName,
                    ImportDate = DateTime.Now,
                    TotalAmount = totalAmount
                };

                _context.ImportReceipts.Add(receipt);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    var ingredient = await _context.Ingredients.FindAsync(item.IdIngredient);
                    if (ingredient == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = $"Không tìm thấy nguyên liệu có ID {item.IdIngredient}!" });
                    }

                    // Thêm chi tiết phiếu nhập (ImportDetail)
                    var detail = new ImportDetail
                    {
                        IdImportReceipt = receipt.Id,
                        IdIngredient = item.IdIngredient,
                        Count = item.Count
                    };
                    _context.ImportDetails.Add(detail);

                    // Cộng dồn vào kho nguyên liệu
                    ingredient.Quantity += item.Count;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Nhập kho thành công!", idReceipt = receipt.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi trong quá trình nhập kho!", detail = ex.Message });
            }
        }

        // 3. Xem danh sách công thức của các món
        [HttpGet("recipe")]
        public async Task<IActionResult> GetRecipes([FromQuery] int? foodId)
        {
            try
            {
                var query = _context.Foods.AsQueryable();

                if (foodId.HasValue)
                {
                    query = query.Where(f => f.Id == foodId.Value);
                }

                var recipes = await query
                    .Select(f => new RecipeDto
                    {
                        IdFood = f.Id,
                        FoodName = f.Name,
                        Ingredients = f.Recipes.Select(r => new RecipeItemDto
                        {
                            IdIngredient = r.IdIngredient,
                            IngredientName = r.Ingredient.Name,
                            Amount = r.Amount,
                            Unit = r.Ingredient.Unit
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(recipes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy công thức món!", detail = ex.Message });
            }
        }

        // 4. Thiết lập / Cập nhật công thức cho món ăn
        [HttpPost("recipe")]
        public async Task<IActionResult> SaveRecipe([FromBody] CreateRecipeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var food = await _context.Foods.FindAsync(dto.IdFood);
                if (food == null) return NotFound(new { message = "Không tìm thấy món ăn!" });

                // Xóa công thức cũ
                var oldRecipes = await _context.Recipes.Where(r => r.IdFood == dto.IdFood).ToListAsync();
                _context.Recipes.RemoveRange(oldRecipes);

                // Thêm công thức mới
                foreach (var item in dto.Items)
                {
                    var recipe = new Recipe
                    {
                        IdFood = dto.IdFood,
                        IdIngredient = item.IdIngredient,
                        Amount = item.Amount
                    };
                    _context.Recipes.Add(recipe);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Cập nhật công thức thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi khi lưu công thức!", detail = ex.Message });
            }
        }
    }
}