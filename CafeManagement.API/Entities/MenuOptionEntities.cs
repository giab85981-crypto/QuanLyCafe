using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.API.Entities;

public class FoodVariant
{
    public int Id { get; set; }
    public int IdFood { get; set; }
    public Food Food { get; set; } = null!;
    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    public double CostPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
public class FoodTopping
{
    public int IdFood { get; set; }
    public Food Food { get; set; } = null!;
    public int IdTopping { get; set; }
    public Food Topping { get; set; } = null!;
}
public class StockMovement
{
    public int Id { get; set; }
    public int IdIngredient { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public int? IdKitchenDetail { get; set; }
    public KitchenOrderDetail? KitchenDetail { get; set; }
    public int? IdLot { get; set; }
    public StockLot? Lot { get; set; }
    public int? IdDocument { get; set; }
    public WarehouseDocument? Document { get; set; }
    public int? IdImportReceipt { get; set; }
    public ImportReceipt? ImportReceipt { get; set; }
    [MaxLength(100)] public string CreatedBy { get; set; } = "";
    [Column(TypeName="decimal(18,4)")] public decimal? UnitCost { get; set; }
    public double Quantity { get; set; }
    [MaxLength(20)] public string Kind { get; set; } = "Kitchen";
    [MaxLength(500)] public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

