namespace FlavorMasterSYS;

/// <summary>
/// Application-wide configuration constants.
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// Tax rate applied to orders (16% = 0.16).
    /// </summary>
    public const decimal TaxRate = 0.16m;

    /// <summary>
    /// Minimum barcode length for validation.
    /// </summary>
    public const int MinBarcodeLength = 8;

    /// <summary>
    /// Maximum barcode length for validation.
    /// </summary>
    public const int MaxBarcodeLength = 14;

    /// <summary>
    /// Application name.
    /// </summary>
    public const string AppName = "Flavor Master";

    /// <summary>
    /// Database file name.
    /// </summary>
    public const string DatabaseFileName = "flavormaster.db";

    /// <summary>
    /// Tickets folder name.
    /// </summary>
    public const string TicketsFolderName = "Tickets";
}
