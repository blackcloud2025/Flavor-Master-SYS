using FlavorMasterSYS.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Services;

/// <summary>
/// Service for generating PDF tickets/receipts.
/// </summary>
public class TicketService
{
    private readonly DatabaseService _db;
    private readonly AuthService _auth;
    private readonly string _ticketsPath;

    public TicketService(DatabaseService db, AuthService auth)
    {
        _db = db;
        _auth = auth;
        _ticketsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlavorMasterSYS",
            "Tickets");
        Directory.CreateDirectory(_ticketsPath);
    }

    public async Task<Ticket?> GenerateTicketAsync(Order order)
    {
        if (_auth.CurrentUser == null) return null;

        try
        {
            var ticketNumber = $"TKT-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var pdfPath = Path.Combine(_ticketsPath, $"{ticketNumber}.pdf");

            // Generate PDF content
            var pdfContent = GeneratePdfContent(order, ticketNumber);
            await File.WriteAllBytesAsync(pdfPath, pdfContent);

            // Save to database
            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tickets (OrderId, TicketNumber, PdfPath, CreatedAt, CreatedByUserId)
                VALUES (@OrderId, @TicketNumber, @PdfPath, @CreatedAt, @CreatedByUserId);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@OrderId", order.Id);
            command.Parameters.AddWithValue("@TicketNumber", ticketNumber);
            command.Parameters.AddWithValue("@PdfPath", pdfPath);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@CreatedByUserId", _auth.CurrentUser.Id);

            var ticketId = Convert.ToInt32(await command.ExecuteScalarAsync());

            return new Ticket
            {
                Id = ticketId,
                OrderId = order.Id,
                TicketNumber = ticketNumber,
                PdfPath = pdfPath,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _auth.CurrentUser.Id
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a simple PDF document.
    /// This creates a basic PDF structure without external libraries.
    /// For production, consider using a library like QuestPDF or iTextSharp.
    /// </summary>
    private byte[] GeneratePdfContent(Order order, string ticketNumber)
    {
        // Simple PDF structure
        var content = new StringBuilder();
        
        // PDF Header
        content.AppendLine("%PDF-1.4");
        content.AppendLine("1 0 obj");
        content.AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
        content.AppendLine("endobj");
        
        content.AppendLine("2 0 obj");
        content.AppendLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        content.AppendLine("endobj");
        
        content.AppendLine("3 0 obj");
        content.AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 600] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
        content.AppendLine("endobj");

        // Build receipt text
        var receiptText = new StringBuilder();
        receiptText.AppendLine("BT");
        receiptText.AppendLine("/F1 14 Tf");
        receiptText.AppendLine("50 580 Td");
        receiptText.AppendLine("(FLAVOR MASTER) Tj");
        receiptText.AppendLine("/F1 10 Tf");
        receiptText.AppendLine("0 -20 Td");
        receiptText.AppendLine("(POS Restaurant System) Tj");
        receiptText.AppendLine("0 -30 Td");
        receiptText.AppendLine("(================================) Tj");
        receiptText.AppendLine($"0 -15 Td");
        receiptText.AppendLine($"(Ticket: {ticketNumber}) Tj");
        receiptText.AppendLine($"0 -15 Td");
        receiptText.AppendLine($"(Order: {order.OrderNumber}) Tj");
        receiptText.AppendLine($"0 -15 Td");
        receiptText.AppendLine($"(Table: {order.TableNumber}) Tj");
        receiptText.AppendLine($"0 -15 Td");
        receiptText.AppendLine($"(Date: {order.CreatedAt:yyyy-MM-dd HH:mm}) Tj");
        receiptText.AppendLine("0 -20 Td");
        receiptText.AppendLine("(--------------------------------) Tj");

        foreach (var item in order.Items)
        {
            receiptText.AppendLine("0 -15 Td");
            var itemLine = $"{item.Quantity}x {item.ProductName}";
            if (itemLine.Length > 25) itemLine = itemLine[..25];
            receiptText.AppendLine($"({itemLine}) Tj");
            receiptText.AppendLine("0 -12 Td");
            receiptText.AppendLine($"(   ${item.Total:F2}) Tj");
        }

        var taxPercent = (int)(AppConfig.TaxRate * 100);
        receiptText.AppendLine("0 -20 Td");
        receiptText.AppendLine("(--------------------------------) Tj");
        receiptText.AppendLine("0 -15 Td");
        receiptText.AppendLine($"(Subtotal: ${order.Subtotal:F2}) Tj");
        receiptText.AppendLine("0 -15 Td");
        receiptText.AppendLine($"(Tax \\({taxPercent}%\\): ${order.Tax:F2}) Tj");
        receiptText.AppendLine("0 -15 Td");
        receiptText.AppendLine($"(TOTAL: ${order.Total:F2}) Tj");
        receiptText.AppendLine("0 -30 Td");
        receiptText.AppendLine("(================================) Tj");
        receiptText.AppendLine("0 -15 Td");
        receiptText.AppendLine("(Thank you for your visit!) Tj");
        receiptText.AppendLine("ET");

        var streamContent = receiptText.ToString();
        content.AppendLine("4 0 obj");
        content.AppendLine($"<< /Length {streamContent.Length} >>");
        content.AppendLine("stream");
        content.Append(streamContent);
        content.AppendLine("endstream");
        content.AppendLine("endobj");

        // Font
        content.AppendLine("5 0 obj");
        content.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        content.AppendLine("endobj");

        // XRef table
        content.AppendLine("xref");
        content.AppendLine("0 6");
        content.AppendLine("0000000000 65535 f");
        content.AppendLine("0000000009 00000 n");
        content.AppendLine("0000000058 00000 n");
        content.AppendLine("0000000115 00000 n");
        content.AppendLine("0000000270 00000 n");
        content.AppendLine("0000000500 00000 n");

        content.AppendLine("trailer");
        content.AppendLine("<< /Size 6 /Root 1 0 R >>");
        content.AppendLine("startxref");
        content.AppendLine("600");
        content.AppendLine("%%EOF");

        return Encoding.UTF8.GetBytes(content.ToString());
    }
}
