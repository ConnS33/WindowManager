using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowManager.Models;
using WindowManager.Services;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using WpfMessageBox = System.Windows.MessageBox;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace WindowManager
{
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_LEFT = 0x25;
        private const uint VK_RIGHT = 0x27;
        private const uint VK_UP = 0x26;
        private const uint VK_DOWN = 0x28;

        private bool isOverlayVisible = false;
        private WindowInteropHelper host;
        private readonly LayoutManager _layoutManager = new();
        private readonly WindowManagerService _windowManager = new();
        private readonly DispatcherTimer _refreshTimer = new();
        private ObservableCollection<WindowInfo> _openWindows = new();
        private ObservableCollection<SavedLayout> _savedLayouts = new();
        private bool _disposedValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.Property.Name));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Tick -= RefreshTimer_Tick;
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ObservableCollection<WindowInfo> OpenWindows
        {
            get => _openWindows;
            set
            {
                _openWindows = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Set up data binding
            windowsList.ItemsSource = _openWindows;
            layoutsList.ItemsSource = _savedLayouts;

            // Initial load of windows and layouts
            RefreshOpenWindows();
            LoadLayouts();

            // Handle window closing
            this.Closed += (s, e) => 
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= RefreshTimer_Tick;
            };

            host = new WindowInteropHelper(this);
            host.EnsureHandle();
            RegisterHotKeys();
            CreateZoneOverlays();
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RefreshOpenWindows);
            }
            else
            {
                RefreshOpenWindows();
            }
        }

        private void InitializeWindowRefreshTimer()
        {
            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        private void RefreshOpenWindows()
        {
            try
            {
                var currentWindows = _windowManager.GetOpenWindows();
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Clear and repopulate the collection to update the UI
                        _openWindows.Clear();
                        foreach (var window in currentWindows)
                        {
                            _openWindows.Add(window);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing windows: {ex.Message}");
            }
        }

        private void BtnRefreshWindows_Click(object sender, RoutedEventArgs e)
        {
            RefreshOpenWindows();
        }

        public void LoadLayouts()
        {
            try
            {
                Debug.WriteLine("Loading layouts...");
                var layouts = _layoutManager.LoadAllLayouts();
                Debug.WriteLine($"Found {layouts.Count} layouts");
                
                // Clear existing layouts
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _savedLayouts.Clear();
                    
                    // Add each layout to the observable collection
                    foreach (var layout in layouts)
                    {
                        if (layout != null && !string.IsNullOrEmpty(layout.Name))
                        {
                            Debug.WriteLine($"Adding layout: {layout.Name} with {layout.Zones?.Count ?? 0} zones");
                            _savedLayouts.Add(layout);
                        }
                    }
                    
                    // Update the UI
                    if (layoutsList != null)
                    {
                        layoutsList.ItemsSource = null;
                        layoutsList.ItemsSource = _savedLayouts;
                        Debug.WriteLine($"List updated with {_savedLayouts.Count} layouts");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadLayouts: {ex}");
                WpfMessageBox.Show($"Error loading layouts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNewLayout_Click(object sender, RoutedEventArgs e)
        {
            var editor = new LayoutEditorWindow();
            if (editor.ShowDialog() == true)
            {
                LoadLayouts();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLayouts();
        }

        public void ApplyLayout(SavedLayout layout, bool applyToAllWindows = false)
        {
            if (layout == null || layout.Zones.Count == 0) 
            {
                WpfMessageBox.Show("The selected layout has no zones defined.", "Invalid Layout", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Get the primary screen dimensions
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                var screenBounds = new Rect(0, 0, screenWidth, screenHeight);

                if (applyToAllWindows)
                {
                    // Apply layout to all open windows
                    var windows = _windowManager.GetOpenWindows()
                        .Where(w => w.Handle != IntPtr.Zero)
                        .ToList();

                    if (windows.Count == 0)
                    {
                        WpfMessageBox.Show("No open windows found to arrange.", "No Windows", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }


                    // For each zone in the layout, move a window to it
                    for (int i = 0; i < Math.Min(windows.Count, layout.Zones.Count); i++)
                    {
                        var window = windows[i];
                        var zone = layout.Zones[i];
                        
                        // Convert normalized zone coordinates to screen coordinates
                        var zoneRect = new Rect(
                            zone.Bounds.Left,
                            zone.Bounds.Top,
                            zone.Bounds.Width,
                            zone.Bounds.Height);

                        // Move the window to the zone
                        _windowManager.MoveWindowToZone(window.Handle, zoneRect, screenBounds);
                    }
                }
                else
                {
                    // Apply layout to the currently selected window
                    var selectedWindow = windowsList.SelectedItem as WindowInfo;
                    if (selectedWindow == null)
                    {
                        WpfMessageBox.Show("Please select a window to apply the layout to.", "No Window Selected", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // Get the first zone in the layout
                    var zone = layout.Zones[0];
                    var zoneRect = new Rect(
                        zone.Bounds.Left,
                        zone.Bounds.Top,
                        zone.Bounds.Width,
                        zone.Bounds.Height);

                    _windowManager.MoveWindowToZone(selectedWindow.Handle, zoneRect, screenBounds);
                }

                // Show a brief notification instead of a message box
                var notification = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = new SolidColorBrush(System.Windows.Media.Colors.Black) { Opacity = 0.7 },
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Topmost = true,
                    ShowInTaskbar = false,
                    Left = SystemParameters.PrimaryScreenWidth - 200,
                    Top = 50
                };

                var textBlock = new TextBlock
                {
                    Text = $"Applied: {layout.Name}",
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(20, 10, 20, 10),
                    FontSize = 14
                };

                notification.Content = textBlock;
                notification.Show();

                // Auto-close the notification after 2 seconds
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, e) => {
                    timer.Stop();
                    notification.Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error applying layout: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LayoutsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is SavedLayout selectedLayout)
                {
                    // Show a preview of the selected layout
                    ShowLayoutPreview(selectedLayout);
                }
                else
                {
                    // If no item is selected, hide the overlay
                    HideOverlay();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LayoutsList_SelectionChanged: {ex}");
                HideOverlay();
            }
        }

        private void LayoutsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (layoutsList.SelectedItem is SavedLayout selectedLayout)
            {
                // If a window is selected, apply to that window, otherwise apply to all
                ApplyLayout(selectedLayout, windowsList.SelectedItem == null);
            }
        }

        private void ApplyLayout_Click(object sender, RoutedEventArgs e)
        {
            if (layoutsList.SelectedItem is SavedLayout selectedLayout)
            {
                // Apply to selected window if one is selected, otherwise apply to all
                ApplyLayout(selectedLayout, windowsList.SelectedItem == null);
            }
            else
            {
                WpfMessageBox.Show("Please select a layout to apply.", "No Layout Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyToAllWindows_Click(object sender, RoutedEventArgs e)
        {
            if (layoutsList.SelectedItem is SavedLayout selectedLayout)
            {
                // Apply to all open windows
                ApplyLayout(selectedLayout, true);
            }
            else
            {
                WpfMessageBox.Show("Please select a layout to apply.", "No Layout Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteLayout_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = layoutsList.SelectedItem as SavedLayout;
            if (selectedItem != null)
            {
                var result = WpfMessageBox.Show($"Are you sure you want to delete the layout '{selectedItem.Name}'?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _layoutManager.DeleteLayout(selectedItem.Name);
                        LoadLayouts();
                    }
                    catch (Exception ex)
                    {
                        WpfMessageBox.Show($"Error deleting layout: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        
        private void RegisterHotKeys()
        {
            var hWnd = host.Handle;
            var source = HwndSource.FromHwnd(hWnd);
            source.AddHook(HwndHook);

            // Register hotkeys for half-screen with Ctrl+Alt+Arrow
            RegisterHotKey(hWnd, 1, MOD_CONTROL | MOD_ALT, VK_LEFT);   // Left half
            RegisterHotKey(hWnd, 2, MOD_CONTROL | MOD_ALT, VK_RIGHT);  // Right half
            RegisterHotKey(hWnd, 3, MOD_CONTROL | MOD_ALT, VK_UP);     // Top half
            RegisterHotKey(hWnd, 4, MOD_CONTROL | MOD_ALT, VK_DOWN);   // Bottom half

            // Register hotkeys for quarter-screen with Shift+Ctrl+Alt+Arrow
            RegisterHotKey(hWnd, 5, MOD_SHIFT | MOD_CONTROL | MOD_ALT, VK_LEFT);   // Top Left
            RegisterHotKey(hWnd, 6, MOD_SHIFT | MOD_CONTROL | MOD_ALT, VK_RIGHT);  // Top Right
            RegisterHotKey(hWnd, 7, MOD_SHIFT | MOD_CONTROL | MOD_ALT, VK_UP);     // Bottom Left
            RegisterHotKey(hWnd, 8, MOD_SHIFT | MOD_CONTROL | MOD_ALT, VK_DOWN);   // Bottom Right
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                var activeWindow = GetForegroundWindow();
                if (activeWindow != IntPtr.Zero && activeWindow != host.Handle)
                {
                    // Skip if the active window is the desktop or a file explorer window
                    var className = new System.Text.StringBuilder(256);
                    GetClassName(activeWindow, className, className.Capacity);
                    string classNameStr = className.ToString();
                    
                    if (classNameStr == "Progman" || classNameStr == "WorkerW" || classNameStr == "Shell_TrayWnd" || 
                        classNameStr == "Shell_SecondaryTrayWnd" || classNameStr.StartsWith("CabinetWClass") || 
                        classNameStr.StartsWith("ExploreWClass"))
                    {
                        // Don't process hotkeys for desktop or file explorer windows
                        return IntPtr.Zero;
                    }

                    // Handle window snapping hotkeys
                    switch (wParam.ToInt32())
                    {
                        // Half-screen
                        case 1: // Ctrl+Shift+Alt+Left - Left Half
                            SnapWindow(activeWindow, 0, 0, 0.5, 1);
                            break;
                        case 2: // Ctrl+Shift+Alt+Right - Right Half
                            SnapWindow(activeWindow, 0.5, 0, 0.5, 1);
                            break;
                        case 3: // Ctrl+Shift+Alt+Up - Top Half
                            SnapWindow(activeWindow, 0, 0, 1, 0.5);
                            break;
                        case 4: // Ctrl+Shift+Alt+Down - Bottom Half
                            SnapWindow(activeWindow, 0, 0.5, 1, 0.5);
                            break;
                        
                        // Quarter-screen (Ctrl+Shift+Alt+Number)
                        case 5: // Ctrl+Shift+Alt+1 - Top Left
                            SnapWindow(activeWindow, 0, 0, 0.5, 0.5);
                            break;
                        case 6: // Ctrl+Shift+Alt+2 - Top Right
                            SnapWindow(activeWindow, 0.5, 0, 0.5, 0.5);
                            break;
                        case 7: // Ctrl+Shift+Alt+3 - Bottom Left
                            SnapWindow(activeWindow, 0, 0.5, 0.5, 0.5);
                            break;
                        case 8: // Ctrl+Shift+Alt+4 - Bottom Right
                            SnapWindow(activeWindow, 0.5, 0.5, 0.5, 0.5);
                            break;
                    }
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void SnapWindow(IntPtr hWnd, double x, double y, double width, double height)
        {
            var screen = SystemParameters.WorkArea;
            int newX = (int)(screen.Width * x);
            int newY = (int)(screen.Height * y);
            int newWidth = (int)(screen.Width * width);
            int newHeight = (int)(screen.Height * height);

            SetWindowPos(hWnd, IntPtr.Zero, newX, newY, newWidth, newHeight, SWP_SHOWWINDOW);
        }

        private void CreateZoneOverlays()
        {
            var screen = SystemParameters.WorkArea;
            double width = screen.Width;
            double height = screen.Height;
            double halfWidth = width / 2;
            double halfHeight = height / 2;

            // Clear any existing overlays
            OverlayCanvas.Children.Clear();

            // Half-screen
            AddZoneOverlay(0, 0, halfWidth, height, "Left Half (Ctrl+Shift+Alt+←)");
            AddZoneOverlay(halfWidth, 0, halfWidth, height, "Right Half (Ctrl+Shift+Alt+→)");
            AddZoneOverlay(0, 0, width, halfHeight, "Top Half (Ctrl+Shift+Alt+↑)");
            AddZoneOverlay(0, halfHeight, width, halfHeight, "Bottom Half (Ctrl+Shift+Alt+↓)");
            
            // Quarter-screen
            AddZoneOverlay(0, 0, halfWidth, halfHeight, "Top Left (Ctrl+Shift+Alt+1)");
            AddZoneOverlay(halfWidth, 0, halfWidth, halfHeight, "Top Right (Ctrl+Shift+Alt+2)");
            AddZoneOverlay(0, halfHeight, halfWidth, halfHeight, "Bottom Left (Ctrl+Shift+Alt+3)");
            AddZoneOverlay(halfWidth, halfHeight, halfWidth, halfHeight, "Bottom Right (Ctrl+Shift+Alt+4)");
        }

        private void AddZoneOverlay(double x, double y, double width, double height, string label)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 4,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x60, 0x00, 0x7A, 0xCC)) { Opacity = 0.5 },
                Opacity = 0,
                RadiusX = 8,
                RadiusY = 8
            };

            // Add a border effect
            var border = new System.Windows.Shapes.Rectangle
            {
                Width = width - 4,
                Height = height - 4,
                Stroke = WpfBrushes.White,
                StrokeThickness = 2,
                Fill = WpfBrushes.Transparent,
                Opacity = 0.8,
                RadiusX = 6,
                RadiusY = 6
            };

            Canvas.SetLeft(rect, x + 2);
            Canvas.SetTop(rect, y + 2);
            OverlayCanvas.Children.Add(rect);
            
            Canvas.SetLeft(border, x + 4);
            Canvas.SetTop(border, y + 4);
            OverlayCanvas.Children.Add(border);

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = label,
                Foreground = WpfBrushes.White,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Opacity = 0,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 2,
                    Opacity = 0.8
                }
            };

            // Center the text in the zone
            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textBlock, x + (width - textBlock.DesiredSize.Width) / 2);
            Canvas.SetTop(textBlock, y + (height - textBlock.DesiredSize.Height) / 2);
            OverlayCanvas.Children.Add(textBlock);
        }

        private void ShowLayoutPreview(SavedLayout layout)
        {
            try
            {
                if (OverlayCanvas == null) return;
                
                // Clear existing preview
                OverlayCanvas.Children.Clear();

                if (layout == null || layout.Zones.Count == 0)
                {
                    OverlayCanvas.Visibility = Visibility.Collapsed;
                    return;
                }

                // Show each zone in the layout
                foreach (var zone in layout.Zones)
                {
                    var border = new Border
                    {
                        BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.DodgerBlue),
                        BorderThickness = new Thickness(2),
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 30, 144, 255)),
                        Width = zone.Bounds.Width,
                        Height = zone.Bounds.Height,
                        CornerRadius = new CornerRadius(4)
                    };

                    // Convert screen coordinates to canvas coordinates
                    var screenPoint = new Point(zone.Bounds.Left, zone.Bounds.Top);
                    var canvasPoint = this.PointFromScreen(screenPoint);

                    Canvas.SetLeft(border, canvasPoint.X);
                    Canvas.SetTop(border, canvasPoint.Y);

                    // Add zone label
                    var label = new TextBlock
                    {
                        Text = $"Zone {layout.Zones.IndexOf(zone) + 1}",
                        Foreground = System.Windows.Media.Brushes.White,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Margin = new Thickness(5),
                        TextAlignment = TextAlignment.Center
                    };

                    border.Child = label;
                    OverlayCanvas.Children.Add(border);
                }


                // Show the overlay
                OverlayCanvas.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing layout preview: {ex}");
                if (OverlayCanvas != null)
                    OverlayCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowOverlay()
        {
            if (!isOverlayVisible)
            {
                isOverlayVisible = true;
                Visibility = Visibility.Visible;
                Activate();
                foreach (var child in OverlayCanvas.Children)
                {
                    if (child is UIElement element)
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
                        element.BeginAnimation(UIElement.OpacityProperty, animation);
                    }
                }
            }
        }

        private void HideOverlay()
        {
            if (isOverlayVisible)
            {
                isOverlayVisible = false;
                var animation = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
                animation.Completed += (s, e) => { if (!isOverlayVisible) Visibility = Visibility.Collapsed; };
                
                foreach (var child in OverlayCanvas.Children)
                {
                    if (child is UIElement element)
                    {
                        element.BeginAnimation(UIElement.OpacityProperty, animation);
                    }
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            HideOverlay();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up hotkeys
            for (int i = 1; i <= 4; i++)
            {
                UnregisterHotKey(host.Handle, i);
            }
            base.OnClosed(e);
        }
    }
}
