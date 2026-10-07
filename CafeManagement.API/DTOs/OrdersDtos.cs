using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.DTOs;
public class TakeawayWrite
{
    [Required, MaxLength(64)] public string RequestKey { get; set; } = "";
    [MaxLength(100)] public string Label { get; set; } = "";
    [MaxLength(500)] public string Note { get; set; } = "";
}
public class RefundWrite
{
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
    public string PaymentMethod { get; set; } = "Cash";
}
