using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxOnAir
{
    internal class MicrophoneHelper
    {
        // Registry path to mic usage data
        public const string MicCapabilityKey = "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone";

        public static List<string> GetMicUsers()
        {
            List<string> micUsers = new List<string>();

            // Open the current user mic usage key
            RegistryKey rootStore = Registry.CurrentUser.OpenSubKey(MicCapabilityKey);

            // Get any current mic users and add their names to the list
            foreach (string micUser in FindRegKeyWithValueData(rootStore))
            {
                micUsers.Add(micUser.Replace("#", "\\"));
            }

            return micUsers;
        }

        private static List<string> FindRegKeyWithValueData(RegistryKey parentKey)
        {
            List<string> micUserNames = new List<string>();
            
            // Check if this key has a last used value
            object lastUsed = parentKey.GetValue("LastUsedTimeStop");

            // If it does...
            if (lastUsed != null)
            {
                // ... check if it is zero, indicating a program currently using the mic
                ulong value = Convert.ToUInt64(lastUsed);
                if (value == 0)
                {
                    micUserNames.Add(parentKey.Name.Substring(parentKey.Name.LastIndexOf("\\") + 1));
                }
            }

            // Now recursively check all subkeys
            foreach (string childKeyName in parentKey.GetSubKeyNames())
            {
                RegistryKey childKey = parentKey.OpenSubKey(childKeyName);
                micUserNames.AddRange(FindRegKeyWithValueData(childKey));
            }

            return micUserNames;
        }
    }
}
