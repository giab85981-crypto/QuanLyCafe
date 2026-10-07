namespace CafeManagement.API.DTOs;
public class IngredientUnitWrite { public string Name { get; set; } = ""; public decimal Factor { get; set; } }
public class WarehouseIngredientWrite : IngredientWriteDto
{
    public string Code { get; set; } = "";
    public int? IdGroup { get; set; }
    public bool IsActive { get; set; } = true;
    public List<IngredientUnitWrite> Units { get; set; } = new();
}
public class SupplierWrite
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public class WarehouseImportLine
{
    public int IdIngredient { get; set; }
    public int? IdUnit { get; set; }
    public double Quantity { get; set; }
    public decimal Price { get; set; }
    public string LotCode { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
}
public class WarehouseImportWrite
{
    public int IdSupplier { get; set; }
    public string Note { get; set; } = "";
    public string RequestKey { get; set; } = "";
    public decimal PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public List<WarehouseImportLine> Items { get; set; } = new();
}
public class SupplierPaymentWrite
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string Note { get; set; } = "";
    public string RequestKey { get; set; } = "";
}
public class WarehouseOperationLine
{
    public int IdIngredient { get; set; }
    public int? IdUnit { get; set; }
    public int? IdLot { get; set; }
    public double Quantity { get; set; }
    public double? ExpectedQuantity { get; set; }
}
public class WarehouseOperationWrite
{
    public string Kind { get; set; } = "Export";
    public string Note { get; set; } = "";
    public string RequestKey { get; set; } = "";
    public List<WarehouseOperationLine> Items { get; set; } = new();
}
public class CashEntryWrite : SupplierPaymentWrite
{
    public string Direction { get; set; } = "Out";
    public string Category { get; set; } = "Chi khác";
}
public class WarehouseExcelWrite
{
    public bool Preview { get; set; } = true;
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public List<WarehouseIngredientWrite> Items { get; set; } = new();
}
