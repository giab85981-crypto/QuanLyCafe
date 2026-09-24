namespace CafeManagement.API.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Points { get; set; }
    }

    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}