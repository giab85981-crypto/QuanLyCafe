using System.ComponentModel.DataAnnotations;

namespace CafeManagement.API.DTOs;

public class ItemTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateItemTypeDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
