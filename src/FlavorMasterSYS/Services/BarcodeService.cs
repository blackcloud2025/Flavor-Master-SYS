using System;

namespace FlavorMasterSYS.Services;

/// <summary>
/// Service for handling barcode scanning input.
/// </summary>
public class BarcodeService
{
    private string _buffer = string.Empty;
    private DateTime _lastKeyTime = DateTime.MinValue;
    private readonly TimeSpan _maxKeyInterval = TimeSpan.FromMilliseconds(50);
    
    public event EventHandler<string>? BarcodeScanned;

    /// <summary>
    /// Process keyboard input that may be from a barcode scanner.
    /// Barcode scanners typically send characters very quickly followed by Enter.
    /// </summary>
    public void ProcessKeyInput(char key)
    {
        var now = DateTime.Now;
        
        // If too much time has passed, start a new buffer
        if (now - _lastKeyTime > _maxKeyInterval && _buffer.Length > 0)
        {
            _buffer = string.Empty;
        }
        
        _lastKeyTime = now;
        
        if (key == '\r' || key == '\n')
        {
            // Enter key - check if we have a valid barcode
            if (_buffer.Length >= 8) // Minimum barcode length
            {
                BarcodeScanned?.Invoke(this, _buffer);
            }
            _buffer = string.Empty;
        }
        else if (char.IsLetterOrDigit(key) || key == '-')
        {
            _buffer += key;
        }
    }

    /// <summary>
    /// Manually process a barcode string (for testing or manual input).
    /// </summary>
    public void ProcessBarcode(string barcode)
    {
        if (!string.IsNullOrWhiteSpace(barcode) && barcode.Length >= 8)
        {
            BarcodeScanned?.Invoke(this, barcode.Trim());
        }
    }

    /// <summary>
    /// Clear the current buffer.
    /// </summary>
    public void ClearBuffer()
    {
        _buffer = string.Empty;
    }

    /// <summary>
    /// Validates a barcode format (basic validation).
    /// </summary>
    public static bool IsValidBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        if (barcode.Length < 8 || barcode.Length > 14) return false;
        
        foreach (var c in barcode)
        {
            if (!char.IsLetterOrDigit(c) && c != '-') return false;
        }
        
        return true;
    }
}
