using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.Entities;
public class TableOperation
{
    public int Id { get; set; }
    [MaxLength(64)] public string RequestKey { get; set; } = "";
    [MaxLength(10)] public string Kind { get; set; } = "Move";
    public int SourceBillId { get; set; }
    public int TargetBillId { get; set; }
    public int SourceTableId { get; set; }
    public int TargetTableId { get; set; }
    [MaxLength(100)] public string SourceName { get; set; } = "";
    [MaxLength(100)] public string TargetName { get; set; } = "";
    [MaxLength(100)] public string CreatedBy { get; set; } = "";
    [MaxLength(500)] public string Note { get; set; } = "";
    public string ItemsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
