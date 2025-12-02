# Flavor-Master-SYS

A Point of Sale (POS) system for restaurants built with .NET 10 and WinUI 3.

## Overview

Flavor Master is a modern Windows desktop application designed for restaurant point-of-sale operations. It features a clean, intuitive interface for managing orders, menu items, and transactions.

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

## Getting Started

### Building the Project

1. Open `FlavorMasterSYS.sln` in Visual Studio 2022
2. Select the desired build configuration (Debug/Release) and platform (x64/x86/ARM64)
3. Build the solution (Ctrl+Shift+B)

### Running the Application

1. Set `FlavorMasterSYS` as the startup project
2. Press F5 to run with debugging, or Ctrl+F5 to run without debugging

## Project Structure

```
FlavorMasterSYS.sln
└── src/
    └── FlavorMasterSYS/
        ├── App.xaml              # Application entry point
        ├── App.xaml.cs           # Application logic
        ├── MainWindow.xaml       # Main window UI
        ├── MainWindow.xaml.cs    # Main window logic
        ├── Assets/               # Application assets (icons, logos)
        └── app.manifest          # Application manifest
```

## Features

- Menu category navigation
- Order management
- Real-time order total calculation
- Modern Windows 11 design language

## License

This project is proprietary software.
