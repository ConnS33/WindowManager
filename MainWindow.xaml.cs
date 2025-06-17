using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;  // For Canvas
using Brushes = System.Windows.Media.Brushes;  // Resolve Brushes ambiguity
using Color = System.Windows.Media.Color;  // Resolve Color ambiguity

namespace WindowManager
{
    public partial class MainWindow : Window
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
        private const uint VK_LEFT = 0x25;
        private const uint VK_RIGHT = 0x27;
        private const uint VK_UP = 0x26;
        private const uint VK_DOWN = 0x28;
        private const uint VK_SPACE = 0x20;

        private bool isOverlayVisible = false;
        private WindowInteropHelper host;

        public MainWindow()
        {
            InitializeComponent();
            host = new WindowInteropHelper(this);
            host.EnsureHandle();
            RegisterHotKeys();
            CreateZoneOverlays();
        }

        private enum SnapZone 
        { 
            FullScreen, 
            LeftHalf, 
            RightHalf, 
            TopHalf, 
            BottomHalf, 
            TopLeft, 
            TopRight, 
            BottomLeft, 
            BottomRight 
        }
        
        private Dictionary<IntPtr, SnapZone> windowStates = new Dictionary<IntPtr, SnapZone>();

        private void RegisterHotKeys()
        {
            var hWnd = host.Handle;
            var source = HwndSource.FromHwnd(hWnd);
            source.AddHook(HwndHook);

            // Register single hotkey for cycling through zones
            RegisterHotKey(hWnd, 1, MOD_CONTROL | MOD_ALT, VK_SPACE);  // Ctrl+Alt+Space to cycle zones
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                var activeWindow = GetForegroundWindow();
                if (activeWindow != IntPtr.Zero && activeWindow != host.Handle)
                {
                    // Get current state or default to FullScreen
                    if (!windowStates.TryGetValue(activeWindow, out var currentState))
                    {
                        currentState = SnapZone.FullScreen;
                    }

                    // Cycle to next zone
                    var nextState = currentState switch
                    {
                        SnapZone.FullScreen => SnapZone.LeftHalf,
                        SnapZone.LeftHalf => SnapZone.RightHalf,
                        SnapZone.RightHalf => SnapZone.TopHalf,
                        SnapZone.TopHalf => SnapZone.BottomHalf,
                        SnapZone.BottomHalf => SnapZone.TopLeft,
                        SnapZone.TopLeft => SnapZone.TopRight,
                        SnapZone.TopRight => SnapZone.BottomLeft,
                        SnapZone.BottomLeft => SnapZone.BottomRight,
                        _ => SnapZone.FullScreen
                    };

                    // Snap window based on the next zone
                    switch (nextState)
                    {
                        case SnapZone.FullScreen:
                            var screen = SystemParameters.WorkArea;
                            SetWindowPos(activeWindow, IntPtr.Zero, 0, 0, (int)screen.Width, (int)screen.Height, SWP_SHOWWINDOW);
                            break;
                        case SnapZone.LeftHalf:
                            SnapWindow(activeWindow, 0, 0, 0.5, 1);
                            break;
                        case SnapZone.RightHalf:
                            SnapWindow(activeWindow, 0.5, 0, 0.5, 1);
                            break;
                        case SnapZone.TopHalf:
                            SnapWindow(activeWindow, 0, 0, 1, 0.5);
                            break;
                        case SnapZone.BottomHalf:
                            SnapWindow(activeWindow, 0, 0.5, 1, 0.5);
                            break;
                        case SnapZone.TopLeft:
                            SnapWindow(activeWindow, 0, 0, 0.5, 0.5);
                            break;
                        case SnapZone.TopRight:
                            SnapWindow(activeWindow, 0.5, 0, 0.5, 0.5);
                            break;
                        case SnapZone.BottomLeft:
                            SnapWindow(activeWindow, 0, 0.5, 0.5, 0.5);
                            break;
                        case SnapZone.BottomRight:
                            SnapWindow(activeWindow, 0.5, 0.5, 0.5, 0.5);
                            break;
                    }

                    // Update the window's state
                    windowStates[activeWindow] = nextState;
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

            // Top Left
            AddZoneOverlay(0, 0, halfWidth, halfHeight, "Top Left (Ctrl+Alt+Q)");
            // Top Right
            AddZoneOverlay(halfWidth, 0, halfWidth, halfHeight, "Top Right (Ctrl+Alt+W)");
            // Bottom Left
            AddZoneOverlay(0, halfHeight, halfWidth, halfHeight, "Bottom Left (Ctrl+Alt+A)");
            // Bottom Right
            AddZoneOverlay(halfWidth, halfHeight, halfWidth, halfHeight, "Bottom Right (Ctrl+Alt+S)");
        }

        private void AddZoneOverlay(double x, double y, double width, double height, string label)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brushes.White,
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0x7A, 0xCC)),
                Opacity = 0,
                RadiusX = 8,
                RadiusY = 8
            };

            // Add a border effect
            var border = new System.Windows.Shapes.Rectangle
            {
                Width = width - 4,
                Height = height - 4,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
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
                Foreground = Brushes.White,
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
