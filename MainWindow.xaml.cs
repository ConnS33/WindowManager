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
        private const uint VK_LEFT = 0x25;
        private const uint VK_RIGHT = 0x27;
        private const uint VK_UP = 0x26;
        private const uint VK_DOWN = 0x28;

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

        private void RegisterHotKeys()
        {
            var hWnd = host.Handle;
            var source = HwndSource.FromHwnd(hWnd);
            source.AddHook(HwndHook);

            // Register hotkeys (Ctrl+Alt+Arrow keys)
            RegisterHotKey(hWnd, 1, MOD_CONTROL | 0x0001, VK_LEFT);  // Ctrl+Alt+Left
            RegisterHotKey(hWnd, 2, MOD_CONTROL | 0x0001, VK_RIGHT); // Ctrl+Alt+Right
            RegisterHotKey(hWnd, 3, MOD_CONTROL | 0x0001, VK_UP);    // Ctrl+Alt+Up
            RegisterHotKey(hWnd, 4, MOD_CONTROL | 0x0001, VK_DOWN);  // Ctrl+Alt+Down
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                var activeWindow = GetForegroundWindow();
                if (activeWindow != IntPtr.Zero && activeWindow != host.Handle)
                {
                    switch (wParam.ToInt32())
                    {
                        case 1: SnapWindow(activeWindow, 0, 0, 0.5, 1); break;    // Left half
                        case 2: SnapWindow(activeWindow, 0.5, 0, 0.5, 1); break; // Right half
                        case 3: SnapWindow(activeWindow, 0, 0, 1, 0.5); break;   // Top half
                        case 4: SnapWindow(activeWindow, 0, 0.5, 1, 0.5); break; // Bottom half
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

            // Left half
            AddZoneOverlay(0, 0, width / 2, height, "Left Half");
            // Right half
            AddZoneOverlay(width / 2, 0, width / 2, height, "Right Half");
            // Top half
            AddZoneOverlay(0, 0, width, height / 2, "Top Half");
            // Bottom half
            AddZoneOverlay(0, height / 2, width, height / 2, "Bottom Half");
        }

        private void AddZoneOverlay(double x, double y, double width, double height, string label)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(0x40, 0x00, 0x7A, 0xCC)),
                Opacity = 0
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            OverlayCanvas.Children.Add(rect);

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Opacity = 0
            };

            Canvas.SetLeft(textBlock, x + 10);
            Canvas.SetTop(textBlock, y + 10);
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
