using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    /// <summary>
    /// Permanent HidHide Cloaking & Slot #0 Exclusive Ownership Manager.
    /// Keeps physical gamepads cloaked so that Virtual Gamepad always seizes Slot #0 regardless of connection sequence.
    /// </summary>
    public static class DeviceManager
    {
        private static bool _handlersRegistered = false;

        static DeviceManager()
        {
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

        public static bool IsSlot0Occupied()
        {
            return true;
        }

        /// <summary>
        /// App Startup: Initializes Virtual Gamepad and guarantees HidHide Cloaking is permanently active.
        /// </summary>
        public static void EnsureVirtualPadIsSlot0()
        {
            try
            {
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 starting...");

                // 1. Engage HidHide Cloaking for physical controllers permanently
                if (HidHideManager.IsDriverInstalled())
                {
                    DebugLog.Write("[DeviceManager] HidHide driver detected. Activating permanent Auto-Cloak...");
                    HidHideManager.AutoCloakConnectedGamepads();
                }

                // 2. Initialize Virtual Gamepad
                VirtualGamepad.Initialize();
                Thread.Sleep(50);

                // 3. Start passthrough engine
                GamepadPassthrough.Start();

                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 completed.");
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] EnsureVirtualPadIsSlot0 error: " + ex.Message);
            }
        }

        /// <summary>
        /// App Exit: Shuts down Virtual Gamepad cleanly while keeping physical pad cloaked for instant Slot #0 on next start.
        /// </summary>
        public static void RestorePhysicalPadToSlot0()
        {
            try
            {
                DebugLog.Write("[DeviceManager] App closing. Shutting down Virtual Gamepad...");
                GamepadPassthrough.Stop();
                VirtualGamepad.Shutdown();

                // Keep HidHide active so on next launch, Virtual Gamepad immediately gets Slot 0 without sequence issues!
                DebugLog.Write("[DeviceManager] Virtual Gamepad shut down cleanly.");
            }
            catch (Exception ex)
            {
                DebugLog.Write("[DeviceManager] RestorePhysicalPadToSlot0 error: " + ex.Message);
            }
        }

        public static void CyclePhysicalControllers()
        {
        }
    }
}
