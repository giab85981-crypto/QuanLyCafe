namespace CafeManagement.API.DTOs
{
    public class CreateKitchenOrderDto
    {
        public int IdBill { get; set; }
        public List<CreateKitchenOrderDetailDto> Items { get; set; } = new List<CreateKitchenOrderDetailDto>();
    }

    public class CreateKitchenOrderDetailDto
    {
        public int IdFood { get; set; }
        public int Count { get; set; }
    }

    public class KitchenOrderDetailDto
    {
        public int Id { get; set; }
        public int IdFood { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class KitchenOrderDto
    {
        public int Id { get; set; }
        public int IdBill { get; set; }
        public string TableName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public List<KitchenOrderDetailDto> Details { get; set; } = new List<KitchenOrderDetailDto>();
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = "Completed";
    }
}