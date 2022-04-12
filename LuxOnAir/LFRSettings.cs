using System;
using System.Linq;
using System.Timers;
using LuxaforSharp;

namespace LuxOnAir
{

    /// <summary>
    /// Luxafor user settings
    /// </summary>
    public class LFRSettings : LightSettings
    {
        [NonSerialized]
        private IDeviceList devices;

        /// <summary>
        /// Indicates whether Luxafor lighting is available on this system
        /// </summary>
        /// <returns></returns>
        private bool Available()
        {
            return (devices != null);
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
        [NonSerialized] 
        private const int blinkPeriod = 3000;

        /// <summary>
        /// Text description of a Luxafor light
        /// </summary>
        internal override string LightDescription => "Luxafor light";

        public override int InitSeconds => 1;

        public LFRSettings()
        {
            Colors = new StatusColors();
        }

        /// <summary>
        /// Initilizes the Luxafor hardware
        /// </summary>
        public override void InitHardware()
        {
            // Create a new device controller
            devices = new DeviceList();
            devices.Scan();
        }

        /// <summary>
        /// Gets the number of Luxafor devices currently connected.
        /// </summary>
        /// <returns>Numerical count of devices connected.</returns>
        public override int ConnectedDeviceCount()
        {
            return (devices == null ? 0 : devices.Count());
        }

        /// <summary>
        /// Shutdown Luxafor lighting
        /// </summary>
        public override void ShutdownHardware()
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
                    device.SetColor(LedTarget.All, new LuxaforSharp.Color(color.R, color.G, color.B), 10);
                }
            }
        }

        /// <summary>
        /// Wave all Luxafor lights with a specified color
        /// </summary>
        /// <param name="color">Color to set</param>
        private void WaveAllLights(System.Drawing.Color color)
        {
            if (Available())
            {
                foreach (IDevice device in devices)
                {
                    // Wave the required color
                    device.Wave(WaveType.Long, new LuxaforSharp.Color(color.R, color.G, color.B), 10, 0);
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
        public override void SetInUse()
        {
            currentColor = System.Drawing.Color.FromArgb(Colors.MicInUse);
            if (Colors.WaveMicInUse)
            {
                WaveAllLights(currentColor);
            }
            else
            {
                SetAllLights(currentColor);
                if (Colors.BlinkMicInUse) { BlinkAllLights(); }
            }

            
        }

        /// <summary>
        /// Set Luxafor lights to not-in-use status
        /// </summary>
        public override void SetNotInUse()
        {
            StopBlink();
            currentColor = System.Drawing.Color.FromArgb(Colors.MicNotInUse);
            SetAllLights(currentColor);
        }

        /// <summary>
        /// Set Luxafor lights to locked status
        /// </summary>
        public override void SetLocked()
        {
            StopBlink();
            currentColor = System.Drawing.Color.FromArgb(Colors.SessionLocked);
            SetAllLights(currentColor);
        }

        /// <summary>
        /// Turn all Luxafor lights off
        /// </summary>
        public override void SetLightsOff()
        {
            currentColor = System.Drawing.Color.Black;
            SetAllLights(currentColor);
        }
    }
}
