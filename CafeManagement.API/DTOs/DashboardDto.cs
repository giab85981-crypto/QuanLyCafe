namespace CafeManagement.API.Dtos
{
    // Tổng quan các thẻ KPI
    public class DashboardSummaryDto
    {
        public decimal TodayRevenue { get; set; }        // Doanh thu hôm nay
        public int TodayOrderCount { get; set; }         // Số đơn hôm nay
        public int OccupiedTables { get; set; }          // Số bàn đang có khách
        public int TotalTables { get; set; }             // Tổng số bàn
        public double OccupancyRate { get; set; }        // Tỷ lệ phủ bàn (%)
    }

    // Top món bán chạy
    public class TopSellingFoodDto
    {
        public int Stt { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Thống kê theo nhóm món (Dùng cho biểu đồ Donut)
    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Doanh thu theo ngày (Dùng cho biểu đồ Cột/Đường)
    public class DailyRevenueDto
    {
        public string Date { get; set; } = string.Empty; // Định dạng "dd/MM"
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    // Response tổng hợp cho Màn hình Tổng quan
    public class DashboardOverviewResponse
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public List<TopSellingFoodDto> TopSellingFoods { get; set; } = new();
        public List<CategorySalesDto> CategorySales { get; set; } = new();
        public List<DailyRevenueDto> RevenueChart { get; set; } = new();
    }
}