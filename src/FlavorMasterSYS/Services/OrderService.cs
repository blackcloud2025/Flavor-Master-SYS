using FlavorMasterSYS.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Services;

/// <summary>
/// Service for order management.
/// </summary>
public class OrderService
{
    private readonly DatabaseService _db;
    private readonly AuthService _auth;
    private readonly InventoryService _inventory;

    public OrderService(DatabaseService db, AuthService auth, InventoryService inventory)
    {
        _db = db;
        _auth = auth;
        _inventory = inventory;
    }

    public async Task<Order?> CreateOrderAsync(string tableNumber, List<OrderItem> items)
    {
        if (_auth.CurrentUser == null) return null;

        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var subtotal = 0m;
                foreach (var item in items)
                {
                    item.Total = item.UnitPrice * item.Quantity;
                    subtotal += item.Total;
                }

                var tax = subtotal * 0.16m; // 16% tax
                var total = subtotal + tax;
                var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

                // Create order
                var orderCommand = connection.CreateCommand();
                orderCommand.Transaction = transaction;
                orderCommand.CommandText = @"
                    INSERT INTO Orders (OrderNumber, UserId, TableNumber, Status, Subtotal, Tax, Total, CreatedAt)
                    VALUES (@OrderNumber, @UserId, @TableNumber, @Status, @Subtotal, @Tax, @Total, @CreatedAt);
                    SELECT last_insert_rowid();";

                orderCommand.Parameters.AddWithValue("@OrderNumber", orderNumber);
                orderCommand.Parameters.AddWithValue("@UserId", _auth.CurrentUser.Id);
                orderCommand.Parameters.AddWithValue("@TableNumber", tableNumber);
                orderCommand.Parameters.AddWithValue("@Status", (int)OrderStatus.Pending);
                orderCommand.Parameters.AddWithValue("@Subtotal", (double)subtotal);
                orderCommand.Parameters.AddWithValue("@Tax", (double)tax);
                orderCommand.Parameters.AddWithValue("@Total", (double)total);
                orderCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));

                var orderId = Convert.ToInt32(await orderCommand.ExecuteScalarAsync());

                // Add order items
                foreach (var item in items)
                {
                    var itemCommand = connection.CreateCommand();
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = @"
                        INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, UnitPrice, Total, Notes)
                        VALUES (@OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice, @Total, @Notes)";

                    itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                    itemCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCommand.Parameters.AddWithValue("@ProductName", item.ProductName);
                    itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("@UnitPrice", (double)item.UnitPrice);
                    itemCommand.Parameters.AddWithValue("@Total", (double)item.Total);
                    itemCommand.Parameters.AddWithValue("@Notes", item.Notes);

                    await itemCommand.ExecuteNonQueryAsync();

                    // Reduce stock
                    var stockCommand = connection.CreateCommand();
                    stockCommand.Transaction = transaction;
                    stockCommand.CommandText = @"
                        UPDATE Products SET StockQuantity = StockQuantity - @Quantity, UpdatedAt = @UpdatedAt
                        WHERE Id = @ProductId";
                    stockCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                    stockCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                    stockCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));
                    await stockCommand.ExecuteNonQueryAsync();
                }

                transaction.Commit();

                return new Order
                {
                    Id = orderId,
                    OrderNumber = orderNumber,
                    UserId = _auth.CurrentUser.Id,
                    TableNumber = tableNumber,
                    Status = OrderStatus.Pending,
                    Subtotal = subtotal,
                    Tax = tax,
                    Total = total,
                    CreatedAt = DateTime.UtcNow,
                    Items = items
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch
        {
            return null;
        }
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, OrderNumber, UserId, TableNumber, Status, Subtotal, Tax, Total, PaymentMethod, CreatedAt, CompletedAt
            FROM Orders WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", orderId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var order = ReadOrder(reader);
            order.Items = await GetOrderItemsAsync(orderId);
            return order;
        }

        return null;
    }

    private async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
    {
        var items = new List<OrderItem>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, OrderId, ProductId, ProductName, Quantity, UnitPrice, Total, Notes
            FROM OrderItems WHERE OrderId = @OrderId";
        command.Parameters.AddWithValue("@OrderId", orderId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new OrderItem
            {
                Id = reader.GetInt32(0),
                OrderId = reader.GetInt32(1),
                ProductId = reader.GetInt32(2),
                ProductName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = (decimal)reader.GetDouble(5),
                Total = (decimal)reader.GetDouble(6),
                Notes = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
            });
        }

        return items;
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var orders = new List<Order>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, OrderNumber, UserId, TableNumber, Status, Subtotal, Tax, Total, PaymentMethod, CreatedAt, CompletedAt
            FROM Orders WHERE Status = @Status ORDER BY CreatedAt DESC";
        command.Parameters.AddWithValue("@Status", (int)status);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            orders.Add(ReadOrder(reader));
        }

        return orders;
    }

    public async Task<List<Order>> GetTodayOrdersAsync()
    {
        var orders = new List<Order>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var today = DateTime.UtcNow.Date.ToString("o");
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, OrderNumber, UserId, TableNumber, Status, Subtotal, Tax, Total, PaymentMethod, CreatedAt, CompletedAt
            FROM Orders WHERE date(CreatedAt) = date(@Today) ORDER BY CreatedAt DESC";
        command.Parameters.AddWithValue("@Today", today);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            orders.Add(ReadOrder(reader));
        }

        return orders;
    }

    private static Order ReadOrder(SqliteDataReader reader)
    {
        return new Order
        {
            Id = reader.GetInt32(0),
            OrderNumber = reader.GetString(1),
            UserId = reader.GetInt32(2),
            TableNumber = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Status = (OrderStatus)reader.GetInt32(4),
            Subtotal = (decimal)reader.GetDouble(5),
            Tax = (decimal)reader.GetDouble(6),
            Total = (decimal)reader.GetDouble(7),
            PaymentMethod = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            CreatedAt = DateTime.Parse(reader.GetString(9)),
            CompletedAt = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10))
        };
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status, string? paymentMethod = null)
    {
        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            
            if (status == OrderStatus.Completed && paymentMethod != null)
            {
                command.CommandText = @"
                    UPDATE Orders SET Status = @Status, PaymentMethod = @PaymentMethod, CompletedAt = @CompletedAt
                    WHERE Id = @Id";
                command.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                command.Parameters.AddWithValue("@CompletedAt", DateTime.UtcNow.ToString("o"));
            }
            else
            {
                command.CommandText = "UPDATE Orders SET Status = @Status WHERE Id = @Id";
            }

            command.Parameters.AddWithValue("@Id", orderId);
            command.Parameters.AddWithValue("@Status", (int)status);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var data = new DashboardData();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var today = DateTime.UtcNow.Date.ToString("o");

        // Today's sales and orders
        var salesCommand = connection.CreateCommand();
        salesCommand.CommandText = @"
            SELECT COALESCE(SUM(Total), 0), COUNT(*) 
            FROM Orders WHERE date(CreatedAt) = date(@Today) AND Status = @Completed";
        salesCommand.Parameters.AddWithValue("@Today", today);
        salesCommand.Parameters.AddWithValue("@Completed", (int)OrderStatus.Completed);

        using (var reader = await salesCommand.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                data.TodaySales = (decimal)reader.GetDouble(0);
                data.TodayOrders = reader.GetInt32(1);
            }
        }

        // Active orders
        var activeCommand = connection.CreateCommand();
        activeCommand.CommandText = @"
            SELECT COUNT(*) FROM Orders WHERE Status IN (@Pending, @InProgress, @Ready)";
        activeCommand.Parameters.AddWithValue("@Pending", (int)OrderStatus.Pending);
        activeCommand.Parameters.AddWithValue("@InProgress", (int)OrderStatus.InProgress);
        activeCommand.Parameters.AddWithValue("@Ready", (int)OrderStatus.Ready);
        data.ActiveOrders = Convert.ToInt32(await activeCommand.ExecuteScalarAsync());

        // Low stock items
        var stockCommand = connection.CreateCommand();
        stockCommand.CommandText = "SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND StockQuantity <= MinStockLevel";
        data.LowStockItems = Convert.ToInt32(await stockCommand.ExecuteScalarAsync());

        // Top products
        var topCommand = connection.CreateCommand();
        topCommand.CommandText = @"
            SELECT ProductName, SUM(Quantity) as TotalQty, SUM(Total) as TotalRevenue
            FROM OrderItems oi
            JOIN Orders o ON oi.OrderId = o.Id
            WHERE o.Status = @Completed
            GROUP BY ProductName
            ORDER BY TotalQty DESC
            LIMIT 5";
        topCommand.Parameters.AddWithValue("@Completed", (int)OrderStatus.Completed);

        using (var reader = await topCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                data.TopProducts.Add(new TopSellingProduct
                {
                    ProductName = reader.GetString(0),
                    QuantitySold = reader.GetInt32(1),
                    TotalRevenue = (decimal)reader.GetDouble(2)
                });
            }
        }

        // Recent orders
        var recentCommand = connection.CreateCommand();
        recentCommand.CommandText = @"
            SELECT OrderNumber, TableNumber, Total, Status, CreatedAt
            FROM Orders ORDER BY CreatedAt DESC LIMIT 10";

        using (var reader = await recentCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                data.RecentOrders.Add(new RecentOrder
                {
                    OrderNumber = reader.GetString(0),
                    TableNumber = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Total = (decimal)reader.GetDouble(2),
                    Status = (OrderStatus)reader.GetInt32(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4))
                });
            }
        }

        return data;
    }
}
