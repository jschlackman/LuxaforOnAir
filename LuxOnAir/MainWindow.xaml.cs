using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Forms;
using LuxOnAir.Properties;
using System.Management;
using System.Windows.Media;
using System.Threading;
using System.Windows.Interop;

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
        /// Watches for registry changes (mic in use)
        /// </summary>
        private static ManagementEventWatcher regWatcher;

        /// <summary>
        /// Watches for system power events (e.g. suspend/resume)
        /// </summary>
        private static ManagementEventWatcher powerWatcher;

        /// <summary>
        /// Keeps track of whether the console is locked or unlocked.
        /// </summary>
        private static bool bConsoleLocked = false;

        /// <summary>
        /// Stores the window's state prior to being minimized to the notification area
        /// </summary>
        private WindowState storedWindowState = WindowState.Normal;

        /// <summary>
        /// Mutex used to check whether another instance of this app is already running.
        /// </summary>
        private static readonly Mutex mutex = new Mutex(true, "{FEB97F90-3F85-429F-9F59-526F29856FC0}");

        /// <summary>
        /// Whether to hide the settings UI on startup
        /// </summary>
        private static bool HideOnStart = true;

        public MainWindow()
        {
            // Check no other instance is already running
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {

                InitializeComponent();

                lblProductVer.Content = string.Format("{0} {1}", System.Windows.Forms.Application.ProductName, System.Windows.Forms.Application.ProductVersion);
                lblAbout.Content = SettingsHelper.About;

                SystemEvents.SessionSwitch += SessionSwitchHandler = new SessionSwitchEventHandler(OnSessionSwitch);

                InitNotifyIcon();
                LoadSettings();

                // Initialize devices
                Settings.Default.Lights.InitHardware();
                WriteToDebug(Settings.Default.Lights.ConnectedDeviceDesc());

                // Update the device status UI
                UpdateDeviceStatus();

                // Check if program is correctly set to run at logon
                chkStartAtLogon.IsChecked = GetRunAtLogon();

                // Get current user SID so we can monitor the correct key in the USERS registry hive
                string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;

                // Watch for registry changes
                regWatcher = new ManagementEventWatcher
                {
                    Query = new WqlEventQuery(string.Format("SELECT * FROM RegistryTreeChangeEvent WHERE Hive='HKEY_USERS' AND RootPath='{0}\\\\{1}'", sid, MicrophoneHelper.MicCapabilityKey.Replace("\\", "\\\\")))
                };
                regWatcher.EventArrived += RegWatcher_EventArrived;
                regWatcher.Start();

                // Watch for hardware changes
                hardwareWatcher = new ManagementEventWatcher
                {
                    Query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity' GROUP WITHIN " + Settings.Default.Lights.InitSeconds.ToString())
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

                // Run the first mic check now
                CheckMicUsage();

            }
            else
            {
                // Another instance is already running
                
                // Message the other instance to show the settings window
                MessageHelper.PostMessage((IntPtr)MessageHelper.HWND_BROADCAST, MessageHelper.WM_SHOWME, IntPtr.Zero, IntPtr.Zero);

                ReallyExit = true;
                Close();
            }
        }

        /// <summary>
        /// Load saved settings and reflect them in the UI
        /// </summary>
        private void LoadSettings()
        {
            // Try to load settings, init defaults and show UI if no previous settings found
            if (!SettingsHelper.LoadSettings())
            {
                WriteToDebug("No previous settings found, loading defaults and showing UI for first run.");
                HideOnStart = false;
            }

            btnMicInUse.Background = Settings.Default.Lights.Colors.MicInUse.ToBrush();
            btnMicNotInUse.Background = Settings.Default.Lights.Colors.MicNotInUse.ToBrush();
            btnLocked.Background = Settings.Default.Lights.Colors.SessionLocked.ToBrush();

            chkInUseBlink.IsChecked = Settings.Default.Lights.Colors.BlinkMicInUse;
            chkInUseWave.IsChecked = Settings.Default.Lights.Colors.WaveMicInUse;
        }

        /// <summary>
        /// Ensure currently configured settings are saved from the UI
        /// </summary>
        private void ApplySettings()
        {
            Settings.Default.Save();
            CheckMicUsage();
        }

        /// <summary>
        /// Handles registry change events. Check for mic usage whenever a change is detected.
        /// </summary>
        private void RegWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            CheckMicUsage();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // Listen for windows messages
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);

            // Now that the window has fully initialized, hide it if we aren't showing the settings screen immediately.
            if (HideOnStart) { WindowState = WindowState.Minimized; }
        }

        // Handle incoming windows messages
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == MessageHelper.WM_SHOWME)
            {
                ShowSettingsWindow();
                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Determines whether the application is set to run automatically when the user logs on
        /// </summary>
        private bool GetRunAtLogon()
        {
            // Check if registry value exists and has the correct value for the current app path
            return Registry.CurrentUser.OpenSubKey(SettingsHelper.RegWindowsRunKey).GetValue(SettingsHelper.ProgramValueID, "").ToString() == string.Format("\"{0}\"", System.Windows.Forms.Application.ExecutablePath);
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
                    windowsRun.SetValue(SettingsHelper.ProgramValueID, string.Format("\"{0}\"", System.Windows.Forms.Application.ExecutablePath));
                    WriteToDebug(string.Format("Added registry key at {0} to run app at startup.", windowsRun.Name));
                }
            }
            else
            {
                // Check for the presence of the startup registry value and remove it
                if (windowsRun.GetValue(SettingsHelper.ProgramValueID) != null)
                {
                    windowsRun.DeleteValue(SettingsHelper.ProgramValueID);
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
            Settings.Default.Lights.InitHardware();
            WriteToDebug(Settings.Default.Lights.ConnectedDeviceDesc());

            // Update the device status UI
            UpdateDeviceStatus();
            // Run a mic check now
            CheckMicUsage();
        }

        /// <summary>
        /// Update the indicator on the main settings screen to show how many devices are connected
        /// </summary>
        private void UpdateDeviceStatus()
        {
            // Thread-safe UI update
            Dispatcher.Invoke(() =>
            {
                lblStatus.Content = Settings.Default.Lights.ConnectedDeviceDesc();

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
                            CheckMicUsage();
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
                    CheckMicUsage();
                    break;
                default:
                    WriteToDebug("Console session locked. Setting to Locked color.");
                    bConsoleLocked = true;
                    Settings.Default.Lights.SetLocked();
                break;
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
        /// Check if the microphone is in use and react accordingly
        /// </summary>
        /// <returns>Text names of all applications using the microphone</returns>
        private List<string> CheckMicUsage()
        {
            List<string> micUsers = MicrophoneHelper.GetMicUsers();

            if (bConsoleLocked)
            {
                Settings.Default.Lights.SetLocked();
            }
            // Check if any applications were found using the mic
            else if (micUsers.Count > 0)
            {
                // Trigger mic in use lights
                Settings.Default.Lights.SetInUse();
            }
            else
            {
                // Trigger mic not in use lights
                Settings.Default.Lights.SetNotInUse();
            }

            return micUsers;

        }

        /// <summary>
        /// Write a message to the debug log control
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
            ShowSettingsWindow();
        }

        /// <summary>
        /// Show the settings window with the same state (maximized or not) as before it was hidden.
        /// </summary>
        private void ShowSettingsWindow()
        {
            Show();
            WindowState = storedWindowState;

            // Store the current Topmost value (usually false)
            bool top = this.Topmost;
            // Make settings jump to the top of everything
            this.Topmost = true;
            // Set it back to whatever it was before
            this.Topmost = top;
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
            SystemEvents.SessionSwitch -= SessionSwitchHandler;

            notifyIcon.Dispose();

            regWatcher.Stop();
            regWatcher.Dispose();
            hardwareWatcher.Stop();
            hardwareWatcher.Dispose();
            powerWatcher.Stop();
            powerWatcher.Dispose();

            // Release the single-instance mutex
            mutex.ReleaseMutex();
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
            List<string> micUsers = CheckMicUsage();

            if (micUsers.Count > 0)
            {
                WriteToDebug(string.Format("Mic is in use by {0} app{1}:\n{2}", micUsers.Count, micUsers.Count == 1 ? "" : "s", string.Join("\n", micUsers)));
            }
            else
            {
                WriteToDebug("Mic is not currently in use.");
            }
        }

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
            // If using blink, turn off wave
            if (Settings.Default.Lights.Colors.BlinkMicInUse) { chkInUseWave.IsChecked = false; }
            ApplySettings();
        }

        private void ChkInUseWave_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Default.Lights.Colors.WaveMicInUse = (bool)chkInUseWave.IsChecked;
            // If using wave, turn off blink
            if (Settings.Default.Lights.Colors.WaveMicInUse) { chkInUseBlink.IsChecked = false; }
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

        private void BtnTestInUse_Click(object sender, RoutedEventArgs e)
        {
            WriteToDebug("Testing 'In Use' color.");
            Settings.Default.Lights.SetInUse();
        }

        private void BtnTestNotInUse_Click(object sender, RoutedEventArgs e)
        {
            WriteToDebug("Testing 'Not In Use' color.");
            Settings.Default.Lights.SetNotInUse();
        }

        private void BtnTestLocked_Click(object sender, RoutedEventArgs e)
        {
            WriteToDebug("Testing 'Console Locked' color.");
            Settings.Default.Lights.SetLocked();
        }

        private void BtnTestReset_Click(object sender, RoutedEventArgs e)
        {
            WriteToDebug("Resetting to normal status color.");
            CheckMicUsage();
        }

        private void TabItem_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckMicUsage();
        }
    }
}
