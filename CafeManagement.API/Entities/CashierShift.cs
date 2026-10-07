using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CafeManagement.API.Entities;
public class CashierShift
{
    public int Id { get; set; }
    [MaxLength(100)] public string UserName { get; set; } = "";
    public Account Account { get; set; } = null!;
    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal OpeningCash { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? CountedCash { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? ExpectedCash { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? Difference { get; set; }
    [MaxLength(500)] public string OpeningNote { get; set; } = "";
    [MaxLength(500)] public string ClosingNote { get; set; } = "";
    [MaxLength(100)] public string ClosedBy { get; set; } = "";
    [MaxLength(64)] public string RequestKey { get; set; } = "";
    public string ClosingSummaryJson { get; set; } = "";
}
