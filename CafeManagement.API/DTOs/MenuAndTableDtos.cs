namespace CafeManagement.API.DTOs
{
    // --- Food Category DTOs ---
    public class FoodCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateFoodCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }

    // --- Food DTOs ---
    public class CreateFoodDto
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Range(0, 1000000000)] public decimal Price { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, 1000000000)] public double CostPrice { get; set; }
        public int IdCategory { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string MenuKind { get; set; } = "Khác";
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsTopping { get; set; }
        public bool IsFavorite { get; set; }
        public List<CreateRecipeItemDto> Recipe { get; set; } = new();
        public List<VariantDto> Variants { get; set; } = new();
        public List<int> ToppingIds { get; set; } = new();
    }
    public class UpdateFoodDto : CreateFoodDto { }
    public class FoodDto : CreateFoodDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool HasRecipe { get; set; }
        public int? AvailableQuantity { get; set; }
    }
    public class VariantDto
    {
        public int? AvailableQuantity { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double CostPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public List<CreateRecipeItemDto> Recipe { get; set; } = new();
    }
    public class IngredientWriteDto
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double MinQuantity { get; set; }
        public decimal UnitCost { get; set; }
    }
    public class FoodStatusDto { public bool IsActive { get; set; } }
    public class FavoriteDto { public bool IsFavorite { get; set; } }
    public class FoodImportDto { [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public List<CreateFoodDto> Items { get; set; } = new(); public bool Preview { get; set; } = true; }
    public class ToppingSelectionDto { public int IdFood { get; set; } public int Count { get; set; } = 1; }
    public class CancelBillItemDto { public int Count { get; set; } = 1; public string Reason { get; set; } = string.Empty; }
    // --- Area DTOs ---
    public class AreaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int TableCount { get; set; }
    }

    public class CreateAreaDto
    {
        public string Name { get; set; } = string.Empty;
    }

    // --- Table DTOs ---
    public class TableFoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Trống";
        public int Seats { get; set; } = 4;
        public string? Note { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public int? IdArea { get; set; }
        public string AreaName { get; set; } = string.Empty;
    }

    public class CreateTableFoodDto
    {
        public string Name { get; set; } = string.Empty;
        public int Seats { get; set; } = 4;
        public string? Note { get; set; }
        public int SortOrder { get; set; } = 0;
        public int? IdArea { get; set; }
    }

    public class UpdateTableStatusDto
    {
        public string Status { get; set; } = "Trống";
    }
}
