using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.DTOs;
public class QrOrderWrite
{
    [Required, StringLength(36, MinimumLength = 36)] public string RequestKey { get; set; } = "";
    [Required, MinLength(1), MaxLength(30)] public List<QrItemWrite> Items { get; set; } = [];
    [MaxLength(300)] public string Note { get; set; } = "";
    [Range(0, 1000000000)] public decimal ExpectedTotal { get; set; }
}
public class QrItemWrite
{
    public int IdFood { get; set; }
    public int? IdVariant { get; set; }
    [Range(1, 20)] public int Count { get; set; }
    [Required, MaxLength(10)] public List<ToppingSelectionDto> Toppings { get; set; } = [];
}
public record QrItemSnapshot(int IdFood, int? IdVariant, int Count, List<ToppingSelectionDto> Toppings, string Name, string Options, decimal Price);
public class QrDecision
{
    public bool Accept { get; set; }
    [MaxLength(300)] public string Reason { get; set; } = "";
}
