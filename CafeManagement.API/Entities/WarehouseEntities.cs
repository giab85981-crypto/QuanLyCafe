using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CafeManagement.API.Entities;
public class IngredientGroup { public int Id { get; set; } [MaxLength(100)] public string Name { get; set; } = ""; }
public class IngredientUnit
{
    public int Id { get; set; }
    public int IdIngredient { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    [MaxLength(30)] public string Name { get; set; } = "";
    [Column(TypeName="decimal(18,6)")] public decimal Factor { get; set; }
}
public class StockLot
{
    public int Id { get; set; }
    public int IdIngredient { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    [MaxLength(100)] public string Code { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public double Quantity { get; set; }
    [Column(TypeName="decimal(18,4)")] public decimal UnitCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? IdImportReceipt { get; set; }
    public ImportReceipt? ImportReceipt { get; set; }
}
public class WarehouseDocument
{
    public int Id { get; set; }
    [MaxLength(20)] public string Kind { get; set; } = "Export";
    [MaxLength(500)] public string Note { get; set; } = "";
    [MaxLength(100)] public string CreatedBy { get; set; } = "";
    [MaxLength(64)] public string RequestKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<WarehouseDocumentLine> Lines { get; set; } = new List<WarehouseDocumentLine>();
}
public class WarehouseDocumentLine
{
    public int Id { get; set; }
    public int IdDocument { get; set; }
    public WarehouseDocument Document { get; set; } = null!;
    public int IdIngredient { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public double BeforeQuantity { get; set; }
    public double AfterQuantity { get; set; }
    [Column(TypeName="decimal(18,4)")] public decimal UnitCost { get; set; }
}
public class CashEntry
{
    public int? IdShift { get; set; }
    public CashierShift? Shift { get; set; }
    public int Id { get; set; }
    [MaxLength(10)] public string Direction { get; set; } = "Out";
    [MaxLength(100)] public string Category { get; set; } = "Nhập hàng";
    [MaxLength(500)] public string Note { get; set; } = "";
    [MaxLength(20)] public string PaymentMethod { get; set; } = "Cash";
    [MaxLength(100)] public string CreatedBy { get; set; } = "";
    [MaxLength(64)] public string RequestKey { get; set; } = "";
    [Column(TypeName="decimal(18,2)")] public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? IdImportReceipt { get; set; }
    public ImportReceipt? ImportReceipt { get; set; }
    public int? IdBill { get; set; }
    public Bill? Bill { get; set; }
}
