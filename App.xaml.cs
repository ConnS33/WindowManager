using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using Application = System.Windows.Application;
using System.Collections.Generic;
using WindowManager.Services;
using WindowManager.Models;

namespace WindowManager
{
    public partial class App : Application
    {
        private MainWindow? mainWindow;
        private NotifyIcon? notifyIcon;
        private bool isEnabled = true;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            mainWindow = new MainWindow();
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                Visible = true,
                Text = "Window Manager"
            };

            var contextMenu = new ContextMenuStrip();
            
            var toggleItem = new ToolStripMenuItem("Enable/Disable");
            toggleItem.Click += (s, e) => ToggleEnabled();
            
            // Add Saved Layouts dropdown
            var savedLayoutsItem = new ToolStripMenuItem("Apply Layout");
            UpdateSavedLayoutsMenu(savedLayoutsItem);
            
            var screenSetupItem = new ToolStripMenuItem("Edit Layouts");
            screenSetupItem.Click += (s, e) => ShowLayoutEditor();
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Shutdown();
            
            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(savedLayoutsItem);
            contextMenu.Items.Add(screenSetupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);
            
            // Refresh layouts when the menu opens
            contextMenu.Opening += (s, e) => UpdateSavedLayoutsMenu(savedLayoutsItem);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => ToggleEnabled();
        }
        
        private void UpdateSavedLayoutsMenu(ToolStripMenuItem parentItem)
        {
            parentItem.DropDownItems.Clear();
            
            try
            {
                var layoutManager = new LayoutManager();
                var layouts = layoutManager.LoadAllLayouts();
                
                if (layouts.Count == 0)
                {
                    var noLayoutsItem = new ToolStripMenuItem("No saved layouts");
                    noLayoutsItem.Enabled = false;
                    parentItem.DropDownItems.Add(noLayoutsItem);
                    return;
                }
                
                foreach (var layout in layouts)
                {
                    if (layout == null || string.IsNullOrEmpty(layout.Name)) continue;
                    
                    var layoutItem = new ToolStripMenuItem(layout.Name);
                    layoutItem.Tag = layout;
                    layoutItem.Click += (s, e) => 
                    {
                        if (s is ToolStripMenuItem menuItem && menuItem.Tag is SavedLayout selectedLayout)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    var window = Application.Current.MainWindow as MainWindow;
                                    if (window != null)
                                    {
                                        window.ApplyLayout(selectedLayout, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.MessageBox.Show($"Error applying layout: {ex.Message}", 
                                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            });
                        }
                    };
                    parentItem.DropDownItems.Add(layoutItem);
                }
            }
            catch (Exception ex)
            {
                var errorItem = new ToolStripMenuItem("Error loading layouts");
                errorItem.Enabled = false;
                parentItem.DropDownItems.Add(errorItem);
                Debug.WriteLine($"Error loading layouts: {ex}");
            }
        }
        
        private void ShowLayoutEditor()
        {
            var layoutEditor = new LayoutEditorWindow();
            layoutEditor.Owner = Application.Current.MainWindow;
            layoutEditor.Closed += (s, e) => 
            {
                // Refresh the main window's layout list when the editor closes
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.LoadLayouts();
                }
            };
            layoutEditor.ShowDialog();
        }

        private void ToggleEnabled()
        {
            isEnabled = !isEnabled;
            if (notifyIcon != null)
            {
                notifyIcon.Text = $"Window Manager ({(isEnabled ? "Enabled" : "Disabled")})";
            }
            
            if (isEnabled)
            {
                mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                mainWindow?.Close();
                mainWindow = null;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        }
    }
}
