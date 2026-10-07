using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CafeManagement.API.Entities;
public class QrOrderRequest
{
    public int Id { get; set; }
    [MaxLength(36)] public string RequestKey { get; set; } = "";
    public int IdTable { get; set; }
    public TableFood Table { get; set; } = null!;
    [MaxLength(100)] public string TableName { get; set; } = "";
    public string ItemsJson { get; set; } = "[]";
    [MaxLength(300)] public string Note { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
    public DateTime? DecidedAt { get; set; }
    [MaxLength(100)] public string DecidedBy { get; set; } = "";
    [MaxLength(300)] public string Reason { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")] public decimal Total { get; set; }
    public int? IdBill { get; set; }
    public Bill? Bill { get; set; }
}
