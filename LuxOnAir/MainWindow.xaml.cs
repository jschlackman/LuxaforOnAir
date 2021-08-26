using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using Microsoft.Win32;
using System.Windows.Forms;
using LuxOnAir.Properties;
using System.Management;
using System.Windows.Media;

namespace LuxOnAir
{
        /// <summary>
        /// Interaction logic for MainWindow.xaml
        /// </summary>
        public partial class MainWindow : Window
    {
        /// <summary>
        /// Indicate if the application should fully exit when closing a window.
        /// </summary>
        private bool ReallyExit = false;

        /// <summary>
        /// String values used by notification icons that indicate the microphone is in use.
        /// </summary>
        private readonly List<string> InUseText;

        /// <summary>
        /// Notification icon for this application
        /// </summary>
        private static NotifyIcon notifyIcon;

        /// <summary>
        /// Handles session switch events
        /// </summary>
        private static SessionSwitchEventHandler SessionSwitchHandler;

        /// <summary>
        /// Watches for system hardware changes (e.g. USB connect/disconnect)
        /// </summary>
        private static ManagementEventWatcher hardwareWatcher;
        
        /// <summary>
        /// Watches for system power events (e.g. suspend/resume)
        /// </summary>
        private static ManagementEventWatcher powerWatcher;

        /// <summary>
        /// Keeps track of whether the console is locked or unlocked.
        /// </summary>
        private static bool bConsoleLocked = false;

        public MainWindow()
        {
            
            InitializeComponent();
            
            lblProductVer.Content = string.Format("{0} {1}", System.Windows.Forms.Application.ProductName, System.Windows.Forms.Application.ProductVersion);
            lblAbout.Content = SettingsHelper.About;

            ShellEvents.InitTrayHooks(new StructureChangedEventHandler(OnStructureChanged));
            SystemEvents.SessionSwitch += SessionSwitchHandler = new SessionSwitchEventHandler(OnSessionSwitch);

            InitNotifyIcon();

            // Try to load settings, init defaults and show UI if no previous settings found
            if (!SettingsHelper.LoadSettings())
            {
                WriteToDebug("No previous settings found, loading defaults and showing UI for first run.");
                
                // Always show the UI on first run
                Visibility = Visibility.Visible;
            }

            InUseText = WindowsStrings.GetMicUseStrings();
            
            // Initialize devices
            WriteToDebug(Settings.Default.Lights.InitHardware());

            // Update the device status UI
            UpdateDeviceStatus();

            btnMicInUse.Background = Settings.Default.Lights.Colors.MicInUse.ToBrush();
            btnMicNotInUse.Background = Settings.Default.Lights.Colors.MicNotInUse.ToBrush();
            btnLocked.Background = Settings.Default.Lights.Colors.SessionLocked.ToBrush();

            chkInUseBlink.IsChecked = Settings.Default.Lights.Colors.BlinkMicInUse;

            // Check if program is correctly set to run at logon
            chkStartAtLogon.IsChecked = GetRunAtLogon();

            // Watch for hardware changes
            hardwareWatcher = new ManagementEventWatcher
            {
                Query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity' GROUP WITHIN 1")
            };
            hardwareWatcher.EventArrived += USBDevices_Changed;
            hardwareWatcher.Start();

            // Watch for power changes
            powerWatcher = new ManagementEventWatcher
            {
                Query = new WqlEventQuery("SELECT * FROM Win32_PowerManagementEvent")
            };
            powerWatcher.EventArrived += PowerEvent_Arrive;
            powerWatcher.Start();

            // Run the first icon check now
            CheckNotificationIcons();

        }

        /// <summary>
        /// Determines whether the application is set to run automatically when the user logs on
        /// </summary>
        private bool GetRunAtLogon()
        {
            // Check if registry value exists and has the correct value for the current app path
            return Registry.CurrentUser.OpenSubKey(SettingsHelper.RegWindowsRunKey).GetValue(SettingsHelper.RegProgramValue, "").ToString() == string.Format("\"{0}\"", System.Windows.Forms.Application.ExecutablePath);
        }

