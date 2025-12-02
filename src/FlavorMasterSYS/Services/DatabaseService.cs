using FlavorMasterSYS.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Services;

/// <summary>
/// SQLite database service for local data storage.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;
    private static DatabaseService? _instance;
    private static readonly object _lock = new();

    public static DatabaseService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new DatabaseService();
                }
            }
            return _instance;
        }
    }

    private DatabaseService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlavorMasterSYS");
        
        Directory.CreateDirectory(appDataPath);
        var dbPath = Path.Combine(appDataPath, "flavormaster.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CanViewDashboard INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                LastLogin TEXT
            );

            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Category TEXT NOT NULL,
                Price REAL NOT NULL,
                Barcode TEXT,
                StockQuantity INTEGER NOT NULL DEFAULT 0,
                MinStockLevel INTEGER NOT NULL DEFAULT 5,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderNumber TEXT NOT NULL UNIQUE,
                UserId INTEGER NOT NULL,
                TableNumber TEXT,
                Status INTEGER NOT NULL DEFAULT 0,
                Subtotal REAL NOT NULL,
                Tax REAL NOT NULL,
                Total REAL NOT NULL,
                PaymentMethod TEXT,
                CreatedAt TEXT NOT NULL,
                CompletedAt TEXT,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );

            CREATE TABLE IF NOT EXISTS OrderItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                Total REAL NOT NULL,
                Notes TEXT,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                FOREIGN KEY (ProductId) REFERENCES Products(Id)
            );

            CREATE TABLE IF NOT EXISTS Tickets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                TicketNumber TEXT NOT NULL UNIQUE,
                PdfPath TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedByUserId INTEGER NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
            );

            CREATE TABLE IF NOT EXISTS InventoryTransactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductId INTEGER NOT NULL,
                QuantityChange INTEGER NOT NULL,
                TransactionType TEXT NOT NULL,
                Notes TEXT,
                UserId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (ProductId) REFERENCES Products(Id),
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );

            CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
            CREATE INDEX IF NOT EXISTS idx_orders_status ON Orders(Status);
            CREATE INDEX IF NOT EXISTS idx_orders_created ON Orders(CreatedAt);
        ";

        await command.ExecuteNonQueryAsync();

        // Create default master user if not exists
        await CreateDefaultMasterUserAsync(connection);
    }

    private async Task CreateDefaultMasterUserAsync(SqliteConnection connection)
    {
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Role = 0";
        var count = (long)(await checkCommand.ExecuteScalarAsync() ?? 0);

        if (count == 0)
        {
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CanViewDashboard, CreatedAt)
                VALUES (@Username, @PasswordHash, @FullName, @Role, 1, 1, @CreatedAt)";
            
            insertCommand.Parameters.AddWithValue("@Username", "master");
            insertCommand.Parameters.AddWithValue("@PasswordHash", HashPassword("master123"));
            insertCommand.Parameters.AddWithValue("@FullName", "System Administrator");
            insertCommand.Parameters.AddWithValue("@Role", (int)UserRole.Master);
            insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
            
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public SqliteConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
