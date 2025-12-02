using FlavorMasterSYS.Models;
using FlavorMasterSYS.Services;
using FlavorMasterSYS.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FlavorMasterSYS;

/// <summary>
/// Main window for the Flavor Master POS Restaurant System.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly AuthService _authService;
    private readonly InventoryService _inventoryService;
    private readonly OrderService _orderService;
    private readonly TicketService _ticketService;
    private readonly BarcodeService _barcodeService;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _timer;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Flavor Master - POS Restaurant System";

        // Initialize services
        _databaseService = DatabaseService.Instance;
        _authService = new AuthService(_databaseService);
        _inventoryService = new InventoryService(_databaseService, _authService);
        _orderService = new OrderService(_databaseService, _authService, _inventoryService);
        _ticketService = new TicketService(_databaseService, _authService);
        _barcodeService = new BarcodeService();

        // Initialize app
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            // Initialize database
            await _databaseService.InitializeDatabaseAsync();
            
            // Start timer for clock
            StartClock();

            // Show login page
            ShowLoginPage();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
        }
    }

    private void StartClock()
    {
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateTime();
        _timer.Start();
        UpdateTime();
    }

    private void UpdateTime()
    {
        TimeTextBlock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void ShowLoginPage()
    {
        NavigationPanel.Visibility = Visibility.Collapsed;
        LogoutButton.Visibility = Visibility.Collapsed;
        UserInfoTextBlock.Text = string.Empty;

        var loginPage = new LoginPage(_authService);
        loginPage.LoginSuccessful += LoginPage_LoginSuccessful;
        ContentFrame.Content = loginPage;
    }

    private async void LoginPage_LoginSuccessful(object? sender, EventArgs e)
    {
        var user = _authService.CurrentUser;
        if (user == null) return;

        // Show navigation and user info
        NavigationPanel.Visibility = Visibility.Visible;
        LogoutButton.Visibility = Visibility.Visible;
        UserInfoTextBlock.Text = $"👤 {user.FullName} ({user.Role})";

        // Show/hide user management based on role
        NavUsers.Visibility = _authService.IsMaster ? Visibility.Visible : Visibility.Collapsed;

        // Show/hide dashboard based on permission
        NavDashboard.Visibility = (user.CanViewDashboard || _authService.IsMaster) 
            ? Visibility.Visible 
            : Visibility.Collapsed;

        StatusTextBlock.Text = $"Bienvenido / Welcome, {user.FullName}!";

        // Navigate to POS by default
        await NavigateToPOSAsync();
    }

    private async void NavPOS_Click(object sender, RoutedEventArgs e)
    {
        await NavigateToPOSAsync();
    }

    private async Task NavigateToPOSAsync()
    {
        var posPage = new POSPage(_inventoryService, _orderService, _ticketService, _barcodeService);
        ContentFrame.Content = posPage;
        await posPage.LoadDataAsync();
        StatusTextBlock.Text = "POS - Punto de Venta";
    }

    private async void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        var dashboardPage = new DashboardPage(_orderService);
        ContentFrame.Content = dashboardPage;
        await dashboardPage.LoadDataAsync();
        StatusTextBlock.Text = "Dashboard - Panel de Control";
    }

    private async void NavInventory_Click(object sender, RoutedEventArgs e)
    {
        var inventoryPage = new InventoryPage(_inventoryService);
        ContentFrame.Content = inventoryPage;
        await inventoryPage.LoadDataAsync();
        StatusTextBlock.Text = "Inventario / Inventory";
    }

    private async void NavUsers_Click(object sender, RoutedEventArgs e)
    {
        if (!_authService.IsMaster)
        {
            StatusTextBlock.Text = "Acceso denegado / Access denied";
            return;
        }

        var usersPage = new UserManagementPage(_authService);
        ContentFrame.Content = usersPage;
        await usersPage.LoadUsersAsync();
        StatusTextBlock.Text = "Gestión de Usuarios / User Management";
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _authService.Logout();
        StatusTextBlock.Text = "Sesión cerrada / Logged out";
        ShowLoginPage();
    }
}
