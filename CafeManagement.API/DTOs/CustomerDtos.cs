using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.DTOs;
public class CustomerDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public int Points { get; set; }
}
public class CreateCustomerDto
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(30)] public string Phone { get; set; } = "";
    [MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(300)] public string Address { get; set; } = "";
    [MaxLength(500)] public string Note { get; set; } = "";
    [MaxLength(10)] public string Gender { get; set; } = "";
    public DateTime? Birthday { get; set; }
    public int? IdGroup { get; set; }
}
public class CustomerStateWrite { public bool IsActive { get; set; } }
public class CustomerGroupWrite { [Required, MaxLength(100)] public string Name { get; set; } = ""; }
public class CustomerImportWrite { public List<CreateCustomerDto> Items { get; set; } = []; public bool Preview { get; set; } = true; }
