using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WindowManager.Models;
using WindowManager.Services;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMessageBox = System.Windows.MessageBox;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using System.Windows.Forms;

namespace WindowManager
{
    public partial class LayoutEditorWindow : Window, IDisposable
    {
        private bool isDragging = false;
        private Point dragStartPoint;
        private double initialSplitterPosition;
        private GridSplitter? activeSplitter;
        private List<Grid> zones = new();
        private int zoneCounter = 1;
        private readonly LayoutManager _layoutManager = new();
        private bool _isLoadingLayout = false;
        private bool _disposedValue;

        public LayoutEditorWindow()
        {
            try
            {
                Console.WriteLine("LayoutEditorWindow constructor started");
                
                // Initialize component first
                InitializeComponent();
                
                Console.WriteLine("Components initialized");
                
                // Initialize zones list
                zones = new List<Grid>();
                
                // Ensure LeftZone is added to zones
                if (LeftZone == null)
                {
                    Console.WriteLine("WARNING: LeftZone is null after InitializeComponent!");
                    // Try to find it again
                    LeftZone = FindName("LeftZone") as Grid;
                    Console.WriteLine($"LeftZone after FindName: {LeftZone != null}");
                }

                if (LeftZone != null)
                {
                    zones.Add(LeftZone);
                }

                // Set up event handlers
                this.Closed += (s, e) => Dispose();
                this.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) this.Close(); };
                
                // Initialize with a single zone
                zoneCounter = 1;
                
