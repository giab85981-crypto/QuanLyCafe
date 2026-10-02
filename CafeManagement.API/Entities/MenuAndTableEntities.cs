using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.API.Entities
{
    public class Area
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<TableFood> TableFoods { get; set; } = new List<TableFood>();
    }
    public class FoodCategory
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Food> Foods { get; set; } = new List<Food>();
    }

    public class Food
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column("costPrice")]
        public double CostPrice { get; set; } // Khớp với cột costPrice FLOAT trong SQL

        public int IdCategory { get; set; }
        [ForeignKey("IdCategory")]
        public FoodCategory Category { get; set; } = null!;

        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public ICollection<BillInfo> BillInfos { get; set; } = new List<BillInfo>();
        public ICollection<KitchenOrderDetail> KitchenOrderDetails { get; set; } = new List<KitchenOrderDetail>();
    }

    public class TableFood
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Trống";

        public int Seats { get; set; } = 4;

        [MaxLength(255)]
        public string? Note { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // --- BỔ SUNG 2 DÒNG NÀY ĐỂ HẾT LỖI ---
        public int? IdArea { get; set; }
        [ForeignKey("IdArea")]
        public Area? Area { get; set; }
        // -------------------------------------

        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}