        /// <summary>
        /// Sets whether the application is set to run automatically when the user logs on
        /// </summary>
        private void SetRunAtLogon(bool value)
        {
            // Get reference to the current user's Windows Run key with edit permissions
            RegistryKey windowsRun = Registry.CurrentUser.OpenSubKey(SettingsHelper.RegWindowsRunKey, true);

            if (value)
            {
                // If not already set to run at logon, set the correct registry key now
                if (!GetRunAtLogon())
                {
                    windowsRun.SetValue(SettingsHelper.RegProgramValue, string.Format("\"{0}\"", System.Windows.Forms.Application.ExecutablePath));
                    WriteToDebug(string.Format("Added registry key at {0} to run app at startup.", windowsRun.Name));
                }
            }
            else
            {
                // Check for the presence of the startup registry value and remove it
                if (windowsRun.GetValue(SettingsHelper.RegProgramValue) != null)
                {
                    windowsRun.DeleteValue(SettingsHelper.RegProgramValue);
                    WriteToDebug(string.Format("Removed registry key from {0}, app will no longer run at startup.", windowsRun.Name));
                }
            }
        }

        /// <summary>
        /// Respond to hardware changes
        /// </summary>
        private void USBDevices_Changed(object sender, EventArrivedEventArgs e)
        {
            WriteToDebug("Hardware change detected.");
            WriteToDebug(Settings.Default.Lights.InitHardware());

            // Update the device status UI
            UpdateDeviceStatus();
            // Run an icon check now
            CheckNotificationIcons();
        }

