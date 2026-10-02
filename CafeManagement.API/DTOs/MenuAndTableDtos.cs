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

    // --- Area DTOs (MỚI) ---
    public class AreaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int TableCount { get; set; }
    }

    public class CreateAreaDto
    {
        public string Name { get; set; } = string.Empty;
    }

    // --- Table DTOs (CẬP NHẬT) ---
    public class TableFoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Trống";
        public int Seats { get; set; }
        public string? Note { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int? IdArea { get; set; }
        public string? AreaName { get; set; }
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