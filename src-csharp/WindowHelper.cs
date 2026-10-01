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

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

        private static IntPtr _cachedDiabloHwnd = IntPtr.Zero;
        private static long _lastHwndCheckTick = 0;

        public static IntPtr GetDiabloWindowHandle()
        {
            long now = DateTime.UtcNow.Ticks;
            if (_cachedDiabloHwnd != IntPtr.Zero && (now - _lastHwndCheckTick < TimeSpan.TicksPerSecond * 2))
            {
                return _cachedDiabloHwnd;
            }

            _lastHwndCheckTick = now;
            IntPtr found = IntPtr.Zero;

            // 1. Scan via EnumWindows for exact window title and visibility
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (!string.IsNullOrEmpty(title) && title.IndexOf("Diablo", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = hWnd;
                    return false; // Stop enumeration
                }

                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                try
                {
                    var proc = Process.GetProcessById((int)pid);
                    if (proc.ProcessName.IndexOf("Diablo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = hWnd;
                        return false;
                    }
                }
                catch { }

                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
            {
                _cachedDiabloHwnd = found;
                return found;
            }

            // 2. Fallback to Process.MainWindowHandle
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (var proc in procs)
                {
                    if (proc.ProcessName.IndexOf("Diablo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        IntPtr h = proc.MainWindowHandle;
                        if (h != IntPtr.Zero)
                        {
                            _cachedDiabloHwnd = h;
                            return h;
                        }
                    }
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
                uint lParam = 1 | ((uint)scanCode << 16);
                if (!down) lParam |= (1u << 30) | (1u << 31);

                PostMessage(hWnd, msg, (IntPtr)vk, (IntPtr)lParam);
            }
            catch { }
        }

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        private const uint WM_XBUTTONDOWN = 0x020B;
        private const uint WM_XBUTTONUP = 0x020C;

        public static void PostMouseToDiablo(int mouseCode, bool down)
        {
            try
            {
                IntPtr hWnd = GetDiabloWindowHandle();
                if (hWnd == IntPtr.Zero) return;

                uint msg = 0;
                IntPtr wParam = IntPtr.Zero;

                switch (mouseCode)
                {
                    case 1001: msg = down ? WM_LBUTTONDOWN : WM_LBUTTONUP; wParam = down ? (IntPtr)0x0001 : IntPtr.Zero; break;
                    case 1002: msg = down ? WM_RBUTTONDOWN : WM_RBUTTONUP; wParam = down ? (IntPtr)0x0002 : IntPtr.Zero; break;
                    case 1003: msg = down ? WM_MBUTTONDOWN : WM_MBUTTONUP; wParam = down ? (IntPtr)0x0010 : IntPtr.Zero; break;
                    case 1004: msg = down ? WM_XBUTTONDOWN : WM_XBUTTONUP; wParam = (IntPtr)(1 << 16 | (down ? 0x0020 : 0)); break;
                    case 1005: msg = down ? WM_XBUTTONDOWN : WM_XBUTTONUP; wParam = (IntPtr)(2 << 16 | (down ? 0x0040 : 0)); break;
                }

                if (msg != 0)
                {
                    PostMessage(hWnd, msg, wParam, IntPtr.Zero);
                }
            }
            catch { }
        }

        public static bool IsDiabloActive()
        {
            return GetDiabloWindowHandle() != IntPtr.Zero;
        }
    }
}