        /// <summary>
        /// Update the indicator on the mains ettings screen to show how many devices are connected
        /// </summary>
        private void UpdateDeviceStatus()
        {
            // Thread-safe UI update
            Dispatcher.Invoke(() =>
            {
                lblStatus.Content = Settings.Default.Lights.InitHardware();

                if (Settings.Default.Lights.ConnectedDeviceCount() == 0)
                {
                    // Red status indicator
                    elpStatus.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x58, 0x58));
                    elpStatus.Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0xB7, 0, 0));
                }
                else
                {
                    // Green status indicator
                    elpStatus.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x55, 0xBB, 0x55));
                    elpStatus.Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0, 0xB6, 0));
                }
            });
        }

        /// <summary>
        /// Respond to suspend/resume events
        /// </summary>
        private void PowerEvent_Arrive(object sender, EventArrivedEventArgs e)
        {
            foreach (PropertyData pd in e.NewEvent.Properties)
            {
                if (pd.Value != null)
                {
                    string eventValue = pd.Value.ToString();
                    // Entering suspend
                    if (eventValue == "4")
                    {
                        WriteToDebug("System entering Suspend, turning lights off.");
                        Dispatcher.Invoke(() =>
                        {
                            Settings.Default.Lights.SetLightsOff();
                        });
                    } 
                    // Resuming from suspend
                    else if (eventValue == "7")
                    {
                        WriteToDebug("System resuming from Suspend, turning lights back on.");
                        Dispatcher.Invoke(() =>
                        {
                            CheckNotificationIcons();
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Respond to session switching (workstation lock/unlock)
        /// </summary>
        public void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.ConsoleConnect:
                    WriteToDebug("Console session unlocked. Setting to standard color.");
                    bConsoleLocked = false;
                    CheckNotificationIcons();
                    break;
                default:
                    WriteToDebug("Console session locked. Setting to Locked color.");
                    bConsoleLocked = true;
                    Settings.Default.Lights.SetLocked();
                break;
            }
        }

        /// <summary>
        /// Handles structure-changed events. If a new element has been added or removed, makes
        /// sure that notification icons are re-checked for mic use
        /// </summary>
        private void OnStructureChanged(object sender, StructureChangedEventArgs e)
        {
            // If an element was added or removed from the UI structure, check the notification icons
            if ((e.StructureChangeType == StructureChangeType.ChildAdded) || (e.StructureChangeType == StructureChangeType.ChildRemoved))
            {
                CheckNotificationIcons();
            }
        }

        /// <summary>
        /// Initialize the notification icon for this application
        /// </summary>
        private void InitNotifyIcon()
        {
            // Create an Exit menu item
            ToolStripMenuItem ExitMenuItem = new ToolStripMenuItem()
            {
                Name = "ExitMenuItem",
                Text = "Exit"
            };
            ExitMenuItem.Click += ExitMenuItem_Click;

            // Create a Settings menu item
            ToolStripMenuItem SettingsMenuItem = new ToolStripMenuItem()
            {
                Name = "SettingsMenuItem",
                Text = "Show Settings"
            };
            SettingsMenuItem.Click += SettingsMenuItem_Click;

            ToolStripSeparator SpacerMenuItem = new ToolStripSeparator();

            // Create the context menu for the notification icon
            ContextMenuStrip TrayIconContextMenu = new ContextMenuStrip()
            {
                Name = "TrayIconContextMenu"
            };

            // Add items to the menu
            TrayIconContextMenu.SuspendLayout();
            TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {SettingsMenuItem, SpacerMenuItem, ExitMenuItem});
            TrayIconContextMenu.ResumeLayout(false);

            // Define the notification icon 
            notifyIcon = new NotifyIcon
            {
                Icon = Properties.Resources.NotifyIcon,
                Text = System.Windows.Forms.Application.ProductName,
                ContextMenuStrip = TrayIconContextMenu,
                Visible = true,
            };

            notifyIcon.MouseDoubleClick += SettingsMenuItem_Click;

        }

        /// <summary>
        /// Check notification icons for a microphone in use icon and react accordingly
        /// </summary>
        /// <returns>Text names of all found notification icons</returns>
        private string CheckNotificationIcons()
        {
            string iconNames = "";
            
            // Query text labels of all notification icons
            foreach (AutomationElement icon in ShellEvents.EnumNotificationIcons())
            {
                string name = icon.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;

                // Append to list if not blank
                if (name != "")
                {
                    iconNames += name + '\n';
                }
            }

            if (bConsoleLocked)
            {
                Settings.Default.Lights.SetLocked();
            }
            // Check if any of the in use strings are currently being displayed by a notification icon
            else if (InUseText.Any(s => iconNames.Contains(s + "\n")))
            {
                // Trigger mic in use lights
                Settings.Default.Lights.SetInUse();
            }
            else
            {
                // Trigger mic not in use lights
                Settings.Default.Lights.SetNotInUse();
            }

            return iconNames;

        }

        /// <summary>
        /// Write a message to thr debug log control
        /// </summary>
        /// <param name="Msg">Message to write to the log</param>
        /// <param name="NoNewLine">Whether to add a new line at the end of the message</param>
        private void WriteToDebug(string Msg, bool NoNewLine = false)
        {
            Dispatcher.Invoke(() =>
            {
                txtDebugLog.AppendText(DateTime.Now.ToString("'['yy'-'MM'-'dd HH':'mm':'ss']' ") + Msg);
                if (!NoNewLine) { txtDebugLog.AppendText("\n"); }
                txtDebugLog.ScrollToEnd();
            });
        }

        private void ApplySettings()
        {
            Settings.Default.Save();
            CheckNotificationIcons();
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings();
            Hide();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            ReallyExit = true;
            Close();
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = storedWindowState;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Hide the window, don't actually quit unless we used an Exit button or menu
            if (!ReallyExit)
            {
                Hide();
                e.Cancel = true;
            }

            Settings.Default.Save();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Lights.ShutdownHardware();
            ShellEvents.DisposeTrayHooks();
            SystemEvents.SessionSwitch -= SessionSwitchHandler;

            notifyIcon.Dispose();

            hardwareWatcher.Stop();
            hardwareWatcher.Dispose();
            powerWatcher.Stop();
            powerWatcher.Dispose();
        }

        private void BtnMicInUse_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog()
            {
                Color = System.Drawing.Color.FromArgb(Settings.Default.Lights.Colors.MicInUse)
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Default.Lights.Colors.MicInUse = colorDialog.Color.ToArgb();
                btnMicInUse.Background = Settings.Default.Lights.Colors.MicInUse.ToBrush();
                ApplySettings();
            }
        }

        private void BtnMicNotInUse_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog()
            {
                Color = System.Drawing.Color.FromArgb(Settings.Default.Lights.Colors.MicNotInUse)
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Default.Lights.Colors.MicNotInUse = colorDialog.Color.ToArgb();
                btnMicNotInUse.Background = Settings.Default.Lights.Colors.MicNotInUse.ToBrush();
                ApplySettings();
            }
        }

        private void BtnLocked_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog()
            {
                Color = System.Drawing.Color.FromArgb(Settings.Default.Lights.Colors.SessionLocked)
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Default.Lights.Colors.SessionLocked = colorDialog.Color.ToArgb();
                btnLocked.Background = Settings.Default.Lights.Colors.SessionLocked.ToBrush();
                ApplySettings();
            }
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            WriteToDebug("Found tray icons:\n" + CheckNotificationIcons());
        }

        /// <summary>
        /// Stores the windows state prior to being minimized to the notification area
        /// </summary>
        private WindowState storedWindowState = WindowState.Normal;

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                storedWindowState = WindowState;
            }

        }

        private void ChkInUseBlink_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Default.Lights.Colors.BlinkMicInUse = (bool)chkInUseBlink.IsChecked;
            ApplySettings();
        }

        private void ChkStartAtLogon_Changed(object sender, RoutedEventArgs e)
        {
            // Set the correct registry value to match whether the box is checked
            SetRunAtLogon((bool)chkStartAtLogon.IsChecked);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
