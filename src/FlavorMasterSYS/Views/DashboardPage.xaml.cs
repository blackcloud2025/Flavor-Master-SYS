using FlavorMasterSYS.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Views;

/// <summary>
/// Dashboard page showing system statistics and overview.
/// </summary>
public sealed partial class DashboardPage : Page
{
    private readonly OrderService _orderService;

    public DashboardPage(OrderService orderService)
    {
        this.InitializeComponent();
        _orderService = orderService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var data = await _orderService.GetDashboardDataAsync();
            
            TodaySalesText.Text = $"${data.TodaySales:F2}";
            TodayOrdersText.Text = data.TodayOrders.ToString();
            ActiveOrdersText.Text = data.ActiveOrders.ToString();
            LowStockText.Text = data.LowStockItems.ToString();

            TopProductsListView.ItemsSource = data.TopProducts;
            RecentOrdersListView.ItemsSource = data.RecentOrders;
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
        }
    }
}
