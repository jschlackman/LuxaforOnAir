using System.Configuration;

namespace LuxOnAir
{
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public abstract class LightSettings
    {
        /// <summary>
        /// User-configured RGB light colors
        /// </summary>
        public StatusColors Colors;

        /// <summary>
        /// Initilizes the RGB light hardware
        /// </summary>
        /// <returns>Text message indicating how many light devices were found.</returns>
        public abstract void InitHardware();

        /// <summary>
        /// Shutdown RGB lighting
        /// </summary>
        public abstract void ShutdownHardware();

        /// <summary>
        /// Gets the number of RGB light devices currently connected.
        /// </summary>
        /// <returns>Numerical count of devices connected.</returns>
        public abstract int ConnectedDeviceCount();

        /// <summary>
        /// Turn all RGB lights off
        /// </summary>
        public abstract void SetLightsOff();

        /// <summary>
        /// Set RGB lights to locked status
        /// </summary>
        public abstract void SetLocked();

        /// <summary>
        /// Set RGB lights to not-in-use status
        /// </summary>
        public abstract void SetNotInUse();

        /// <summary>
        /// Set RGB lights to in-use status
        /// </summary>
        public abstract void SetInUse();

        /// <summary>
        /// Text decription of this type of RGB light
        /// </summary>
        internal abstract string LightDescription
        {
            get;
        }

        /// <summary>
        /// Generates a text description of how many RGB devices are currently connected.
        /// </summary>
        /// <returns>User-friendly text describing how many RGB devices are connected.</returns>
        public string ConnectedDeviceDesc()
        {
            int devCount = ConnectedDeviceCount();
            if (devCount == 0)
            {
                return string.Format("No {0} connected.", LightDescription);
            }
            else
            {
                return string.Format("{0} {1}{2} connected.", devCount.ToString(), LightDescription, (devCount != 1) ? "s" : "");
            }
        }
    }
}
