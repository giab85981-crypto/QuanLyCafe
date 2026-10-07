namespace CafeManagement.API.Dtos;

public class DashboardSummaryDto
{
    public decimal TodayRevenue { get; set; }
    public decimal TodayRefund { get; set; }
    public int TodayOrderCount { get; set; }
    public decimal TodayDiscount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double? AverageGuestsPerOrder { get; set; }
    public int OccupiedTables { get; set; }
    public int TotalTables { get; set; }
    public double OccupancyRate { get; set; }
    public int ServingOrders { get; set; }
    public int ServingGuests { get; set; }
    public int UnknownServingGuestOrders { get; set; }
}
public class TopSellingFoodDto
{
    public int Stt { get; set; }
    public int FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
public class CategorySalesDto
{
    public string Key { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
public class DailyRevenueDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public int GuestCount { get; set; }
}
public class ChartSummaryDto
{
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public int GuestCount { get; set; }
    public int UnknownGuestOrders { get; set; }
    public int EstimatedRevenueOrders { get; set; }
    public List<DailyRevenueDto> Points { get; set; } = new();
}
public class MenuPerformanceDto
{
    public decimal AverageItemValue { get; set; }
    public decimal AverageFoodValue { get; set; }
    public decimal AverageDrinkValue { get; set; }
    public int UnclassifiedQuantity { get; set; }
    public List<CategorySalesDto> Sales { get; set; } = new();
    public List<CategorySalesDto> FilterOptions { get; set; } = new();
    public List<TopSellingFoodDto> TopSellingFoods { get; set; } = new();
}
public class DashboardOverviewResponse
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public ChartSummaryDto Revenue { get; set; } = new();
    public ChartSummaryDto Customers { get; set; } = new();
    public MenuPerformanceDto Menu { get; set; } = new();
    // Keep existing response fields for older clients.
    public List<TopSellingFoodDto> TopSellingFoods { get; set; } = new();
    public List<CategorySalesDto> CategorySales { get; set; } = new();
    public List<DailyRevenueDto> RevenueChart { get; set; } = new();
}
