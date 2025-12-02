# Flavor-Master-SYS

A Point of Sale (POS) system for restaurants built with .NET 10 and WinUI 3.

## Overview

Flavor Master is a modern Windows desktop application designed for restaurant point-of-sale operations. It features a clean, intuitive interface for managing orders, menu items, inventory, users, and transactions.

## Features

### 🔐 Multi-User System with Roles
- **Master User**: Full system access, can manage all users and settings
- **Manager**: Limited administrative access
- **Cashier**: POS access for order processing
- **Kitchen**: View orders for preparation
- **Waiter**: Order taking capabilities

### 🎫 PDF Ticket System
- Automatic ticket/receipt generation for completed orders
- PDF format for easy printing and archiving
- Unique ticket numbers for tracking

### 📦 Inventory Management
- Track product stock levels
- Low stock alerts
- Barcode support for quick product lookup
- Stock adjustment with transaction logging

### 📊 Dashboard (Role-based visibility)
- Real-time sales statistics
- Today's orders and revenue
- Active orders monitoring
- Low stock alerts
- Top selling products
- Master user controls dashboard visibility for other roles

### 📷 Barcode Scanner Support
- USB barcode scanner compatible
- Quick product lookup by scanning
- Automatic product addition to orders

### 💾 Local SQLite Database
- All data stored locally
- No internet connection required
- Fast and reliable

## Requirements

- Windows 10 version 1809 (build 17763) or higher
- Windows 11 (recommended)
- Visual Studio 2022 version 17.0 or later with:
  - .NET Desktop Development workload
  - Windows App SDK C# Templates

## Technology Stack

- **.NET 10** - Latest .NET framework
- **WinUI 3** - Modern native UI framework for Windows
- **Windows App SDK 1.6** - Windows application development platform
- **SQLite** - Local database storage (Microsoft.Data.Sqlite)

## Getting Started

### Building the Project

1. Open `FlavorMasterSYS.sln` in Visual Studio 2022
2. Select the desired build configuration (Debug/Release) and platform (x64/x86/ARM64)
3. Build the solution (Ctrl+Shift+B)

### Running the Application

1. Set `FlavorMasterSYS` as the startup project
2. Press F5 to run with debugging, or Ctrl+F5 to run without debugging

### Default Login

- **Username**: `master`
- **Password**: `master123`

> ⚠️ Change the default password after first login!

## Project Structure

```
FlavorMasterSYS.sln
└── src/
    └── FlavorMasterSYS/
        ├── App.xaml/cs              # Application entry point
        ├── MainWindow.xaml/cs       # Main window with navigation
        ├── Models/                   # Data models
        │   ├── User.cs              # User and roles
        │   ├── Product.cs           # Products/menu items
        │   ├── Order.cs             # Orders and order items
        │   ├── Ticket.cs            # PDF tickets
        │   ├── InventoryTransaction.cs
        │   └── DashboardData.cs
        ├── Services/                 # Business logic services
        │   ├── DatabaseService.cs   # SQLite database management
        │   ├── AuthService.cs       # Authentication & user management
        │   ├── InventoryService.cs  # Product & inventory management
        │   ├── OrderService.cs      # Order processing
        │   ├── TicketService.cs     # PDF ticket generation
        │   └── BarcodeService.cs    # Barcode scanner input handling
        ├── Views/                    # UI pages
        │   ├── LoginPage.xaml/cs    # User login
        │   ├── POSPage.xaml/cs      # Point of Sale interface
        │   ├── DashboardPage.xaml/cs
        │   ├── InventoryPage.xaml/cs
        │   └── UserManagementPage.xaml/cs
        ├── Assets/                   # Application icons
        └── app.manifest             # Application manifest
```

## User Roles & Permissions

| Feature | Master | Manager | Cashier | Kitchen | Waiter |
|---------|--------|---------|---------|---------|--------|
| POS | ✅ | ✅ | ✅ | ❌ | ✅ |
| Dashboard | ✅ | ⚙️ | ⚙️ | ⚙️ | ⚙️ |
| Inventory | ✅ | ✅ | ❌ | ❌ | ❌ |
| User Management | ✅ | ❌ | ❌ | ❌ | ❌ |
| View Orders | ✅ | ✅ | ✅ | ✅ | ✅ |

⚙️ = Configurable by Master user

## License

This project is proprietary software.
