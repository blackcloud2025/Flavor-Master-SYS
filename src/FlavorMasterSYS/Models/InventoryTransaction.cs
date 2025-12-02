namespace FlavorMasterSYS.Models;

/// <summary>
/// Represents an inventory transaction.
/// </summary>
public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "IN", "OUT", "ADJUSTMENT"
    public string Notes { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
