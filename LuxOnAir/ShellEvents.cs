using System.Collections.Generic;
using System.Windows.Automation;

namespace LuxOnAir
{
    internal static class ShellEvents
    {
        /// <summary>
        /// Automation object for Shell_TrayWnd
        /// </summary>
        private static AutomationElement shellTray;
        /// <summary>
        /// Automation object for the User Promoted Notification Area
        /// </summary>
        private static AutomationElement userArea;

        /// <summary>
        /// Enumerate the notification icons in a UI Automation object
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AutomationElement> EnumNotificationIcons()
        {
            if (userArea != null)
            {
                foreach (AutomationElement button in userArea.EnumChildButtons())
                {
                    yield return button;
                }
                foreach (AutomationElement button in userArea.GetTopLevelElement().Find("System Promoted Notification Area").EnumChildButtons())
                {
                    yield return button;
                }
            }
        }

        /// <summary>
        /// Event handler object for subscribing to changes to the notification icons
        /// </summary>
        private static StructureChangedEventHandler trayEventHandler;

        /// <summary>
        /// Dispose of references and hooks to the shell tray
        /// </summary>
        public static void DisposeTrayHooks()
        {
            if (trayEventHandler != null)
            {
                Automation.RemoveStructureChangedEventHandler(shellTray, trayEventHandler);
            }
        }

        /// <summary>
        /// Initialize references and hooks to elements of the tray window (yes, the tray, not the just the notification area which is PART of the shell tray).
        /// </summary>
        public static void InitTrayHooks(StructureChangedEventHandler eventHandler)
        {
            userArea = AutomationElement.RootElement.Find("User Promoted Notification Area");
            shellTray = userArea.GetTopLevelElement();

            Automation.AddStructureChangedEventHandler(shellTray, TreeScope.Descendants, trayEventHandler = eventHandler);
        }
    }
}
