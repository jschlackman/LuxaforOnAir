using System;
using System.Configuration;
using System.Linq;
using System.Timers;
using LuxaforSharp;

namespace LuxOnAir
{

    /// <summary>
    /// Luxafor user settings
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class LFRSettings
    {
        /// <summary>
        /// Whether the user has enabled use of Luxafor lighting
        /// </summary>
        public bool Enabled;
        /// <summary>
        /// User color settings for Luxafor lights
        /// </summary>
        public StatusColors Colors;
        
        [NonSerialized]
        private IDeviceList devices;

        /// <summary>
        /// Indicates whether Luxafor lighting is available on this system
        /// </summary>
        /// <returns></returns>
        public bool Available()
        {
            return (devices != null);
        }

        /// <summary>
        /// Indicates whether Luxafor lighting is available and enabled for use by the user
        /// </summary>
        /// <returns></returns>
        public bool Active()
        {
            return Enabled && Available();
        }

        /// <summary>
        /// Current color to blink with
        /// </summary>
        [NonSerialized]
        private System.Drawing.Color currentColor;

        /// <summary>
        /// Timer object for light blink
        /// </summary>
        [NonSerialized]
        private Timer blinkTimer;

        /// <summary>
        /// Number of ms between blinks
        /// </summary>
        private const int blinkPeriod = 3000;

        public LFRSettings()
        {
            Enabled = true;
            Colors = new StatusColors();
        }

        /// <summary>
        /// Initilizes the Luxafor hardware
        /// </summary>
        /// <returns>Debug log message indicating how many devices were found.</returns>
        public string InitHardware()
        {
            string logMsg;
            
            // Now create a new device controller
            devices = new DeviceList();
            devices.Scan();

            if (devices.Count() == 0)
            {
                logMsg = "No Luxafor light available.";
            }
            else
            {
                logMsg = string.Format("{0} Luxafor light{1} ready.", devices.Count().ToString(), (devices.Count() != 1) ? "s" : "");
            }

            return logMsg;
        }

        /// <summary>
        /// Shutdown Luxafor lighting
        /// </summary>
        public void ShutdownHardware()
        {
            if (Available())
            {
                // Turn off the lights before shutting down
                foreach (IDevice device in devices)
                {
                    device.SetColor(LedTarget.All, new LuxaforSharp.Color(0, 0, 0));
                    device.Dispose();
                }
            }
        }

        /// <summary>
        /// Set all Luxafor lights to a specified color
        /// </summary>
        /// <param name="color">Color to set</param>
        private void SetAllLights(System.Drawing.Color color)
        {
            if (Available())
            {
                foreach (IDevice device in devices)
                {
                    // Set the required color with a short fade time
                    device.SetColor(LedTarget.All, new LuxaforSharp.Color(currentColor.R, currentColor.G, currentColor.B), 10);
                }
            }
        }

        /// <summary>
        /// Blink lights with the current color
        /// </summary>
        private void BlinkAllLights()
        {
            if (Available())
            {
                // Set the current color first
                SetAllLights(currentColor);

                // Create a time object if we don't already have one
                if (blinkTimer == null)
                {
                    blinkTimer = new Timer(blinkPeriod);
                    blinkTimer.Elapsed += BlinkTimer_Elapsed;
                }

                // Start the blink timer
                blinkTimer.Start();
            }
        }

        private void BlinkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (IDevice device in devices)
            {
                // Blink the current color once with a longer fade time
                device.Blink(LedTarget.All, new LuxaforSharp.Color(currentColor.R, currentColor.G, currentColor.B), 20, 1);
            }
        }


        /// <summary>
        /// Stop blinking the lights
        /// </summary>
        private void StopBlink()
        {
            if (blinkTimer != null) { blinkTimer.Stop(); }
        }

        /// <summary>
        /// Set Luxafor lights to in-use status
        /// </summary>
        public void SetInUse()
        {
            currentColor = System.Drawing.Color.FromArgb(Colors.MicInUse);
            SetAllLights(currentColor);
            if (Colors.BlinkMicInUse) { BlinkAllLights(); }
        }

        /// <summary>
        /// Set Luxafor lights to not-in-use status
        /// </summary>
        public void SetNotInUse()
        {
            StopBlink();
            currentColor = System.Drawing.Color.FromArgb(Colors.MicNotInUse);
            SetAllLights(currentColor);
        }

        /// <summary>
        /// Set Luxafor lights to locked status
        /// </summary>
        public void SetLocked()
        {
            StopBlink();
            currentColor = System.Drawing.Color.FromArgb(Colors.SessionLocked);
            SetAllLights(currentColor);
        }

        public void SetLightsOff()
        {
            currentColor = System.Drawing.Color.Black;
            SetAllLights(currentColor);
        }
    }
}
