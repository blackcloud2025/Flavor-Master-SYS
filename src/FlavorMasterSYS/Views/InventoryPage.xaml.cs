using FlavorMasterSYS.Models;
using FlavorMasterSYS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Views;

/// <summary>
/// Inventory management page.
/// </summary>
public sealed partial class InventoryPage : Page
{
    private readonly InventoryService _inventoryService;
    private List<Product> _allProducts = [];
    private bool _showLowStockOnly;

    public InventoryPage(InventoryService inventoryService)
    {
        this.InitializeComponent();
        _inventoryService = inventoryService;
    }

    public async Task LoadDataAsync()
    {
        _allProducts = await _inventoryService.GetAllProductsAsync();
        var categories = await _inventoryService.GetCategoriesAsync();
        
        CategoryFilter.ItemsSource = new[] { "Todas / All" }.Concat(categories).ToList();
        CategoryFilter.SelectedIndex = 0;
        
        FilterProducts();
    }

    private void FilterProducts()
    {
        var filtered = _allProducts.AsEnumerable();

        // Search filter
        var search = SearchBox.Text?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(p => 
                p.Name.ToLower().Contains(search) || 
                p.Barcode.ToLower().Contains(search));
        }

        // Category filter
        if (CategoryFilter.SelectedIndex > 0)
        {
            var category = CategoryFilter.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(category))
            {
                filtered = filtered.Where(p => p.Category == category);
            }
        }

        // Low stock filter
        if (_showLowStockOnly)
        {
            filtered = filtered.Where(p => p.StockQuantity <= p.MinStockLevel);
        }

        ProductsListView.ItemsSource = filtered.ToList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterProducts();
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FilterProducts();
    }

    private void LowStockFilter_Click(object sender, RoutedEventArgs e)
    {
        _showLowStockOnly = LowStockFilter.IsChecked ?? false;
        FilterProducts();
    }

    private async void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowProductDialogAsync(null);
    }

    private async void EditProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Product product)
        {
            await ShowProductDialogAsync(product);
        }
    }

    private async void AddStockButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Product product)
        {
            await ShowStockAdjustmentDialogAsync(product, true);
        }
    }

    private async void RemoveStockButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Product product)
        {
            await ShowStockAdjustmentDialogAsync(product, false);
        }
    }

    private async Task ShowProductDialogAsync(Product? existingProduct)
    {
        var isEdit = existingProduct != null;

        var nameBox = new TextBox { Header = "Nombre / Name", Text = existingProduct?.Name ?? "" };
        var descriptionBox = new TextBox { Header = "Descripción / Description", Text = existingProduct?.Description ?? "" };
        var categoryBox = new TextBox { Header = "Categoría / Category", Text = existingProduct?.Category ?? "" };
        var priceBox = new NumberBox { Header = "Precio / Price", Value = (double)(existingProduct?.Price ?? 0) };
        var barcodeBox = new TextBox { Header = "Código de Barras / Barcode", Text = existingProduct?.Barcode ?? "" };
        var stockBox = new NumberBox { Header = "Stock Inicial / Initial Stock", Value = existingProduct?.StockQuantity ?? 0, IsEnabled = !isEdit };
        var minStockBox = new NumberBox { Header = "Stock Mínimo / Min Stock Level", Value = existingProduct?.MinStockLevel ?? 5 };

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(nameBox);
        panel.Children.Add(descriptionBox);
        panel.Children.Add(categoryBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(barcodeBox);
        if (!isEdit) panel.Children.Add(stockBox);
        panel.Children.Add(minStockBox);

        var dialog = new ContentDialog
        {
            Title = isEdit ? "Editar Producto / Edit Product" : "Nuevo Producto / New Product",
            Content = panel,
            PrimaryButtonText = "Guardar / Save",
            CloseButtonText = "Cancelar / Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var product = existingProduct ?? new Product();
            product.Name = nameBox.Text;
            product.Description = descriptionBox.Text;
            product.Category = categoryBox.Text;
            product.Price = (decimal)priceBox.Value;
            product.Barcode = barcodeBox.Text;
            product.MinStockLevel = (int)minStockBox.Value;
            
            if (!isEdit)
            {
                product.StockQuantity = (int)stockBox.Value;
            }

            bool success;
            if (isEdit)
            {
                success = await _inventoryService.UpdateProductAsync(product);
            }
            else
            {
                success = await _inventoryService.CreateProductAsync(product);
            }

            if (success)
            {
                await LoadDataAsync();
            }
        }
    }

    private async Task ShowStockAdjustmentDialogAsync(Product product, bool isAddition)
    {
        var quantityBox = new NumberBox 
        { 
            Header = "Cantidad / Quantity", 
            Value = 1,
            Minimum = 1
        };
        var notesBox = new TextBox { Header = "Notas / Notes", PlaceholderText = "Razón del ajuste / Reason for adjustment" };

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = $"Producto: {product.Name}\nStock Actual: {product.StockQuantity}" });
        panel.Children.Add(quantityBox);
        panel.Children.Add(notesBox);

        var dialog = new ContentDialog
        {
            Title = isAddition ? "Agregar Stock / Add Stock" : "Remover Stock / Remove Stock",
            Content = panel,
            PrimaryButtonText = "Confirmar / Confirm",
            CloseButtonText = "Cancelar / Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var quantity = (int)quantityBox.Value;
            if (!isAddition) quantity = -quantity;

            var transactionType = isAddition ? "IN" : "OUT";
            var success = await _inventoryService.AdjustStockAsync(product.Id, quantity, transactionType, notesBox.Text);

            if (success)
            {
                await LoadDataAsync();
            }
        }
    }
}
