using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using WindowManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowManager.Services
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string? Title { get; set; }
        public Rect Bounds { get; set; }
        public bool IsVisible { get; set; }
    }

    public class WindowManagerService
    {
        // Import necessary Windows API functions
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public List<WindowInfo> GetOpenWindows()
        {
            var windows = new List<WindowInfo>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var sb = new StringBuilder(length + 1);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();

                        // Skip windows with empty titles or specific system windows
                        if (!string.IsNullOrEmpty(title) && 
                            !title.StartsWith("Program Manager") && 
                            !title.StartsWith("Microsoft Text Input Application") &&
                            !title.Contains("Window Manager"))
                        {
                            if (GetWindowRect(hWnd, out RECT rect))
                            {
                                windows.Add(new WindowInfo
                                {
                                    Handle = hWnd,
                                    Title = title,
                                    Bounds = new Rect(
                                        rect.Left,
                                        rect.Top,
                                        rect.Right - rect.Left,
                                        rect.Bottom - rect.Top),
                                    IsVisible = true
                                });
                            }
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public void MoveWindowToZone(IntPtr hWnd, Rect zoneRect, Rect screenBounds)
        {
            try
            {
                // Convert relative zone coordinates to screen coordinates
                int x = (int)(screenBounds.Left + zoneRect.Left);
                int y = (int)(screenBounds.Top + zoneRect.Top);
                int width = (int)zoneRect.Width;
                int height = (int)zoneRect.Height;

                // Ensure minimum size
                width = Math.Max(100, width);
                height = Math.Max(100, height);

                // Get current window placement to preserve window state
                WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                placement.length = System.Runtime.InteropServices.Marshal.SizeOf(placement);
                
                if (GetWindowPlacement(hWnd, ref placement))
                {
                    // If window is maximized or minimized, restore it first
                    if (placement.showCmd == SW_SHOWMAXIMIZED || placement.showCmd == SW_SHOWMINIMIZED)
                    {
                        ShowWindow(hWnd, SW_SHOWNORMAL);
                    }
                }

                // Move and resize the window
                MoveWindow(hWnd, x, y, width, height, true);

                // Bring window to foreground
                SetForegroundWindow(hWnd);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error moving window: {ex.Message}");
                throw;
            }
        }

        public void ApplyLayoutToWindows(SavedLayout layout, IntPtr[] windowHandles)
        {
            if (layout == null || layout.Zones.Count == 0 || windowHandles == null || windowHandles.Length == 0)
                return;

            // Get the primary screen dimensions
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenBounds = new Rect(0, 0, screenWidth, screenHeight);

            // For now, just distribute windows across zones in order
            // You might want to implement more sophisticated window-zone matching logic
            for (int i = 0; i < Math.Min(windowHandles.Length, layout.Zones.Count); i++)
            {
                var zone = layout.Zones[i];
                var zoneRect = new Rect(zone.Bounds.Left * screenWidth, 
                                       zone.Bounds.Top * screenHeight,
                                       zone.Bounds.Width * screenWidth,
                                       zone.Bounds.Height * screenHeight);
                
                MoveWindowToZone(windowHandles[i], zoneRect, screenBounds);
            }
        }

        public IntPtr GetActiveWindow()
        {
            return GetForegroundWindow();
        }
    }
}
