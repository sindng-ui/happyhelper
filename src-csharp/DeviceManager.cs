using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    /// <summary>
    /// Automatic Xbox Controller PnP Slot & Cloaking Manager.
    /// Integrates HidHide Cloaking (Primary) and PnP Restarts (Fallback) to guarantee
    /// exclusive Slot #0 ownership for Virtual Gamepad and 100% seamless passthrough input fusion.
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
        private static bool _handlersRegistered = false;

        static DeviceManager()
        {
            LoadXInput();
            RegisterProcessExitHandlers();
        }

        public static void RegisterProcessExitHandlers()
        {
            if (_handlersRegistered) return;
            try
            {
                AppDomain.CurrentDomain.ProcessExit += (s, e) => RestorePhysicalPadToSlot0();
                AppDomain.CurrentDomain.UnhandledException += (s, e) => RestorePhysicalPadToSlot0();
                _handlersRegistered = true;
            }
            catch { }
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
        /// App Startup: Ensures Virtual Gamepad seizes Slot 0 via HidHide Cloaking (or PnP fallback).
        /// </summary>
        public static void EnsureVirtualPadIsSlot0()
        {
            try
            {
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 starting...");

                // 1. Primary Strategy: HidHide Kernel Cloaking (DS4Windows / reWASD standard)
                if (HidHideManager.IsDriverInstalled())
                {
                    DebugLog.Write("[DeviceManager] HidHide driver detected. Activating Auto-Cloak...");
                    HidHideManager.AutoCloakConnectedGamepads();
                    Thread.Sleep(80);
                }
                else
                {
                    // 2. Fallback Strategy: PnP Device Cycling if Slot 0 already occupied
                    bool slot0Occupied = IsSlot0Occupied();
                    DebugLog.Write("[DeviceManager] HidHide not found. Slot0Occupied=" + slot0Occupied);
                    if (slot0Occupied)
                    {
                        CyclePhysicalControllers();
                        Thread.Sleep(120);
                    }
                }

                // 3. Start passthrough & initialize virtual gamepad to claim Slot 0
                GamepadPassthrough.Start();
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 completed. VirtSlot=" + GamepadPassthrough.VirtualSlot);
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 error: " + ex.Message);
            }
        }

        /// <summary>
        /// App Exit: Shuts down Virtual Gamepad and restores physical gamepads to Windows and games.
        /// </summary>
        public static void RestorePhysicalPadToSlot0()
        {
            try
            {
                DebugLog.Write("[DeviceManager] Restoring physical pad to Slot 0...");
                GamepadPassthrough.Stop();
                VirtualGamepad.Shutdown();
                Thread.Sleep(80);

                if (HidHideManager.IsDriverInstalled())
                {
                    HidHideManager.UncloakAllGamepads();
                }
                else
                {
                    CyclePhysicalControllers();
                }

                DebugLog.Write("[DeviceManager] RestorePhysicalPadToSlot0 completed.");
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] RestorePhysicalPadToSlot0 error: " + ex.Message);
            }
        }

        /// <summary>
        /// Restarts physical Xbox controllers using pnputil.exe as fallback.
        /// </summary>
        public static void CyclePhysicalControllers()
        {
            try
            {
                DebugLog.Write("[DeviceManager] Cycling physical Xbox controllers with pnputil...");
                string[] deviceQueries = new string[]
                {
                    "USB\\VID_045E*",
                    "HID\\VID_045E*",
                    "USB\\MS_COMP_XUSB*",
                    "USB\\VID_0E6F*",
                    "USB\\VID_0738*",
                    "USB\\VID_1532*",
                    "USB\\VID_24C6*"
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
    }
}
