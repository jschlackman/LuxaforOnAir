using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LuxOnAir
{
    internal class MicrophoneHelper
    {
        // Registry path used to watch for mic usage changes (no longer used to determine usage itself)
        public const string MicCapabilityKey = "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone";

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
                    using (device)
                    {
                        try
                        {
                            SessionCollection sessions = device.AudioSessionManager.Sessions;

                            for (int i = 0; i < sessions.Count; i++)
                            {
                                AudioSessionControl session = sessions[i];

                                if (session.State != AudioSessionState.AudioSessionStateActive || session.IsSystemSoundsSession)
                                {
                                    continue;
                                }

                                int pid = (int)session.GetProcessID;
                                if (pid == 0 || pid == ownPid)
                                {
                                    continue;
                                }

                                string name = GetProcessName(pid);
                                if (!micUsers.Contains(name))
                                {
                                    micUsers.Add(name);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Device may have been removed or become unavailable mid-enumeration; skip it
                        }
                    }
                }
            }

            return micUsers;
        }

        private static string GetProcessName(int pid)
        {
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    try
                    {
                        return process.MainModule.FileName;
                    }
                    catch (Exception)
                    {
                        // Access denied for elevated/system processes; fall back to the short name
                        return process.ProcessName + ".exe";
                    }
                }
            }
            catch (Exception)
            {
                return "PID " + pid;
            }
        }
    }
}
