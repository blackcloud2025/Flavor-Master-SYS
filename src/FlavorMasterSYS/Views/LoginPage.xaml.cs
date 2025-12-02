using FlavorMasterSYS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace FlavorMasterSYS.Views;

/// <summary>
/// Login page for user authentication.
/// </summary>
public sealed partial class LoginPage : Page
{
    private readonly AuthService _authService;

    public event EventHandler? LoginSuccessful;

    public LoginPage(AuthService authService)
    {
        this.InitializeComponent();
        _authService = authService;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;

        try
        {
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Por favor ingrese usuario y contraseña / Please enter username and password");
                return;
            }

            var user = await _authService.LoginAsync(username, password);

            if (user != null)
            {
                LoginSuccessful?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowError("Usuario o contraseña incorrectos / Invalid username or password");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}
