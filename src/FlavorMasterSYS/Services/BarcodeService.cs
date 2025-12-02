using System;

namespace FlavorMasterSYS.Services;

/// <summary>
/// Service for handling barcode scanning input.
/// </summary>
public class BarcodeService
{
    public event EventHandler<string>? BarcodeScanned;

    /// <summary>
    /// Manually process a barcode string (for manual input).
    /// </summary>
    public void ProcessBarcode(string barcode)
    {
        if (!string.IsNullOrWhiteSpace(barcode) && barcode.Length >= AppConfig.MinBarcodeLength)
        {
            BarcodeScanned?.Invoke(this, barcode.Trim());
        }
    }

    /// <summary>
    /// Validates a barcode format (basic validation).
    /// </summary>
    public static bool IsValidBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        if (barcode.Length < AppConfig.MinBarcodeLength || barcode.Length > AppConfig.MaxBarcodeLength) return false;
        
        foreach (var c in barcode)
        {
            if (!char.IsLetterOrDigit(c) && c != '-') return false;
        }
        
        return true;
    }
}
