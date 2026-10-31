using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LuxOnAir
{
    internal class MicrophoneHelper
    {
        /// <summary>
        /// Get the names of all processes with an active audio capture session.
        /// A session is active while the app has the microphone open, regardless of whether it is muted in the app.
        /// </summary>
        public static List<string> GetMicUsers()
        {
            List<string> micUsers = new List<string>();
            int ownPid = Process.GetCurrentProcess().Id;

            using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
            {
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    SessionCollection sessions = device.AudioSessionManager.Sessions;

                    for (int i = 0; i < sessions.Count; i++)
                    {
                        AudioSessionControl session = sessions[i];

                        if (session.State == AudioSessionState.AudioSessionStateActive)
                        {
                            // Check session does not belong to current process or is a system sound session
                            int pid = (int)session.GetProcessID;

                            if (pid != 0 && pid != ownPid && !session.IsSystemSoundsSession)
                            {
                                string name = GetProcessName(pid);
                                if (!micUsers.Contains(name))
                                {
                                    micUsers.Add(name);
                                }
                            }
                        }
                    }
                }
            }

            return micUsers;
        }

        private static string GetProcessName(int pid)
        {
            string procName = "";
            bool useFallbacks = false;
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    try
                    {
                        // Tryt to get the product name from the file version information
                        procName = process.MainModule.FileVersionInfo.ProductName ?? "";

                        // Use fallbacks if we didn't get a useful name
                        if (procName == "")
                        {
                            useFallbacks = true;
                        }
                    }
                    catch (Exception)
                    {
                        // Access may be denied for elevated/system processes, try fallbacks
                        useFallbacks = true;
                    }

                    if (useFallbacks)
                    {
                        procName = process.MainWindowTitle ?? "";
                        if (procName == "")
                        {
                            procName = process.ProcessName + ".exe";
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If all else fails, use the process ID
                procName = "process ID " + pid;
            }

            return procName;
        }
    }
}
