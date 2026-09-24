namespace CafeManagement.API.DTOs
{
    public class IngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double MinQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class ImportReceiptItemDto
    {
        public int IdIngredient { get; set; }
        public double Count { get; set; }
        public decimal Price { get; set; }
    }

    public class CreateImportReceiptDto
    {
        public int? IdSupplier { get; set; }
        public string? SupplierName { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<ImportReceiptItemDto> Items { get; set; } = new List<ImportReceiptItemDto>();
    }

    public class RecipeItemDto
    {
        public int IdIngredient { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class RecipeDto
    {
        public int IdFood { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public List<RecipeItemDto> Ingredients { get; set; } = new List<RecipeItemDto>();
    }

    public class CreateRecipeItemDto
    {
        public int IdIngredient { get; set; }
        public double Amount { get; set; }
    }

    public class CreateRecipeDto
    {
        public int IdFood { get; set; }
        public List<CreateRecipeItemDto> Items { get; set; } = new List<CreateRecipeItemDto>();
    }
}