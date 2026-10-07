namespace CafeManagement.API.DTOs;
public class TableWrite : CreateTableFoodDto { public bool IsActive { get; set; } = true; }
public class AreaWrite : CreateAreaDto { public bool IsActive { get; set; } = true; }
public class TableTransferLine { public int IdBillInfo { get; set; } public int Count { get; set; } public int ExpectedCount { get; set; } public int ExpectedSentCount { get; set; } }
public class TableTransferWrite
{
    public string Kind { get; set; } = "Move";
    public string RequestKey { get; set; } = "";
    public int SourceBillId { get; set; }
    public int TargetTableId { get; set; }
    public int ExpectedTargetBillId { get; set; }
    public int? GuestCountToMove { get; set; }
    public string Note { get; set; } = "";
    public List<TableTransferLine> Items { get; set; } = new();
}
public class TableExcelWrite { public bool Preview { get; set; } = true; public List<TableWrite> Items { get; set; } = new(); }
