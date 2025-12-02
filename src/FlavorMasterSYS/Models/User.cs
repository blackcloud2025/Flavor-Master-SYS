namespace FlavorMasterSYS.Models;

/// <summary>
/// User roles in the system.
/// </summary>
public enum UserRole
{
    /// <summary>Master administrator with full access.</summary>
    Master = 0,
    /// <summary>Manager with limited administrative access.</summary>
    Manager = 1,
    /// <summary>Cashier with POS access.</summary>
    Cashier = 2,
    /// <summary>Kitchen staff with order view access.</summary>
    Kitchen = 3,
    /// <summary>Waiter with order taking access.</summary>
    Waiter = 4
}

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CanViewDashboard { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}
