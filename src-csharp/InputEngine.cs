using System;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    public static class InputEngine
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint INPUT_KEYBOARD = 1;
        private const uint INPUT_MOUSE = 0;

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;

        private static readonly Random _holdRand = new Random();

        public static void SendAction(int keyCode)
        {
            try
            {
                DebugLog.Write("[InputEngine] SendAction keyCode=" + keyCode);

                if (keyCode >= 1001 && keyCode <= 1005)
                {
                    SendMouseClick(keyCode);
                }
                else if (keyCode > 0)
                {
                    SendDirectKey(keyCode);
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("InputEngine error: " + ex.Message);
            }
        }

        private static void SendDirectKey(int code)
        {
            byte scanCode = MapUiohookToScanCode((uint)code);
            byte vk = (byte)MapUiohookToVk((uint)code);

            // 1. Post to Diablo IV Window Queue directly (non-intrusive, works regardless of slot/focus)
            WindowHelper.PostKeyToDiablo(vk, scanCode, true);

            // 2. Also send hardware keybd_event with scancode
            bool isExtended = (code == 28 || code >= 59);
            uint downFlags = KEYEVENTF_SCANCODE | (isExtended ? KEYEVENTF_EXTENDEDKEY : 0);
            keybd_event(vk, scanCode, downFlags, UIntPtr.Zero);

            int holdMs = 45;
            lock (_holdRand)
            {
                holdMs = (int)Math.Round(NextGaussian(_holdRand, 45, 5));
                if (holdMs < 30) holdMs = 30;
                if (holdMs > 65) holdMs = 65;
            }
            System.Threading.Thread.Sleep(holdMs);

            uint upFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP | (isExtended ? KEYEVENTF_EXTENDEDKEY : 0);
            keybd_event(vk, scanCode, upFlags, UIntPtr.Zero);
            WindowHelper.PostKeyToDiablo(vk, scanCode, false);
        }



        private static double NextGaussian(Random rand, double mean, double stdDev)
        {
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        private static void SendMouseClick(int code)
        {
            uint downFlag = 0, upFlag = 0, data = 0;
            switch (code)
            {
                case 1001: downFlag = MOUSEEVENTF_LEFTDOWN; upFlag = MOUSEEVENTF_LEFTUP; break;
                case 1002: downFlag = MOUSEEVENTF_RIGHTDOWN; upFlag = MOUSEEVENTF_RIGHTUP; break;
                case 1003: downFlag = MOUSEEVENTF_MIDDLEDOWN; upFlag = MOUSEEVENTF_MIDDLEUP; break;
                case 1004: downFlag = MOUSEEVENTF_XDOWN; upFlag = MOUSEEVENTF_XUP; data = 1; break;
                case 1005: downFlag = MOUSEEVENTF_XDOWN; upFlag = MOUSEEVENTF_XUP; data = 2; break;
            }

            if (downFlag != 0)
            {
                WindowHelper.PostMouseToDiablo(code, true);
                mouse_event(downFlag, 0, 0, data, UIntPtr.Zero);
                
                int holdMs = 45;
                lock (_holdRand)
                {
                    holdMs = (int)Math.Round(NextGaussian(_holdRand, 45, 6));
                    if (holdMs < 30) holdMs = 30;
                    if (holdMs > 65) holdMs = 65;
                }
                System.Threading.Thread.Sleep(holdMs);
                
                mouse_event(upFlag, 0, 0, data, UIntPtr.Zero);
                WindowHelper.PostMouseToDiablo(code, false);
            }
        }

        private static byte MapUiohookToScanCode(uint code)
        {
            switch (code)
            {
                case 1: return 0x01;  // ESC
                case 2: return 0x02;  // 1
                case 3: return 0x03;  // 2
                case 4: return 0x04;  // 3
                case 5: return 0x05;  // 4
                case 6: return 0x06;  // 5
                case 7: return 0x07;  // 6
                case 8: return 0x08;  // 7
                case 9: return 0x09;  // 8
                case 10: return 0x0A; // 9
                case 11: return 0x0B; // 0
                case 16: return 0x10; // Q
                case 17: return 0x11; // W
                case 18: return 0x12; // E
                case 19: return 0x13; // R
                case 20: return 0x14; // T
                case 21: return 0x15; // Y
                case 22: return 0x16; // U
                case 23: return 0x17; // I
                case 24: return 0x18; // O
                case 25: return 0x19; // P
                case 30: return 0x1E; // A
                case 31: return 0x1F; // S
                case 32: return 0x20; // D
                case 33: return 0x21; // F
                case 34: return 0x22; // G
                case 35: return 0x23; // H
                case 36: return 0x24; // J
                case 37: return 0x25; // K
                case 38: return 0x26; // L
                case 44: return 0x2C; // Z
                case 45: return 0x2D; // X
                case 46: return 0x2E; // C
                case 47: return 0x2F; // V
                case 48: return 0x30; // B
                case 49: return 0x31; // N
                case 50: return 0x32; // M
                case 28: return 0x1C; // Enter
                case 57: return 0x39; // Space
                case 15: return 0x0F; // Tab
                case 59: return 0x3B; // F1
                case 60: return 0x3C; // F2
                case 61: return 0x3D; // F3
                case 62: return 0x3E; // F4
                case 63: return 0x3F; // F5
                case 64: return 0x40; // F6
                case 65: return 0x41; // F7
                case 66: return 0x42; // F8
                case 67: return 0x43; // F9
                case 68: return 0x44; // F10
                case 87: return 0x57; // F11
                case 88: return 0x58; // F12
                default: return (byte)code;
            }
        }

        private static int MapUiohookToVk(uint code)
        {
            switch (code)
            {
                case 1: return 0x1B;
                case 2: return 0x31;
                case 3: return 0x32;
                case 4: return 0x33;
                case 5: return 0x34;
                case 6: return 0x35;
                case 7: return 0x36;
                case 8: return 0x37;
                case 9: return 0x38;
                case 10: return 0x39;
                case 11: return 0x30;
                case 16: return 0x51;
                case 17: return 0x57;
                case 18: return 0x45;
                case 19: return 0x52;
                case 20: return 0x54;
                case 21: return 0x59;
                case 22: return 0x55;
                case 23: return 0x49;
                case 24: return 0x4F;
                case 25: return 0x50;
                case 30: return 0x41;
                case 31: return 0x53;
                case 32: return 0x44;
                case 33: return 0x46;
                case 34: return 0x47;
                case 35: return 0x48;
                case 36: return 0x4A;
                case 37: return 0x4B;
                case 38: return 0x4C;
                case 44: return 0x5A;
                case 45: return 0x58;
                case 46: return 0x43;
                case 47: return 0x56;
                case 48: return 0x42;
                case 49: return 0x4E;
                case 50: return 0x4D;
                case 28: return 0x0D;
                case 57: return 0x20;
                case 15: return 0x09;
                case 59: return 0x70;
                case 60: return 0x71;
                case 61: return 0x72;
                case 62: return 0x73;
                case 63: return 0x74;
                case 64: return 0x75;
                case 65: return 0x76;
                case 66: return 0x77;
                case 67: return 0x78;
                case 68: return 0x79;
                case 87: return 0x7A;
                case 88: return 0x7B;
                default: return (int)code;
            }
        }
    }
}
