using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.DTOs;
public class EmployeeWrite
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Phone { get; set; } = "";
    [MaxLength(120)] public string Email { get; set; } = "";
    [MaxLength(255)] public string Address { get; set; } = "";
    [MaxLength(10)] public string Gender { get; set; } = "";
    public DateTime? Birthday { get; set; }
    public DateTime? HireDate { get; set; }
    [MaxLength(80)] public string Department { get; set; } = "";
    [MaxLength(80)] public string Position { get; set; } = "";
    [MaxLength(1000)] public string Note { get; set; } = "";
    public string? UserName { get; set; }
}
