namespace CafeManagement.API.DTOs
{
    public class RevenueReportDto
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
        public double TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalBills { get; set; }
    }

    public class TopFoodReportDto
    {
        public string FoodName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}