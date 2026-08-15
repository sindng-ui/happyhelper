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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

        public static IntPtr GetDiabloWindowHandle()
        {
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
                            IntPtr hWnd = proc.MainWindowHandle;
                            if (hWnd != IntPtr.Zero) return hWnd;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return IntPtr.Zero;
        }

        public static void PostKeyToDiablo(byte vk, byte scanCode, bool down)
        {
            try
            {
                IntPtr hWnd = GetDiabloWindowHandle();
                if (hWnd == IntPtr.Zero) return;

                uint msg = down ? WM_KEYDOWN : WM_KEYUP;
                // Build lParam: repeat count (1), scan code (bits 16-23), previous key state, transition state
                uint lParam = 1 | ((uint)scanCode << 16);
                if (!down) lParam |= (1u << 30) | (1u << 31);

                PostMessage(hWnd, msg, (IntPtr)vk, (IntPtr)lParam);
            }
            catch { }
        }

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
