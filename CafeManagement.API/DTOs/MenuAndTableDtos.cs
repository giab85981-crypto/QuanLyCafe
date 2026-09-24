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

    public class FoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double CostPrice { get; set; }
        public int IdCategory { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CreateFoodDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double CostPrice { get; set; }
        public int IdCategory { get; set; }
    }

    // --- Table DTOs ---
    public class TableFoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Trống";
    }

    public class CreateTableFoodDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateTableStatusDto
    {
        public string Status { get; set; } = "Trống";
    }
}