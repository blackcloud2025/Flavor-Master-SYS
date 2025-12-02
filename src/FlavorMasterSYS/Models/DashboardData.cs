namespace FlavorMasterSYS.Models;

/// <summary>
/// Dashboard configuration and statistics.
/// </summary>
public class DashboardData
{
    public decimal TodaySales { get; set; }
    public int TodayOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int LowStockItems { get; set; }
    public List<TopSellingProduct> TopProducts { get; set; } = [];
    public List<RecentOrder> RecentOrders { get; set; } = [];
}

/// <summary>
/// Top selling product data.
/// </summary>
public class TopSellingProduct
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// Recent order summary.
/// </summary>
public class RecentOrder
{
    public string OrderNumber { get; set; } = string.Empty;
    public string TableNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
