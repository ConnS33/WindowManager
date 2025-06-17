using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace WindowManager
{
    public partial class LayoutEditorWindow : Window
    {
        private bool isDragging = false;
        private Point dragStartPoint;
        private double initialSplitterPosition;
        private GridSplitter? activeSplitter;
        private List<Grid> zones = new List<Grid>();
        private int zoneCounter = 0;

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
                
                if (LeftZone != null && !zones.Contains(LeftZone))
                {
                    zones.Add(LeftZone);
                    Console.WriteLine("Added LeftZone to zones list in constructor");
                }
                
                // Set up key handler
                this.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) this.Close(); };
                
                // Initialize with a single zone
                zoneCounter = 1;
                
                Console.WriteLine("LayoutEditorWindow constructor completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LayoutEditorWindow constructor: {ex}");
                MessageBox.Show($"Error initializing layout editor: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Window_Loaded started");
                
                // Get the primary screen dimensions
                var primaryScreen = System.Windows.SystemParameters.WorkArea;
                
                // Set window to cover only the primary screen
                this.Left = primaryScreen.Left;
                this.Top = primaryScreen.Top;
                this.Width = primaryScreen.Width;
                this.Height = primaryScreen.Height;
                
                Console.WriteLine($"Window positioned at ({this.Left}, {this.Top}) with size {this.Width}x{this.Height}");
                
                // Initialize zones list if needed
                if (zones == null)
                {
                    zones = new List<Grid>();
                    Console.WriteLine("Initialized zones list");
                }
                
                // Add left zone if not already present
                if (LeftZone != null && !zones.Contains(LeftZone))
                {
                    zones.Add(LeftZone);
                    Console.WriteLine("Added LeftZone to zones list in Window_Loaded");
                }
                
                // Hide the right column initially
                if (RightColumn != null)
                {
                    RightColumn.Width = new GridLength(0);
                    Console.WriteLine("RightColumn width set to 0");
                }
                
                if (VerticalSplitter != null)
                {
                    VerticalSplitter.Visibility = Visibility.Collapsed;
                    Console.WriteLine("VerticalSplitter hidden");
                }
                
                if (RightZone != null)
                {
                    RightZone.Visibility = Visibility.Collapsed;
                    Console.WriteLine("RightZone hidden");
                }
                
                // Update zone labels
                Console.WriteLine("Calling UpdateZoneLabels");
                UpdateZoneLabels();
                Console.WriteLine("Window_Loaded completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Window_Loaded: {ex}");
                MessageBox.Show($"Error initializing layout editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Maximum of 2 zones supported in this version.", "Zone Limit", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateZoneLabels()
        {
            try
            {
                Console.WriteLine("UpdateZoneLabels started");
                
                if (LeftZoneText == null)
                {
                    Console.WriteLine("LeftZoneText is null!");
                    return;
                }
                
                Console.WriteLine("Setting LeftZoneText visibility");
                LeftZoneText.Visibility = Visibility.Visible;
                
                // Right zone visibility depends on zones count
                if (RightZoneText == null)
                {
                    Console.WriteLine("RightZoneText is null!");
                    return;
                }
                
                Console.WriteLine($"Zones count: {zones?.Count}");
                if (zones?.Count > 1)
                {
                    Console.WriteLine("Making RightZoneText visible");
                    RightZoneText.Visibility = Visibility.Visible;
                }
                else
                {
                    Console.WriteLine("Hiding RightZoneText");
                    RightZoneText.Visibility = Visibility.Collapsed;
                }
                
                Console.WriteLine("UpdateZoneLabels completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateZoneLabels: {ex}");
                throw; // Re-throw to see the full stack trace
            }
        }



        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Calculate the split position as a percentage
            double totalWidth = EditorGrid.ActualWidth;
            double leftWidth = LeftColumn.ActualWidth;
            double splitPosition = leftWidth / totalWidth;
            
            // TODO: Save the split position to settings
            // For now, just show a message
            MessageBox.Show($"Layout saved with split at {splitPosition:P0}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
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
            activeSplitter = (GridSplitter)sender;
            isDragging = true;
            dragStartPoint = e.GetPosition(EditorGrid);
            initialSplitterPosition = LeftColumn.Width.Value;
            activeSplitter.CaptureMouse();
            e.Handled = true;
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


    }
}
