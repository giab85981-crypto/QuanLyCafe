using System.ComponentModel.DataAnnotations;

namespace CafeManagement.API.Entities;

public class ItemType
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
