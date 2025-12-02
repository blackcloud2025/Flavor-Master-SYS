namespace FlavorMasterSYS.Models;

/// <summary>
/// Represents an order in the system.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

/// <summary>
/// Represents an item in an order.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Order status values.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    InProgress = 1,
    Ready = 2,
    Completed = 3,
    Cancelled = 4
}
