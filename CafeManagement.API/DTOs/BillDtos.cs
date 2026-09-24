namespace CafeManagement.API.DTOs
{
    public class AddFoodToBillDto
    {
        public int IdTable { get; set; }
        public int IdFood { get; set; }
        public int Count { get; set; }
    }

    public class CheckoutDto
    {
        public int Discount { get; set; }
        public int? IdCustomer { get; set; }
    }

    public class BillInfoDto
    {
        public int IdFood { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double CostPrice { get; set; }
        public int Count { get; set; }
        public decimal TotalPrice => Price * Count;
        public double TotalCost => CostPrice * Count;
        public decimal Profit => TotalPrice - (decimal)TotalCost;
    }

    public class BillDetailDto
    {
        public int IdBill { get; set; }
        public int IdTable { get; set; }
        public string TableName { get; set; } = string.Empty;
        public DateTime DateCheckIn { get; set; }
        public int Status { get; set; }
        public int Discount { get; set; }
        public List<BillInfoDto> Items { get; set; } = new List<BillInfoDto>();

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice) * (100 - Discount) / 100;
        public double TotalCost => Items.Sum(i => i.TotalCost);
        public decimal TotalProfit => TotalAmount - (decimal)TotalCost;
    }
}