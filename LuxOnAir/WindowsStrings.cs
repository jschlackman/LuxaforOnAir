using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LuxOnAir
{
    // Text resource code adapted from https://www.martinstoeckli.ch/csharp/csharp.html#windows_text_resources

    /// <summary>
    /// Searches for a text resource in a Windows library, allowing use of existing Windows resources for language independence.
    /// </summary>
    internal class WindowsStrings
    {

        /// <summary>
        /// Searches for a text resource in a Windows library.
        /// </summary>
        /// <example>
        ///   btnCancel.Text = WindowsStrings.Load("user32.dll", 801, "Cancel");
        ///   btnYes.Text = WindowsStrings.Load("user32.dll", 805, "Yes");
        /// </example>
        /// <param name="libraryName">Name of the windows library, e.g. "user32.dll" or "shell32.dll"</param>
        /// <param name="ident">ID of the string resource.</param>
        /// <param name="defaultText">Return this text, if the resource string could not be found.</param>
        /// <returns>Requested string if the resource was found, otherwise the <paramref name="defaultText"/></returns>
        private static string Load(IntPtr libraryHandle, uint ident, string defaultText)
        {
            if (libraryHandle != IntPtr.Zero)
            {
                StringBuilder sb = new StringBuilder(1024);
                int size = LoadString(libraryHandle, ident, sb, 1024);
                if (size > 0)
                {
                    return sb.ToString();
                }
            }
            return defaultText;
        }

        /// <summary>
        /// Gets a list of known strings that may be used to indicate the microphone is in use.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetMicUseStrings()
        {
            List<string> moduleStrings = new List<string>();

            // Get a new handle to the external library containing strings used for the mic in use notification
            IntPtr libraryHandle = LoadLibrary("sndvolsso.dll");

            if (libraryHandle != IntPtr.Zero)
            {
                // Load each known string resource (using en-US defaults if not found)
                moduleStrings.Add(Load(libraryHandle, 2045, "Your microphone is currently in use"));
                moduleStrings.Add(Load(libraryHandle, 2046, "%1 is using your microphone").Replace("%1","").Trim());
                moduleStrings.Add(Load(libraryHandle, 2047, "%1 apps are using your microphone").Replace("%1", "").Trim());
                moduleStrings.Add(Load(libraryHandle, 2052, "1 app is using your microphone"));

                // Free handle to external library
                FreeLibrary("sndvolsso.dll");
            }

            return moduleStrings;
        }

        public static string SystemPromotedNotificationArea()
        {
            // Get a new handle to the external library containing strings used for the mic in use notification
            IntPtr libraryHandle = LoadLibrary("explorer.exe");

            string moduleString = "System Promoted Notification Area";

            if (libraryHandle != IntPtr.Zero)
            {
                moduleString = Load(libraryHandle, 590, moduleString);

                // Free handle to external library
                FreeLibrary("explorer.exe");
            }

            return moduleString;
        }

        public static string UserPromotedNotificationArea()
        {
            // Get a new handle to the external library containing strings used for the mic in use notification
            IntPtr libraryHandle = LoadLibrary("explorer.exe");

            string moduleString = "User Promoted Notification Area";

            if (libraryHandle != IntPtr.Zero)
            {
                moduleString = Load(libraryHandle, 593, moduleString);

                // Free handle to external library
                FreeLibrary("explorer.exe");
            }

            return moduleString;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FreeLibrary(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);
    }
}
