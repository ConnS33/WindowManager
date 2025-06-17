# Window Manager

A lightweight window management utility for Windows that allows you to quickly snap windows to different screen zones using keyboard shortcuts.

## Features

- Snap windows to 4 screen quadrants using intuitive keyboard shortcuts
- Visual feedback with elegant zone highlighting
- Runs in the system tray
- Easily enable/disable the window snapping functionality
- Support for both 4-zone and half-screen snapping

## How to Use

### Window Snapping

#### Half-Screen Snapping:
- `Ctrl + Alt + ←` - Left Half
- `Ctrl + Alt + →` - Right Half
- `Ctrl + Alt + ↑` - Top Half
- `Ctrl + Alt + ↓` - Bottom Half

#### Quarter-Screen Snapping:
- `Shift + Ctrl + Alt + ←` - Top Left Quadrant
- `Shift + Ctrl + Alt + →` - Top Right Quadrant
- `Shift + Ctrl + Alt + ↑` - Bottom Left Quadrant
- `Shift + Ctrl + Alt + ↓` - Bottom Right Quadrant

Visual overlays will appear when you press any of these key combinations to show the target zone.

## System Tray Controls

- **Enable/Disable**: Toggle the window snapping functionality
- **Exit**: Close the application

## Requirements

- Windows 10 or later
- .NET 6.0 or later

## Getting Started

### Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/WindowManager.git
   cd WindowManager
   ```

2. Build the application:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

### Building for Release

To create a self-contained executable:

```bash
dotnet publish -c Release -r win10-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output will be in `bin/Release/net6.0-windows/win10-x64/publish/`

## Development

### Project Structure

- `App.xaml` - Application entry point and resources
- `MainWindow.xaml` - Main window with overlay UI
- `App.xaml.cs` - Application logic and system tray integration
- `MainWindow.xaml.cs` - Window management and hotkey handling

### Dependencies

- .NET 6.0
- Windows Forms (for system tray)
- WPF (for UI)

## License

MIT License

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
