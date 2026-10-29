using LuxOnAir.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LuxOnAir
{
    /// <summary>
    /// Checks for running applications that may conflict with this one.
    /// </summary>
    public class ConflictCheck
    {
        /// <summary>
        /// Internal list of conflicting apps that are running.
        /// </summary>
        private List<string> runningConflicts;

        /// <summary>
        /// Potential conflicting application process executables.
        /// </summary>
        private List<string> conflictingApps;

        // Constructor
        public ConflictCheck() {
            // Find any potentially conflicting apps
            conflictingApps = Settings.Default.ConflictingProcesses.ToLower().Split(';').ToList();
            CheckProcesses();
        }

        /// <summary>
        /// Number of potentially conflicting applications that are currently running.
        /// </summary>
        public int Count()
        {
            return runningConflicts.Count;
        }

        /// <summary>
        /// Returns a long, multi-line message about which running applications may conflict with this one.
        /// </summary>
        public string LongMessage()
        {
            if (runningConflicts.Count != 0)
            {
                return "The following applications are running that may conflict with Luxafor On Air:\n\n" + String.Join("\n", runningConflicts) + "\n\nLight behavior may be unpredicatable if these apps are left running.";
            } else
            {
                return "";
            }
        }

        /// <summary>
        /// Returns a short, one-line message about which running applications may conflict with this one.
        /// </summary>
        public string ShortMessage()
        {
            if (runningConflicts.Count != 0)
            {
                return "Possible running conflicts: " + String.Join(", ", runningConflicts);
            }
            else
            {
                return "";
            }
        }

        private void CheckProcesses()
        {
            // Get all running processes on the local computer
            Process[] runningProcs = Process.GetProcesses();
            runningConflicts = new List<string>();
            
            string procName;

            foreach (Process proc in runningProcs)
            {
                procName = proc.ProcessName + ".exe";

                if (conflictingApps.Contains(procName.ToLower()))
                {
                    try
                    {
                        runningConflicts.Add(proc.MainModule.FileVersionInfo.ProductName);
                    }
                    catch
                    {
                        runningConflicts.Add(procName);
                    }
                }
            }
        }
    }
}
