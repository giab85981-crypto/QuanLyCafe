using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Services;

public class MenuCatalog(AppDbContext db)
{
    public IQueryable<Food> Query() => db.Foods.Include(f => f.Category)
        .Include(f => f.Recipes).ThenInclude(r => r.Ingredient).ThenInclude(i => i.Lots)
        .Include(f => f.Variants).ThenInclude(v => v.Recipes).ThenInclude(r => r.Ingredient).ThenInclude(i => i.Lots)
        .Include(f => f.AllowedToppings).AsSplitQuery();
    public static double Cost(IEnumerable<Recipe> recipe, double fallback) => recipe.Any()
        ? recipe.Sum(r => r.Amount * (double)r.Ingredient.UnitCost) : fallback;
    public static int? Availability(IEnumerable<Recipe> recipe, IReadOnlyDictionary<int, double>? held = null)
    {
        var ingredients = recipe.GroupBy(r => r.IdIngredient).Select(g => new { Id = g.Key, Amount = g.Sum(r => r.Amount), Quantity = !g.First().Ingredient.IsActive ? 0 : g.First().Ingredient.Lots.Any() ? g.First().Ingredient.Lots.Where(StockLots.Usable).Sum(l => l.Quantity) : g.First().Ingredient.Quantity }).ToList();
        return ingredients.Count == 0 ? null : (int)Math.Min(int.MaxValue, ingredients.Min(r => Math.Floor(Math.Max(0, r.Quantity - (held?.GetValueOrDefault(r.Id) ?? 0)) / Math.Max(0.0000001, r.Amount))));
    }
    public static FoodDto Dto(Food f) => Dto(f, null);
    public static FoodDto Dto(Food f, IReadOnlyDictionary<int, double>? held) => new()
    {
        Id = f.Id, Name = f.Name, Code = string.IsNullOrWhiteSpace(f.Code) ? $"MON{f.Id:D4}" : f.Code,
        Price = f.Price, CostPrice = Cost(f.Recipes.Where(r => r.IdVariant == null), f.CostPrice),
        IdCategory = f.IdCategory, CategoryName = f.Category?.Name ?? "", ItemType = f.ItemType,
        MenuKind = f.MenuKind, IsActive = f.IsActive, IsFavorite = f.IsFavorite, IsTopping = f.IsTopping,
        ImageUrl = f.ImageUrl, Description = f.Description, HasRecipe = f.Recipes.Any(),
        AvailableQuantity = f.Variants.Any(v => v.IsActive)
            ? (f.Variants.Where(v => v.IsActive).All(v => v.Recipes.Any()) ? f.Variants.Where(v => v.IsActive).Max(v => Availability(v.Recipes, held)) : null)
            : Availability(f.Recipes.Where(r => r.IdVariant == null), held),
        Recipe = f.Recipes.Where(r => r.IdVariant == null).Select(r => new CreateRecipeItemDto { IdIngredient = r.IdIngredient, Amount = r.Amount }).ToList(),
        Variants = f.Variants.Select(v => new VariantDto { Id = v.Id, Name = v.Name, Price = v.Price, CostPrice = Cost(v.Recipes, v.CostPrice), IsActive = v.IsActive, AvailableQuantity = Availability(v.Recipes, held),
            Recipe = v.Recipes.Select(r => new CreateRecipeItemDto { IdIngredient = r.IdIngredient, Amount = r.Amount }).ToList() }).ToList(),
        ToppingIds = f.AllowedToppings.Select(t => t.IdTopping).ToList()
    };
    public async Task<string?> Validate(CreateFoodDto dto, int? id = null)
    {
        if (dto == null) return "Dòng món bị trống.";
        dto.ItemType ??= ""; dto.Code ??= ""; dto.Description ??= "";
        if (dto.Recipe == null || dto.Variants == null || dto.ToppingIds == null) return "Cấu hình công thức, size hoặc topping bị thiếu.";
        if (dto.Variants.Any(v => v == null || v.Recipe == null)) return "Cấu hình size không hợp lệ.";
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 150) return "Tên món phải từ 1 đến 150 ký tự.";
        if (dto.Price < 0 || dto.Price > 1000000000 || !double.IsFinite(dto.CostPrice) || dto.CostPrice < 0 || dto.CostPrice > 1000000000) return "Giá bán và giá vốn phải hợp lệ, không âm.";
        if (!await db.FoodCategories.AnyAsync(c => c.Id == dto.IdCategory)) return "Nhóm món không tồn tại.";
        if (!new[] { "Đồ ăn", "Đồ uống", "Dịch vụ", "Khác" }.Contains(dto.MenuKind)) return "Loại thực đơn không hợp lệ.";
        if (dto.ItemType.Length > 100 || dto.Code.Length > 100 || dto.Description.Length > 1000) return "Mã, loại hoặc mô tả món quá dài.";
        if (!string.IsNullOrWhiteSpace(dto.ItemType) && !await db.ItemTypes.AnyAsync(t => t.Name == dto.ItemType.Trim())) return "Loại món không tồn tại.";
        if (!string.IsNullOrWhiteSpace(dto.Code) && await db.Foods.AnyAsync(f => f.Id != id && f.Code == dto.Code.Trim())) return "Mã món đã tồn tại.";
        if (dto.ImageUrl != null && !(dto.ImageUrl.StartsWith("data:image/jpeg;base64,") || dto.ImageUrl.StartsWith("data:image/png;base64,") || dto.ImageUrl.StartsWith("data:image/webp;base64,") || (Uri.TryCreate(dto.ImageUrl, UriKind.Absolute, out var url) && new[] { "https", "http" }.Contains(url.Scheme)))) return "Ảnh phải là JPG/PNG/WebP hoặc đường dẫn HTTP(S).";
        if (dto.ImageUrl?.Length > 3000000) return "Ảnh quá lớn (tối đa 2 MB).";
        if (dto.IsTopping && (dto.Variants.Count > 0 || dto.ToppingIds.Count > 0)) return "Topping không dùng size hoặc topping con.";
        if (dto.Variants.Any(v => string.IsNullOrWhiteSpace(v.Name) || v.Name.Length > 50 || v.Price < 0 || v.Price > 1000000000 || !double.IsFinite(v.CostPrice) || v.CostPrice < 0 || v.CostPrice > 1000000000)) return "Size cần có tên, giá bán và giá vốn hợp lệ.";
        if (dto.Variants.Select(v => v.Name.Trim().ToLower()).Distinct().Count() != dto.Variants.Count) return "Tên size bị trùng.";
        if (dto.Variants.Where(v => v.Id > 0).Any(v => !db.FoodVariants.Any(x => x.Id == v.Id && x.IdFood == id))) return "Size không thuộc món này.";
        var recipes = dto.Variants.Select(v => v.Recipe).Append(dto.Recipe).ToList();
        foreach (var list in recipes)
        {
            if (list.Any(r => r == null || !double.IsFinite(r.Amount) || r.Amount <= 0 || r.Amount > 1000000000) || list.Select(r => r.IdIngredient).Distinct().Count() != list.Count) return "Công thức cần định lượng dương, không lặp nguyên liệu.";
            var ingredientIds = list.Select(r => r.IdIngredient).ToList();
            if (await db.Ingredients.CountAsync(i => ingredientIds.Contains(i.Id)) != ingredientIds.Count) return "Nguyên liệu không tồn tại.";
        }
        var toppingIds = dto.ToppingIds.Distinct().ToList();
        if (toppingIds.Contains(id ?? 0) || await db.Foods.CountAsync(f => toppingIds.Contains(f.Id) && f.IsTopping && (f.IsActive || db.FoodToppings.Any(t => t.IdFood == id && t.IdTopping == f.Id))) != toppingIds.Count) return "Topping không hợp lệ hoặc đã ngừng bán.";
        if (id.HasValue && dto.IsTopping && await db.FoodToppings.AnyAsync(t => t.IdFood == id)) return "Hãy bỏ topping gắn với món trước khi chuyển món thành topping.";
        if (id.HasValue && !dto.IsTopping && await db.FoodToppings.AnyAsync(t => t.IdTopping == id)) return "Món đang được dùng làm topping, không thể đổi loại.";
        return null;
    }
    public async Task Save(Food food, CreateFoodDto dto)
    {
        food.Name = dto.Name.Trim(); food.Price = dto.Price; food.CostPrice = dto.CostPrice;
        food.IdCategory = dto.IdCategory; food.Code = dto.Code.Trim(); food.Description = dto.Description.Trim();
        food.ItemType = dto.ItemType.Trim(); food.MenuKind = dto.MenuKind; food.IsActive = dto.IsActive;
        food.IsTopping = dto.IsTopping; food.IsFavorite = dto.IsFavorite; food.ImageUrl = dto.ImageUrl;
        db.Recipes.RemoveRange(await db.Recipes.Where(r => r.IdFood == food.Id).ToListAsync());
        db.FoodToppings.RemoveRange(food.AllowedToppings);
        await db.SaveChangesAsync();
        foreach (var previous in food.Variants.Where(v => dto.Variants.All(x => x.Id != v.Id))) previous.IsActive = false;
        foreach (var variant in dto.Variants)
        {
            var item = food.Variants.FirstOrDefault(v => v.Id == variant.Id && variant.Id > 0);
            if (item == null) { item = new FoodVariant { Food = food }; food.Variants.Add(item); }
            item.Name = variant.Name.Trim(); item.Price = variant.Price; item.CostPrice = variant.CostPrice; item.IsActive = variant.IsActive;
            foreach (var r in variant.Recipe) db.Recipes.Add(new Recipe { Food = food, Variant = item, IdIngredient = r.IdIngredient, Amount = r.Amount });
        }
        foreach (var r in dto.Recipe) db.Recipes.Add(new Recipe { Food = food, IdIngredient = r.IdIngredient, Amount = r.Amount });
        foreach (var id in dto.ToppingIds.Distinct()) db.FoodToppings.Add(new FoodTopping { Food = food, IdTopping = id });
        await db.SaveChangesAsync();
        if (string.IsNullOrWhiteSpace(food.Code)) { food.Code = $"MON{food.Id:D4}";
            while (await db.Foods.AnyAsync(f => f.Id != food.Id && f.Code == food.Code)) food.Code += "A";
            await db.SaveChangesAsync(); }
    }
}



