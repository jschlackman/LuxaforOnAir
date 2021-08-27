using LuxOnAir.Properties;

namespace LuxOnAir
{
    static class SettingsHelper
    {
        /// <summary>
        /// Windows autorun registry key path
        /// </summary>
        public const string RegWindowsRunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        /// <summary>
        /// Program-specific name for autorun value and message broadcast
        /// </summary>
        public const string ProgramValueID = "LuxOnAir";

        /// <summary>
        /// About text for this app
        /// </summary>
        public const string About = "by James Schlackman\n\nThis software uses functionality from the following libraries:\n• LuxaforSharp by Edouard Paumier\n• HidLibrary by Mike O'Brien, Austin Mullins, and other contributors.";

        /// <summary>
        /// Load previous settings from configuration file, or initialize defaults for the specific light type if no previous settings saved
        /// </summary>
        /// <returns>True if settings were loaded, False if defaults were used</returns>
        public static bool LoadSettings()
        {
            // If no settings file, attempt to upgrade from previous version's settings
            if (Settings.Default.Lights == null) { Settings.Default.Upgrade(); }

            bool bLoaded = Settings.Default.Lights != null;

            // If still no settings file, initialize defaults
            if (!bLoaded)
            {
                Settings.Default.Lights = new LFRSettings();
            }

            return bLoaded;
        }
    }
}
