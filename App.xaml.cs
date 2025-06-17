using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;

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
            
            var screenSetupItem = new ToolStripMenuItem("Edit Layout");
            screenSetupItem.Click += (s, e) => ShowLayoutEditor();
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Shutdown();
            
            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(screenSetupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => ToggleEnabled();
        }
        
        private void ShowLayoutEditor()
        {
            var layoutEditor = new LayoutEditorWindow();
            layoutEditor.Owner = Application.Current.MainWindow;
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
