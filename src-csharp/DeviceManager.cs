using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    /// <summary>
    /// Automatic Xbox Controller PnP Slot Manager
    /// Executes genuine hardware PnP restarts with Administrator privileges via pnputil.exe
    /// to swap and restore Xbox controller slots automatically without physical cable unplugging.
    /// </summary>
    public static class DeviceManager
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr LoadLibraryA(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        private delegate int XInputGetStateDelegate(int dwUserIndex, ref XINPUT_STATE pState);

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        private static XInputGetStateDelegate _xinputGetState = null;
        private static IntPtr _xinputModule = IntPtr.Zero;

        static DeviceManager()
        {
            LoadXInput();
        }

        private static void LoadXInput()
        {
            if (_xinputGetState != null) return;
            string[] candidates = { "xinput1_4.dll", "xinput1_3.dll", "xinput9_1_0.dll" };
            foreach (string dll in candidates)
            {
                try
                {
                    IntPtr handle = LoadLibraryA(dll);
                    if (handle == IntPtr.Zero) continue;
                    IntPtr proc = GetProcAddress(handle, "XInputGetState");
                    if (proc == IntPtr.Zero) { FreeLibrary(handle); continue; }
                    _xinputGetState = (XInputGetStateDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(XInputGetStateDelegate));
                    _xinputModule = handle;
                    break;
                }
                catch { }
            }
        }

        public static bool IsSlot0Occupied()
        {
            if (_xinputGetState == null) return false;
            try
            {
                XINPUT_STATE st = new XINPUT_STATE();
                return _xinputGetState(0, ref st) == 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Restarts physical Xbox controllers using pnputil.exe (requires Admin).
        /// </summary>
        public static void CyclePhysicalControllers()
        {
            try
            {
                DebugLog.Write("[DeviceManager] Cycling physical Xbox controllers with Admin privileges...");

                // Target USB and Bluetooth Xbox Controller Device Hardware IDs
                string[] deviceQueries = new string[]
                {
                    "USB\\VID_045E*",          // Microsoft Official Xbox 360 / One / Series X|S
                    "HID\\VID_045E*",          // Bluetooth Xbox Wireless Controller
                    "USB\\MS_COMP_XUSB*",      // Compatible XUSB controllers
                    "USB\\VID_0E6F*",          // PDP Xbox Controllers
                    "USB\\VID_0738*",          // Mad Catz Xbox Controllers
                    "USB\\VID_1532*",          // Razer Xbox Controllers
                    "USB\\VID_24C6*"           // PowerA Xbox Controllers
                };

                foreach (var query in deviceQueries)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "pnputil.exe",
                            Arguments = string.Format("/restart-device \"{0}\"", query),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        using (var p = Process.Start(psi))
                        {
                            if (p != null) p.WaitForExit(500);
                        }
                    }
                    catch { }
                }
                DebugLog.Write("[DeviceManager] PnP Restart completed.");
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] CyclePhysicalControllers error: " + ex.Message);
            }
        }

        /// <summary>
        /// App Startup: Ensures Virtual Gamepad seizes Slot 0 by cycling physical pad if already occupied.
        /// </summary>
        public static void EnsureVirtualPadIsSlot0()
        {
            try
            {
                bool slot0Occupied = IsSlot0Occupied();
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0: Slot0Occupied=" + slot0Occupied);

                if (slot0Occupied)
                {
                    // Physical pad already at Slot 0 -> Cycle it to free Slot 0
                    CyclePhysicalControllers();
                    Thread.Sleep(120);
                }

                // Start passthrough & initialize virtual gamepad to claim Slot 0
                GamepadPassthrough.Start();
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 done. VirtSlot=" + GamepadPassthrough.VirtualSlot);
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 error: " + ex.Message);
            }
        }

        /// <summary>
        /// App Exit: Shuts down Virtual Gamepad (freeing Slot 0) and cycles physical pad to claim Slot 0.
        /// </summary>
        public static void RestorePhysicalPadToSlot0()
        {
            try
            {
                DebugLog.Write("[DeviceManager] Restoring physical pad to Slot 0...");
                GamepadPassthrough.Stop();
                VirtualGamepad.Shutdown();
                Thread.Sleep(100);

                // Cycle physical pad so Windows re-assigns it to the now-vacant Slot 0
                CyclePhysicalControllers();
                DebugLog.Write("[DeviceManager] RestorePhysicalPadToSlot0 completed.");
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] RestorePhysicalPadToSlot0 error: " + ex.Message);
            }
        }
    }
}
