using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.API.Entities
{
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Unit { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double MinQuantity { get; set; }

        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
    }

    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        public int IdFood { get; set; }
        [ForeignKey("IdFood")]
        public Food Food { get; set; } = null!;

        public int IdIngredient { get; set; }
        [ForeignKey("IdIngredient")]
        public Ingredient Ingredient { get; set; } = null!;

        public double Amount { get; set; }
    }

    public class Supplier
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }

        public ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
    }

    public class ImportReceipt
    {
        [Key]
        public int Id { get; set; }
        public int IdSupplier { get; set; }
        [ForeignKey("IdSupplier")]
        public Supplier Supplier { get; set; } = null!;

        public string UserName { get; set; } = string.Empty;
        [ForeignKey("UserName")]
        public Account Account { get; set; } = null!;

        public DateTime ImportDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }

        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
    }

    public class ImportDetail
    {
        [Key]
        public int Id { get; set; }

        public int IdImportReceipt { get; set; }
        [ForeignKey("IdImportReceipt")]
        public ImportReceipt ImportReceipt { get; set; } = null!;

        public int IdIngredient { get; set; }
        [ForeignKey("IdIngredient")]
        public Ingredient Ingredient { get; set; } = null!;

        public double Count { get; set; }
    }
}