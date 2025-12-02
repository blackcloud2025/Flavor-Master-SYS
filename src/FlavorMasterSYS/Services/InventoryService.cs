using FlavorMasterSYS.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Services;

/// <summary>
/// Service for inventory and product management.
/// </summary>
public class InventoryService
{
    private readonly DatabaseService _db;
    private readonly AuthService _auth;

    public InventoryService(DatabaseService db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        var products = new List<Product>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Description, Category, Price, Barcode, StockQuantity, MinStockLevel, IsActive, CreatedAt, UpdatedAt
            FROM Products WHERE IsActive = 1 ORDER BY Category, Name";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Description, Category, Price, Barcode, StockQuantity, MinStockLevel, IsActive, CreatedAt, UpdatedAt
            FROM Products WHERE Barcode = @Barcode AND IsActive = 1";
        command.Parameters.AddWithValue("@Barcode", barcode);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadProduct(reader);
        }

        return null;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Description, Category, Price, Barcode, StockQuantity, MinStockLevel, IsActive, CreatedAt, UpdatedAt
            FROM Products WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadProduct(reader);
        }

        return null;
    }

    private static Product ReadProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Category = reader.GetString(3),
            Price = (decimal)reader.GetDouble(4),
            Barcode = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            StockQuantity = reader.GetInt32(6),
            MinStockLevel = reader.GetInt32(7),
            IsActive = reader.GetInt32(8) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(9)),
            UpdatedAt = DateTime.Parse(reader.GetString(10))
        };
    }

    public async Task<bool> CreateProductAsync(Product product)
    {
        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var now = DateTime.UtcNow.ToString("o");
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Products (Name, Description, Category, Price, Barcode, StockQuantity, MinStockLevel, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Name, @Description, @Category, @Price, @Barcode, @StockQuantity, @MinStockLevel, 1, @CreatedAt, @UpdatedAt)";

            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Description", product.Description);
            command.Parameters.AddWithValue("@Category", product.Category);
            command.Parameters.AddWithValue("@Price", (double)product.Price);
            command.Parameters.AddWithValue("@Barcode", product.Barcode);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@MinStockLevel", product.MinStockLevel);
            command.Parameters.AddWithValue("@CreatedAt", now);
            command.Parameters.AddWithValue("@UpdatedAt", now);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Products SET 
                    Name = @Name, 
                    Description = @Description, 
                    Category = @Category, 
                    Price = @Price, 
                    Barcode = @Barcode, 
                    StockQuantity = @StockQuantity, 
                    MinStockLevel = @MinStockLevel,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Description", product.Description);
            command.Parameters.AddWithValue("@Category", product.Category);
            command.Parameters.AddWithValue("@Price", (double)product.Price);
            command.Parameters.AddWithValue("@Barcode", product.Barcode);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@MinStockLevel", product.MinStockLevel);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AdjustStockAsync(int productId, int quantityChange, string transactionType, string notes)
    {
        if (_auth.CurrentUser == null) return false;

        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update stock
                var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE Products SET StockQuantity = StockQuantity + @QuantityChange, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";
                updateCommand.Parameters.AddWithValue("@Id", productId);
                updateCommand.Parameters.AddWithValue("@QuantityChange", quantityChange);
                updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));
                await updateCommand.ExecuteNonQueryAsync();

                // Log transaction
                var logCommand = connection.CreateCommand();
                logCommand.Transaction = transaction;
                logCommand.CommandText = @"
                    INSERT INTO InventoryTransactions (ProductId, QuantityChange, TransactionType, Notes, UserId, CreatedAt)
                    VALUES (@ProductId, @QuantityChange, @TransactionType, @Notes, @UserId, @CreatedAt)";
                logCommand.Parameters.AddWithValue("@ProductId", productId);
                logCommand.Parameters.AddWithValue("@QuantityChange", quantityChange);
                logCommand.Parameters.AddWithValue("@TransactionType", transactionType);
                logCommand.Parameters.AddWithValue("@Notes", notes);
                logCommand.Parameters.AddWithValue("@UserId", _auth.CurrentUser.Id);
                logCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                await logCommand.ExecuteNonQueryAsync();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        var products = new List<Product>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Description, Category, Price, Barcode, StockQuantity, MinStockLevel, IsActive, CreatedAt, UpdatedAt
            FROM Products WHERE IsActive = 1 AND StockQuantity <= MinStockLevel ORDER BY StockQuantity";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var categories = new List<string>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT Category FROM Products WHERE IsActive = 1 ORDER BY Category";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(reader.GetString(0));
        }

        return categories;
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(string category)
    {
        var products = new List<Product>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Description, Category, Price, Barcode, StockQuantity, MinStockLevel, IsActive, CreatedAt, UpdatedAt
            FROM Products WHERE Category = @Category AND IsActive = 1 ORDER BY Name";
        command.Parameters.AddWithValue("@Category", category);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }
}
