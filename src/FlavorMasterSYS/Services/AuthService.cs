using FlavorMasterSYS.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Services;

/// <summary>
/// Service for user authentication and management.
/// </summary>
public class AuthService
{
    private readonly DatabaseService _db;
    private User? _currentUser;

    public User? CurrentUser => _currentUser;
    public bool IsLoggedIn => _currentUser != null;
    public bool IsMaster => _currentUser?.Role == UserRole.Master;

    public AuthService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Username, PasswordHash, FullName, Role, IsActive, CanViewDashboard, CreatedAt, LastLogin
            FROM Users 
            WHERE Username = @Username AND IsActive = 1";
        command.Parameters.AddWithValue("@Username", username);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var storedHash = reader.GetString(2);
            var inputHash = DatabaseService.HashPassword(password);

            if (storedHash == inputHash)
            {
                _currentUser = new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = storedHash,
                    FullName = reader.GetString(3),
                    Role = (UserRole)reader.GetInt32(4),
                    IsActive = reader.GetInt32(5) == 1,
                    CanViewDashboard = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    LastLogin = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
                };

                // Update last login
                await UpdateLastLoginAsync(_currentUser.Id);
                return _currentUser;
            }
        }

        return null;
    }

    private async Task UpdateLastLoginAsync(int userId)
    {
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET LastLogin = @LastLogin WHERE Id = @Id";
        command.Parameters.AddWithValue("@LastLogin", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@Id", userId);
        await command.ExecuteNonQueryAsync();
    }

    public void Logout()
    {
        _currentUser = null;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        using var connection = _db.GetConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Username, FullName, Role, IsActive, CanViewDashboard, CreatedAt, LastLogin
            FROM Users ORDER BY Role, FullName";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                FullName = reader.GetString(2),
                Role = (UserRole)reader.GetInt32(3),
                IsActive = reader.GetInt32(4) == 1,
                CanViewDashboard = reader.GetInt32(5) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                LastLogin = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
            });
        }

        return users;
    }

    public async Task<bool> CreateUserAsync(User user, string password)
    {
        if (!IsMaster) return false;

        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CanViewDashboard, CreatedAt)
                VALUES (@Username, @PasswordHash, @FullName, @Role, @IsActive, @CanViewDashboard, @CreatedAt)";

            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", DatabaseService.HashPassword(password));
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Role", (int)user.Role);
            command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@CanViewDashboard", user.CanViewDashboard ? 1 : 0);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(User user, string? newPassword = null)
    {
        if (!IsMaster) return false;

        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            
            if (!string.IsNullOrEmpty(newPassword))
            {
                command.CommandText = @"
                    UPDATE Users SET 
                        FullName = @FullName, 
                        Role = @Role, 
                        IsActive = @IsActive, 
                        CanViewDashboard = @CanViewDashboard,
                        PasswordHash = @PasswordHash
                    WHERE Id = @Id";
                command.Parameters.AddWithValue("@PasswordHash", DatabaseService.HashPassword(newPassword));
            }
            else
            {
                command.CommandText = @"
                    UPDATE Users SET 
                        FullName = @FullName, 
                        Role = @Role, 
                        IsActive = @IsActive, 
                        CanViewDashboard = @CanViewDashboard
                    WHERE Id = @Id";
            }

            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Role", (int)user.Role);
            command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@CanViewDashboard", user.CanViewDashboard ? 1 : 0);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        if (!IsMaster) return false;
        if (_currentUser?.Id == userId) return false; // Can't delete yourself

        try
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET IsActive = 0 WHERE Id = @Id AND Role != 0";
            command.Parameters.AddWithValue("@Id", userId);

            var affected = await command.ExecuteNonQueryAsync();
            return affected > 0;
        }
        catch
        {
            return false;
        }
    }
}
