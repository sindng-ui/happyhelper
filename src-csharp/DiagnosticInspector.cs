using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace HappyHelper
{
    class DiagnosticInspector
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr LoadLibraryA(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

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

        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("    HappyHelper Real-Time Gamepad Diagnostic      ");
            Console.WriteLine("==================================================");

            // 1. Load XInput
            IntPtr hXInput = LoadLibraryA("xinput1_4.dll");
            if (hXInput == IntPtr.Zero) hXInput = LoadLibraryA("xinput1_3.dll");
            if (hXInput == IntPtr.Zero)
            {
                Console.WriteLine("[Error] Cannot load XInput DLL!");
                return;
            }
            IntPtr pGetState = GetProcAddress(hXInput, "XInputGetState");
            var getState = (XInputGetStateDelegate)Marshal.GetDelegateForFunctionPointer(pGetState, typeof(XInputGetStateDelegate));

            Console.WriteLine("\n[Step 1] Inspecting Current XInput Slots (0~3):");
            for (int i = 0; i < 4; i++)
            {
                XINPUT_STATE state = new XINPUT_STATE();
                int res = getState(i, ref state);
                if (res == 0)
                {
                    Console.WriteLine(string.Format("  -> Slot #{0}: CONNECTED! (LX={1}, LY={2}, Buttons=0x{3:X4})", i, state.Gamepad.sThumbLX, state.Gamepad.sThumbLY, state.Gamepad.wButtons));
                }
                else
                {
                    Console.WriteLine(string.Format("  -> Slot #{0}: Empty (Not connected)", i));
                }
            }

            // 2. HidHide Status
            Console.WriteLine("\n[Step 2] Inspecting HidHide Status:");
            bool isHidInstalled = HidHideManager.IsDriverInstalled();
            Console.WriteLine("  -> Driver Installed: " + isHidInstalled);
            if (isHidInstalled)
            {
                bool active = HidHideManager.GetActive();
                Console.WriteLine("  -> Cloaking Active: " + active);
                var bl = HidHideManager.GetBlacklist();
                Console.WriteLine("  -> Blacklist count: " + bl.Count);
                foreach (var b in bl) Console.WriteLine("     - " + b);
                var wl = HidHideManager.GetWhitelist();
                Console.WriteLine("  -> Whitelist count: " + wl.Count);
                foreach (var w in wl) Console.WriteLine("     - " + w);
            }

            // 3. Test ViGEm Virtual Controller Creation
            Console.WriteLine("\n[Step 3] Creating Virtual ViGEm Controller...");
            try
            {
                var client = new ViGEmClient();
                var pad = client.CreateXbox360Controller();
                pad.Connect();
                Console.WriteLine("  -> ViGEm Connected successfully!");
                Thread.Sleep(200);

                Console.WriteLine("\n[Step 4] Inspecting XInput Slots AFTER ViGEm Connected:");
                for (int i = 0; i < 4; i++)
                {
                    XINPUT_STATE state = new XINPUT_STATE();
                    int res = getState(i, ref state);
                    if (res == 0)
                    {
                        Console.WriteLine(string.Format("  -> Slot #{0}: CONNECTED! (LX={1}, LY={2}, Buttons=0x{3:X4})", i, state.Gamepad.sThumbLX, state.Gamepad.sThumbLY, state.Gamepad.wButtons));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("  -> Slot #{0}: Empty", i));
                    }
                }

                // Cleanup
                pad.Disconnect();
                client.Dispose();
                Console.WriteLine("\n[Step 5] Virtual Gamepad Disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] ViGEm test failed: " + ex.Message);
            }

            Console.WriteLine("\n==================================================");
            Console.WriteLine("Diagnostic completed.");
        }
    }
}
