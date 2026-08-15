using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    public static class WindowHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static bool IsDiabloActive()
        {
            // Check if Diablo IV process is running (not foreground check)
            // Foreground check fails when alwaysOnTop = true (app covers game window)
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (var proc in procs)
                {
                    try
                    {
                        string name = proc.ProcessName;
                        if (name.IndexOf("Diablo", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Also check window title if available
                            try
                            {
                                string title = proc.MainWindowTitle;
                                if (!string.IsNullOrEmpty(title))
                                    return true;
                                // Process exists but might be minimized - still return true
                                if (name.IndexOf("Diablo", StringComparison.OrdinalIgnoreCase) >= 0)
                                    return true;
                            }
                            catch { return true; }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }
    }
}
