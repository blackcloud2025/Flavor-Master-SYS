namespace FlavorMasterSYS.Models;

/// <summary>
/// Represents a ticket/receipt for an order.
/// </summary>
public class Ticket
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string PdfPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
