using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace FlavorMasterSYS;

/// <summary>
/// Main window for the Flavor Master POS Restaurant System.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Collection of menu categories.
    /// </summary>
    public ObservableCollection<string> Categories { get; } =
    [
        "Appetizers",
        "Main Course",
        "Beverages",
        "Desserts",
        "Specials"
    ];

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Flavor Master - POS Restaurant System";
        UpdateTime();
    }

    private void UpdateTime()
    {
        TimeTextBlock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void CheckoutButton_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Processing checkout...";
        // TODO: Implement checkout logic
    }
}
