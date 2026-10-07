using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.Entities;
public class CustomerGroup
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
}
public class CustomerPointEntry
{
    public int Id { get; set; }
    public int IdCustomer { get; set; }
    public Customer Customer { get; set; } = null!;
    public int IdBill { get; set; }
    public Bill Bill { get; set; } = null!;
    [MaxLength(20)] public string Kind { get; set; } = "";
    public int Delta { get; set; }
    public int Balance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    [MaxLength(100)] public string CreatedBy { get; set; } = "";
}
