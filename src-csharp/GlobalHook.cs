using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    public class GlobalHook
    {
        // Keyboard & Mouse Win32 Hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);



        // Keyboard Hook Struct
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // Mouse Hook Struct
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_NCXBUTTONDOWN = 0x00AB;

        private LowLevelKeyboardProc _kbProc;
        private LowLevelMouseProc _mouseProc;
        private IntPtr _kbHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        // Diagnostic props (C# 5 compatible, keyboard/mouse only)
        public bool IsXInputLoaded { get { return false; } }
        public bool IsControllerConnected { get { return false; } }

        public event Action<int, bool> KeyPressed;

        public void Start()
        {
            _kbProc = HookCallbackKB;
            _mouseProc = HookCallbackMouse;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _kbHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, GetModuleHandle(curModule.ModuleName), 0);
                _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void Stop()
        {
            if (_kbHookId != IntPtr.Zero) UnhookWindowsHookEx(_kbHookId);
            if (_mouseHookId != IntPtr.Zero) UnhookWindowsHookEx(_mouseHookId);
        }



        private IntPtr HookCallbackKB(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                KBDLLHOOKSTRUCT kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                // Ignore simulated/injected inputs from self (LLKHF_INJECTED)
                if ((kb.flags & 0x01) == 0 && (kb.flags & 0x10) == 0)
                {
                    int code = MapVkToCode(kb.vkCode);
                    var handler = KeyPressed;
                    if (handler != null) handler(code, false);
                }
            }
            return CallNextHookEx(_kbHookId, nCode, wParam, lParam);
        }

        private IntPtr HookCallbackMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT mhs = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                // Ignore injected mouse clicks from self (LLMHF_INJECTED)
                if ((mhs.flags & 0x01) == 0)
                {
                    int msg = (int)wParam;
                    int mouseCode = 0;
                    if (msg == WM_LBUTTONDOWN) mouseCode = 1001;
                    else if (msg == WM_RBUTTONDOWN) mouseCode = 1002;
                    else if (msg == WM_MBUTTONDOWN) mouseCode = 1003;
                    else if (msg == WM_XBUTTONDOWN || msg == WM_NCXBUTTONDOWN)
                    {
                        int xbtn = (int)((mhs.mouseData >> 16) & 0xFFFF);
                        if (xbtn == 1) mouseCode = 1004;
                        else if (xbtn == 2) mouseCode = 1005;
                    }
                    if (mouseCode != 0)
                    {
                        var handler = KeyPressed;
                        if (handler != null) handler(mouseCode, true);
                    }
                }
            }
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private int MapVkToCode(uint vk)
        {
            switch (vk)
            {
                case 0x1B: return 1;  case 0x31: return 2;  case 0x32: return 3;
                case 0x33: return 4;  case 0x34: return 5;  case 0x35: return 6;
                case 0x36: return 7;  case 0x37: return 8;  case 0x38: return 9;
                case 0x39: return 10; case 0x30: return 11;
                case 0x51: return 16; case 0x57: return 17; case 0x45: return 18;
                case 0x52: return 19; case 0x54: return 20; case 0x59: return 21;
                case 0x55: return 22; case 0x49: return 23; case 0x4F: return 24;
                case 0x50: return 25;
                case 0x41: return 30; case 0x53: return 31; case 0x44: return 32;
                case 0x46: return 33; case 0x47: return 34; case 0x48: return 35;
                case 0x4A: return 36; case 0x4B: return 37; case 0x4C: return 38;
                case 0x5A: return 44; case 0x58: return 45; case 0x43: return 46;
                case 0x56: return 47; case 0x42: return 48; case 0x4E: return 49;
                case 0x4D: return 50;
                case 0x0D: return 28; case 0x20: return 57; case 0x09: return 15;
                case 0x70: return 59; case 0x71: return 60; case 0x72: return 61;
                case 0x73: return 62; case 0x74: return 63; case 0x75: return 64;
                case 0x76: return 65; case 0x77: return 66; case 0x78: return 67;
                case 0x79: return 68; case 0x7A: return 87; case 0x7B: return 88;
                // NumPad
                case 0x60: return 3000; case 0x61: return 3001; case 0x62: return 3002;
                case 0x63: return 3003; case 0x64: return 3004; case 0x65: return 3005;
                case 0x66: return 3006; case 0x67: return 3007; case 0x68: return 3008;
                case 0x69: return 3009; case 0x6E: return 3010; case 0x6B: return 3011;
                case 0x6D: return 3012; case 0x6A: return 3013; case 0x6F: return 3014;
                case 0x90: return 3015;
                default: return (int)vk;
            }
        }
    }
}