                Console.WriteLine("LayoutEditorWindow constructor completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LayoutEditorWindow constructor: {ex}");
                WpfMessageBox.Show($"Error initializing layout editor: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set window position and size
                var primaryScreen = System.Windows.SystemParameters.WorkArea;
                this.Left = primaryScreen.Left;
                this.Top = primaryScreen.Top;
                this.Width = primaryScreen.Width;
                this.Height = primaryScreen.Height;

                // Initialize collections and UI elements
                if (zones == null)
                    zones = new List<Grid>();

                if (LeftZone != null && !zones.Contains(LeftZone))
                    zones.Add(LeftZone);

                // Initialize right column and splitter
                if (RightColumn != null)
                    RightColumn.Width = new GridLength(0);
                if (VerticalSplitter != null)
                    VerticalSplitter.Visibility = Visibility.Collapsed;
                if (RightZone != null)
                    RightZone.Visibility = Visibility.Collapsed;

                // Update UI
                UpdateZoneLabels();
                Console.WriteLine("Window_Loaded completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Window_Loaded: {ex}");
                WpfMessageBox.Show("Failed to initialize LayoutEditorWindow. Please check the logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }


        private void BtnAddZone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("BtnAddZone_Click started");
                
                if (zones.Count >= 2)
                {
                    // For now, limit to 2 zones as in the screenshot
                    WpfMessageBox.Show("Maximum of 2 zones supported in this version.", "Zone Limit", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                Console.WriteLine("Adding second zone...");
                
                // Show the splitter and right column
                if (VerticalSplitter != null)
                {
                    VerticalSplitter.Visibility = Visibility.Visible;
                    Console.WriteLine("Splitter made visible");
                }
                
                if (RightColumn != null)
                {
                    RightColumn.Width = new GridLength(1, GridUnitType.Star);
                    Console.WriteLine("Right column width set to 1*");
                }
                
                if (RightZone != null)
                {
                    RightZone.Visibility = Visibility.Visible;
                    Console.WriteLine("Right zone made visible");
                }
                
                if (RightZoneText != null)
                {
                    RightZoneText.Visibility = Visibility.Visible;
                    Console.WriteLine("Right zone text made visible");
                }
                
                // Add the right zone to our zones list if not already present
                if (RightZone != null && !zones.Contains(RightZone))
                {
                    zones.Add(RightZone);
                    zoneCounter++;
                    Console.WriteLine($"Added RightZone to zones list. Total zones: {zones.Count}");
                    UpdateZoneLabels();
                }
                
                Console.WriteLine("BtnAddZone_Click completed");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error adding zone: {ex.Message}\n\n{ex.StackTrace}";
                Console.WriteLine(errorMsg);
                WpfMessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateZoneLabels()
        {
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] == LeftZone && LeftZoneText != null)
                    LeftZoneText.Text = $"Zone {i + 1} (Left)";
                else if (zones[i] == RightZone && RightZoneText != null)
                    RightZoneText.Text = $"Zone {i + 1} (Right)";
            }
        }

        public void LoadLayout(SavedLayout layout)
        {
            if (layout == null) return;
            
            try
            {
                _isLoadingLayout = true;
                
                // Reset to default state
                if (RightColumn != null)
                    RightColumn.Width = new GridLength(0);
                if (VerticalSplitter != null)
                    VerticalSplitter.Visibility = Visibility.Collapsed;
                if (RightZone != null)
                    RightZone.Visibility = Visibility.Collapsed;
                
                // Clear existing zones except the first one
                zones.Clear();
                if (LeftZone != null)
                    zones.Add(LeftZone);
                
                // Apply layout bounds
                if (layout.Bounds.Width > 0 && layout.Bounds.Height > 0)
                {
                    this.Left = layout.Bounds.Left;
                    this.Top = layout.Bounds.Top;
                    this.Width = layout.Bounds.Width;
                    this.Height = layout.Bounds.Height;
                }
                
                // Apply zones
                foreach (var zoneInfo in layout.Zones)
                {
                    if (zoneInfo.Column == 0)
                    {
                        // Left zone
                        if (LeftZone != null)
                        {
                            Grid.SetColumn(LeftZone, 0);
                            Grid.SetRow(LeftZone, 0);
                            Grid.SetColumnSpan(LeftZone, zoneInfo.ColumnSpan);
                            Grid.SetRowSpan(LeftZone, zoneInfo.RowSpan);
                        }
                    }
                    else if (zoneInfo.Column > 0)
                    {
                        // Right zone
                        if (RightZone != null && VerticalSplitter != null && RightColumn != null)
                        {
                            // Add the second zone
                            if (zones.Count < 2)
                            {
                                // Create a dummy sender object to avoid null reference
                                object sender = new object();
                                BtnAddZone_Click(sender, new RoutedEventArgs());
                            }
                            
                            if (RightZone != null)
                            {
                                Grid.SetColumn(RightZone, zoneInfo.Column);
                                Grid.SetRow(RightZone, zoneInfo.Row);
                                Grid.SetColumnSpan(RightZone, zoneInfo.ColumnSpan);
                                Grid.SetRowSpan(RightZone, zoneInfo.RowSpan);
                            }
                            
                            // Set splitter position
                            if (zoneInfo.Bounds.Width > 0 && this.ActualWidth > 0)
                            {
                                double ratio = zoneInfo.Bounds.Width / this.ActualWidth;
                                LeftColumn.Width = new GridLength(1 - ratio, GridUnitType.Star);
                                RightColumn.Width = new GridLength(ratio, GridUnitType.Star);
                            }
                        }
                    }
                }
                
                UpdateZoneLabels();
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error loading layout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingLayout = false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputDialog = new System.Windows.Window
                {
                    Title = "Save Layout",
                    Width = 300,
                    Height = 120,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                var textBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 5, 0, 10) };
                var buttonPanel = new StackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = WpfHorizontalAlignment.Right };
                var okButton = new System.Windows.Controls.Button { Content = "OK", IsDefault = true, Width = 70, Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", IsCancel = true, Width = 70 };

                stackPanel.Children.Add(new TextBlock { Text = "Enter layout name:" });
                stackPanel.Children.Add(textBox);
                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);
                inputDialog.Content = stackPanel;

                bool? result = null;
                okButton.Click += (s, args) => { result = true; inputDialog.Close(); };
                cancelButton.Click += (s, args) => { result = false; inputDialog.Close(); };

                inputDialog.ShowDialog();

                if (result == true && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // Get the screen bounds
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    
                    var layout = new SavedLayout
                    {
                        Name = textBox.Text,
                        Bounds = new Rect(Left, Top, Width, Height)
                    };

                    // Save each zone
                    foreach (var zone in zones)
                    {
                        if (zone != null)
                        {
                            // Get the zone's position relative to the screen
                            var point = zone.PointToScreen(new Point(0, 0));
                            var bounds = new Rect(
                                point.X,
                                point.Y,
                                zone.ActualWidth,
                                zone.ActualHeight);
                            
                            layout.Zones.Add(new LayoutZone
                            {
                                Bounds = bounds,
                                Column = Grid.GetColumn(zone),
                                Row = Grid.GetRow(zone),
                                ColumnSpan = Grid.GetColumnSpan(zone),
                                RowSpan = Grid.GetRowSpan(zone)
                            });
                        }
                    }

                    _layoutManager.SaveLayout(layout);
                    WpfMessageBox.Show($"Layout '{textBox.Text}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error saving layout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void Splitter_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (activeSplitter != null)
            {
                activeSplitter.ReleaseMouseCapture();
                isDragging = false;
                activeSplitter = null;
            }
        }
        
        private void Splitter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is GridSplitter splitter)
            {
                activeSplitter = splitter;
                isDragging = true;
                dragStartPoint = e.GetPosition(EditorGrid);
                initialSplitterPosition = LeftColumn.Width.Value;
                activeSplitter.CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void EditorGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && activeSplitter != null)
            {
                Point currentPoint = e.GetPosition(EditorGrid);
                double delta = currentPoint.X - dragStartPoint.X;
                double newWidth = initialSplitterPosition + delta;
                
                // Constrain the splitter within the grid
                newWidth = Math.Max(100, Math.Min(EditorGrid.ActualWidth - 100, newWidth));
                
                LeftColumn.Width = new GridLength(newWidth, GridUnitType.Pixel);
            }
        }
        
        private void EditorGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && activeSplitter != null)
            {
                activeSplitter.ReleaseMouseCapture();
                isDragging = false;
                activeSplitter = null;
                e.Handled = true;
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        
        private void SetWindowTransparency()
        {
            try
            {
                // Just set the window background to transparent
                // The controls will handle their own opacity
                this.Background = System.Windows.Media.Brushes.Transparent;
                this.Opacity = 1.0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting window transparency: {ex.Message}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    activeSplitter = null;
                    zones.Clear();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
