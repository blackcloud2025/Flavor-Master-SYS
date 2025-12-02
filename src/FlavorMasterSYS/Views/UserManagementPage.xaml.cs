using FlavorMasterSYS.Models;
using FlavorMasterSYS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FlavorMasterSYS.Views;

/// <summary>
/// User management page - only accessible by Master user.
/// </summary>
public sealed partial class UserManagementPage : Page
{
    private readonly AuthService _authService;

    public UserManagementPage(AuthService authService)
    {
        this.InitializeComponent();
        _authService = authService;
    }

    public async Task LoadUsersAsync()
    {
        var users = await _authService.GetAllUsersAsync();
        UsersListView.ItemsSource = users;
    }

    private async void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowUserDialogAsync(null);
    }

    private async void EditUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is User user)
        {
            await ShowUserDialogAsync(user);
        }
    }

    private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is User user)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmar Eliminación / Confirm Delete",
                Content = $"¿Está seguro de eliminar a {user.FullName}?\nAre you sure you want to delete {user.FullName}?",
                PrimaryButtonText = "Eliminar / Delete",
                CloseButtonText = "Cancelar / Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (await _authService.DeleteUserAsync(user.Id))
                {
                    await LoadUsersAsync();
                }
            }
        }
    }

    private async Task ShowUserDialogAsync(User? existingUser)
    {
        var isEdit = existingUser != null;
        
        var fullNameBox = new TextBox { Header = "Nombre Completo / Full Name", Text = existingUser?.FullName ?? "" };
        var usernameBox = new TextBox { Header = "Usuario / Username", Text = existingUser?.Username ?? "", IsEnabled = !isEdit };
        var passwordBox = new PasswordBox { Header = isEdit ? "Nueva Contraseña (opcional) / New Password (optional)" : "Contraseña / Password" };
        
        var roleCombo = new ComboBox 
        { 
            Header = "Rol / Role",
            ItemsSource = new[] { "Manager", "Cashier", "Kitchen", "Waiter" },
            SelectedIndex = existingUser != null ? (int)existingUser.Role - 1 : 0
        };

        var activeCheck = new CheckBox { Content = "Activo / Active", IsChecked = existingUser?.IsActive ?? true };
        var dashboardCheck = new CheckBox { Content = "Puede Ver Dashboard / Can View Dashboard", IsChecked = existingUser?.CanViewDashboard ?? false };

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(fullNameBox);
        panel.Children.Add(usernameBox);
        panel.Children.Add(passwordBox);
        panel.Children.Add(roleCombo);
        panel.Children.Add(activeCheck);
        panel.Children.Add(dashboardCheck);

        var dialog = new ContentDialog
        {
            Title = isEdit ? "Editar Usuario / Edit User" : "Nuevo Usuario / New User",
            Content = panel,
            PrimaryButtonText = "Guardar / Save",
            CloseButtonText = "Cancelar / Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var user = existingUser ?? new User();
            user.FullName = fullNameBox.Text;
            user.Username = usernameBox.Text;
            user.Role = (UserRole)(roleCombo.SelectedIndex + 1);
            user.IsActive = activeCheck.IsChecked ?? true;
            user.CanViewDashboard = dashboardCheck.IsChecked ?? false;

            bool success;
            if (isEdit)
            {
                var newPassword = string.IsNullOrEmpty(passwordBox.Password) ? null : passwordBox.Password;
                success = await _authService.UpdateUserAsync(user, newPassword);
            }
            else
            {
                success = await _authService.CreateUserAsync(user, passwordBox.Password);
            }

            if (success)
            {
                await LoadUsersAsync();
            }
        }
    }
}
