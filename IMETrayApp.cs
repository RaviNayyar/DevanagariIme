using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevanagariIME
{
    /// <summary>
    /// System tray application for the Devanagari IME
    /// </summary>
    public class IMETrayApp : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private KeyboardHook _keyboardHook;
        private ITRANSTranslator _translator;
        private bool _isEnabled = false;

        public IMETrayApp()
        {
            try
            {
                _translator = new ITRANSTranslator();
                
                _keyboardHook = new KeyboardHook(_translator);
                _keyboardHook.StatusChanged += OnStatusChanged;
                _keyboardHook.ToggleRequested += KeyboardHook_ToggleRequested;

                // Initialize tray icon
                _trayIcon = new NotifyIcon
                {
                    Icon = CreateIcon(),
                    ContextMenuStrip = CreateContextMenu(),
                    Text = "Devanagari IME",
                    Visible = true
                };

                _trayIcon.DoubleClick += TrayIcon_DoubleClick;
                
                UpdateIcon();
                // Only write to console if console output is enabled (not in silent tray mode)
                if (Program.EnableConsoleOutput)
                {
                    Console.WriteLine("System tray icon created. IME is DISABLED by default.");
                    Console.WriteLine("Right-click the tray icon and select 'Enable IME' to activate it.");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error initializing Devanagari IME:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                errorMsg += $"\n\nStack Trace:\n{ex.StackTrace}";
                
                // Only log to console if console output is enabled
                if (Program.EnableConsoleOutput)
                {
                    try
                    {
                        Console.WriteLine($"Error initializing IME: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                    catch { }
                }
                
                MessageBox.Show(errorMsg, 
                    "Devanagari IME Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                throw;
            }
        }

        private Icon CreateIcon()
        {
            try
            {
                // Create a simple icon with Devanagari character
                Bitmap bitmap = new Bitmap(16, 16);
                Graphics g = Graphics.FromImage(bitmap);
                try
                {
                    g.Clear(Color.White);
                    using (Font font = new Font("Arial", 10, FontStyle.Bold))
                    {
                        g.DrawString("द", font, Brushes.Black, 2, 2);
                    }
                }
                finally
                {
                    g.Dispose();
                }
                // Create icon and keep bitmap alive (icon owns the handle)
                IntPtr hIcon = bitmap.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);
                // Note: bitmap will be kept alive by the icon
                return icon;
            }
            catch (Exception ex)
            {
                if (Program.EnableConsoleOutput)
                {
                    Console.WriteLine($"Error creating icon: {ex.Message}");
                }
                // Fallback: use system icon
                return SystemIcons.Application;
            }
        }

        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem enableItem = new ToolStripMenuItem("Enable IME");
            enableItem.Click += EnableItem_Click;
            menu.Items.Add(enableItem);

            ToolStripMenuItem disableItem = new ToolStripMenuItem("Disable IME");
            disableItem.Click += DisableItem_Click;
            menu.Items.Add(disableItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += AboutItem_Click;
            menu.Items.Add(aboutItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;
            menu.Items.Add(exitItem);

            return menu;
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            ToggleIME();
        }

        private void EnableItem_Click(object? sender, EventArgs e)
        {
            EnableIME();
        }

        private void DisableItem_Click(object? sender, EventArgs e)
        {
            DisableIME();
        }

        private void AboutItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Devanagari IME\n\n" +
                "Type ITRANS text and press Space/Enter to convert to Devanagari.\n\n" +
                "Press Escape to clear the input buffer.\n\n" +
                "Press Ctrl+Shift+D to toggle IME on/off.\n\n" +
                "Right-click the tray icon to enable/disable the IME.",
                "Devanagari IME",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ExitItem_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ToggleIME()
        {
            if (_isEnabled)
                DisableIME();
            else
                EnableIME();
        }

        private void EnableIME()
        {
            try
            {
                if (Program.EnableConsoleOutput)
                {
                    Console.WriteLine("Attempting to enable IME...");
                }
                _keyboardHook.IsEnabled = true;
                _isEnabled = _keyboardHook.IsEnabled; // Update our state from actual hook state
                UpdateIcon();
                if (_isEnabled)
                {
                    _trayIcon.ShowBalloonTip(2000, "Devanagari IME", "IME Enabled", ToolTipIcon.Info);
                    if (Program.EnableConsoleOutput)
                    {
                        Console.WriteLine("IME enabled successfully.");
                    }
                }
                else
                {
                    _trayIcon.ShowBalloonTip(3000, "Devanagari IME", "Failed to enable IME. Check console for details.", ToolTipIcon.Warning);
                    if (Program.EnableConsoleOutput)
                    {
                        Console.WriteLine("IME enable failed - check error messages above.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Program.EnableConsoleOutput)
                {
                    Console.WriteLine($"Exception while enabling IME: {ex.Message}");
                }
                MessageBox.Show($"Error enabling IME:\n\n{ex.Message}", "Devanagari IME Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisableIME()
        {
            _isEnabled = false;
            _keyboardHook.IsEnabled = false;
            UpdateIcon();
            _trayIcon.ShowBalloonTip(2000, "Devanagari IME", "IME Disabled", ToolTipIcon.Info);
        }

        private void UpdateIcon()
        {
            // Change icon color or style based on enabled state
            // For now, just update the tooltip
            _trayIcon.Text = _isEnabled 
                ? "Devanagari IME - Enabled" 
                : "Devanagari IME - Disabled";
        }

        private void OnStatusChanged(object? sender, string status)
        {
            // Could update tooltip with buffer status
            // For now, just keep it simple
        }

        private void KeyboardHook_ToggleRequested(object? sender, EventArgs e)
        {
            // Toggle IME on/off when Ctrl+Shift+D is pressed
            ToggleIME();
        }

        protected override void ExitThreadCore()
        {
            _trayIcon.Visible = false;
            _keyboardHook.Dispose();
            base.ExitThreadCore();
        }
    }
}

