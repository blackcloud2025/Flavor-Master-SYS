using FlavorMasterSYS.Models;
using FlavorMasterSYS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Views;

/// <summary>
/// Point of Sale page for order taking.
/// </summary>
public sealed partial class POSPage : Page
{
    private readonly InventoryService _inventoryService;
    private readonly OrderService _orderService;
    private readonly TicketService _ticketService;
    private readonly BarcodeService _barcodeService;

    private readonly ObservableCollection<OrderItem> _orderItems = [];
    private decimal _subtotal;
    private decimal _tax;
    private decimal _total;

    public POSPage(InventoryService inventoryService, OrderService orderService, TicketService ticketService, BarcodeService barcodeService)
    {
        this.InitializeComponent();
        _inventoryService = inventoryService;
        _orderService = orderService;
        _ticketService = ticketService;
        _barcodeService = barcodeService;

        OrderItemsListView.ItemsSource = _orderItems;

        _barcodeService.BarcodeScanned += BarcodeService_BarcodeScanned;
    }

    public async Task LoadDataAsync()
    {
        var categories = await _inventoryService.GetCategoriesAsync();
        CategoryGridView.ItemsSource = categories;

        if (categories.Count > 0)
        {
            CategoryGridView.SelectedIndex = 0;
        }
    }

    private async void CategoryGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryGridView.SelectedItem is string category)
        {
            var products = await _inventoryService.GetProductsByCategoryAsync(category);
            ProductsGridView.ItemsSource = products;
        }
    }

    private void ProductsGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Product product)
        {
            AddProductToOrder(product);
        }
    }

    private void AddProductToOrder(Product product)
    {
        var existingItem = _orderItems.FirstOrDefault(i => i.ProductId == product.Id);
        
        if (existingItem != null)
        {
            existingItem.Quantity++;
            existingItem.Total = existingItem.UnitPrice * existingItem.Quantity;
        }
        else
        {
            _orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = product.Price,
                Total = product.Price
            });
        }

        UpdateTotals();
        RefreshOrderList();
    }

    private void UpdateTotals()
    {
        _subtotal = _orderItems.Sum(i => i.Total);
        _tax = _subtotal * 0.16m;
        _total = _subtotal + _tax;

        SubtotalTextBlock.Text = $"${_subtotal:F2}";
        TaxTextBlock.Text = $"${_tax:F2}";
        TotalTextBlock.Text = $"${_total:F2}";
    }

    private void RefreshOrderList()
    {
        OrderItemsListView.ItemsSource = null;
        OrderItemsListView.ItemsSource = _orderItems;
    }

    private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is OrderItem item)
        {
            item.Quantity++;
            item.Total = item.UnitPrice * item.Quantity;
            UpdateTotals();
            RefreshOrderList();
        }
    }

    private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is OrderItem item)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
                item.Total = item.UnitPrice * item.Quantity;
            }
            else
            {
                _orderItems.Remove(item);
            }
            UpdateTotals();
            RefreshOrderList();
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is OrderItem item)
        {
            _orderItems.Remove(item);
            UpdateTotals();
            RefreshOrderList();
        }
    }

    private void ClearOrderButton_Click(object sender, RoutedEventArgs e)
    {
        _orderItems.Clear();
        TableNumberTextBox.Text = string.Empty;
        UpdateTotals();
        RefreshOrderList();
    }

    private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_orderItems.Count == 0)
        {
            await ShowMessageAsync("Error", "No hay productos en la orden / No products in order");
            return;
        }

        // Show payment method dialog
        var paymentMethods = new[] { "Efectivo / Cash", "Tarjeta / Card", "Otro / Other" };
        var combo = new ComboBox { ItemsSource = paymentMethods, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };

        var dialog = new ContentDialog
        {
            Title = "Método de Pago / Payment Method",
            Content = combo,
            PrimaryButtonText = "Cobrar / Charge",
            CloseButtonText = "Cancelar / Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var paymentMethod = combo.SelectedItem?.ToString() ?? "Cash";
            
            var order = await _orderService.CreateOrderAsync(
                TableNumberTextBox.Text, 
                _orderItems.ToList());

            if (order != null)
            {
                // Update status to completed
                await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Completed, paymentMethod);

                // Generate ticket
                var ticket = await _ticketService.GenerateTicketAsync(order);

                var message = $"Orden completada: {order.OrderNumber}";
                if (ticket != null)
                {
                    message += $"\nTicket generado: {ticket.TicketNumber}";
                    message += $"\nGuardado en: {ticket.PdfPath}";
                }

                await ShowMessageAsync("¡Éxito! / Success!", message);

                // Clear order
                _orderItems.Clear();
                TableNumberTextBox.Text = string.Empty;
                UpdateTotals();
                RefreshOrderList();
            }
            else
            {
                await ShowMessageAsync("Error", "No se pudo crear la orden / Could not create order");
            }
        }
    }

    private async void BarcodeTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await SearchByBarcodeAsync();
        }
    }

    private async void SearchBarcodeButton_Click(object sender, RoutedEventArgs e)
    {
        await SearchByBarcodeAsync();
    }

    private async Task SearchByBarcodeAsync()
    {
        var barcode = BarcodeTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(barcode)) return;

        var product = await _inventoryService.GetProductByBarcodeAsync(barcode);
        if (product != null)
        {
            AddProductToOrder(product);
            BarcodeTextBox.Text = string.Empty;
        }
        else
        {
            await ShowMessageAsync("No encontrado / Not Found", $"Producto con código '{barcode}' no encontrado");
        }
    }

    private async void BarcodeService_BarcodeScanned(object? sender, string barcode)
    {
        var product = await _inventoryService.GetProductByBarcodeAsync(barcode);
        if (product != null)
        {
            DispatcherQueue.TryEnqueue(() => AddProductToOrder(product));
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
