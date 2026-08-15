using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    public class TestRunner
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private static int _ltDetectedCount = 0;
        private static int _anyKeyCount = 0;
        private static readonly List<string> _eventLog = new List<string>();
        private static readonly Stopwatch _sw = new Stopwatch();
        private static IntPtr _kbHook = IntPtr.Zero;
        private static LowLevelProc _kbDelegate;
        private static XInputGetStateDelegate _xinputGetState = null;
        private static IntPtr _xinputModule = IntPtr.Zero;
        private static volatile bool _ltMonitorRunning = false;
        private static bool _lastLT = false;

        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  HappyHelper LT (Left Trigger) Verification Test");
            Console.WriteLine("==================================================");
            Console.WriteLine("skill1 = keyCode=2007 (Pad LT), interval=500ms, duration=4s");
            Console.WriteLine();
            _sw.Start();

            _kbDelegate = KbHookCallback;
            _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbDelegate, GetModuleHandle(null), 0);
            Console.WriteLine("[Hook] Keyboard hook: " + (_kbHook != IntPtr.Zero ? "OK" : "FAILED"));

            LoadXInput();
            Console.WriteLine("[XInput] Loaded: " + (_xinputGetState != null ? "OK" : "FAILED (no XInput DLL)"));

            Console.WriteLine("[VirtualGamepad] Initializing...");
            bool vgReady = false;
            try
            {
                vgReady = VirtualGamepad.Initialize();
                Console.WriteLine("[VirtualGamepad] Ready: " + vgReady);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VirtualGamepad] Exception: " + ex.GetType().Name + " -> " + ex.Message);
            }

            _ltMonitorRunning = true;
            var monThread = new Thread(MonitorLTThread);
            monThread.IsBackground = true;
            monThread.Start();

            var cfg = new AppConfig();
            cfg.slots.Add(new SkillSlotConfig { id = "skill1", name = "LT Test", enabled = true,  key = "Pad LT", keyCode = 2007, intervalMs = 500 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill2", name = "OFF",      enabled = false, key = "2",      keyCode = 3,    intervalMs = 500 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill3", name = "OFF",      enabled = false, key = "3",      keyCode = 4,    intervalMs = 500 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill4", name = "OFF",      enabled = false, key = "4",      keyCode = 5,    intervalMs = 500 });
            cfg.slots.Add(new SkillSlotConfig { id = "skillLeft",  name = "OFF", enabled = false, key = "MouseLeft",  keyCode = 1001, intervalMs = 300 });
            cfg.slots.Add(new SkillSlotConfig { id = "skillRight", name = "OFF", enabled = false, key = "MouseRight", keyCode = 1002, intervalMs = 400 });

            cfg.startKey = new KeyBindItem { key = "F5", keyCode = 63 };

            cfg.stopKey  = new KeyBindItem { key = "F6", keyCode = 64 };

            Console.WriteLine("[Config] keyCode=" + cfg.slots[0].keyCode + " enabled=" + cfg.slots[0].enabled);

            Console.WriteLine();

            var runner = new LoopRunner();
            runner.Start(cfg);
            Console.WriteLine("[LoopRunner] Started. Waiting 4 seconds for LT events...");
            Console.WriteLine();

            Thread testThread = new Thread(() =>
            {
                Thread.Sleep(4000);
                runner.Stop();
                _ltMonitorRunning = false;
                Thread.Sleep(300);
                EvaluateAndReport(vgReady);
                Environment.Exit(0);
            });
            testThread.IsBackground = true;
            testThread.Start();

            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        private static void LoadXInput()
        {
            string[] candidates = { "xinput1_4.dll", "xinput1_3.dll", "xinput1_2.dll", "xinput9_1_0.dll" };
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
                    Console.WriteLine("[XInput] Loaded: " + dll);
                    return;
                }
                catch { }
            }
        }

        private static void MonitorLTThread()
        {
            while (_ltMonitorRunning)
            {
                if (_xinputGetState != null)
                {
                    try
                    {
                        XINPUT_STATE state = new XINPUT_STATE();
                        int res = _xinputGetState(0, ref state);
                        if (res == 0)
                        {
                            bool curLT = state.Gamepad.bLeftTrigger > 50;
                            if (curLT && !_lastLT)
                            {
                                _ltDetectedCount++;
                                string logLine = string.Format("[+{0}ms] [LT DETECTED] bLeftTrigger={1} count={2}",
                                    _sw.ElapsedMilliseconds, state.Gamepad.bLeftTrigger, _ltDetectedCount);
                                Console.WriteLine(logLine);
                                lock (_eventLog) { _eventLog.Add(logLine); }
                            }
                            _lastLT = curLT;
                        }
                    }
                    catch { }
                }
                Thread.Sleep(8);
            }
        }

        private static IntPtr KbHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                KBDLLHOOKSTRUCT kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                bool isInjected = (kb.flags & 0x10) != 0;
                _anyKeyCount++;
                string logLine = string.Format("[+{0}ms] [KEY] VK=0x{1:X2} Scan=0x{2:X2} Injected={3}",
                    _sw.ElapsedMilliseconds, kb.vkCode, kb.scanCode, isInjected);
                Console.WriteLine(logLine);
                lock (_eventLog) { _eventLog.Add(logLine); }
            }
            return CallNextHookEx(_kbHook, nCode, wParam, lParam);
        }

        private static void EvaluateAndReport(bool vgReady)
        {
            if (_kbHook != IntPtr.Zero) UnhookWindowsHookEx(_kbHook);
            if (_xinputModule != IntPtr.Zero) FreeLibrary(_xinputModule);

            bool ltPass = (_ltDetectedCount >= 5);
            bool noKeyLeak = (_anyKeyCount == 0);

            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine("  TEST RESULTS");
            Console.WriteLine("==================================================");
            Console.WriteLine("VirtualGamepad Ready : " + vgReady);
            Console.WriteLine("LT Detections (4s)   : " + _ltDetectedCount + " (target >=5)");
            Console.WriteLine("Key Injections       : " + _anyKeyCount + " (target 0)");
            Console.WriteLine();

            if (!vgReady)
                Console.WriteLine("[FAIL] VirtualGamepad NOT ready. Install ViGEmBus driver!");
            else if (!ltPass)
                Console.WriteLine("[FAIL] LT not detected! VirtualGamepad.SendAction(2007) may be broken.");
            else
                Console.WriteLine("[PASS] LT fires correctly " + _ltDetectedCount + " times in 4 seconds!");

            if (!noKeyLeak)
                Console.WriteLine("[WARN] Key injections: " + _anyKeyCount + " -- LT should NOT inject keyboard events!");
            else
                Console.WriteLine("[PASS] No spurious keyboard injections.");

            var sb = new StringBuilder();
            sb.AppendLine("=== HappyHelper LT Verification ===");
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Config: skill1=Pad LT (keyCode=2007), interval=500ms, duration=4s, onlyInGame=false");
            sb.AppendLine("VirtualGamepad: " + vgReady);
            sb.AppendLine("LT Detections : " + _ltDetectedCount + " (target >=5)");
            sb.AppendLine("Key Injections: " + _anyKeyCount + " (target 0)");
            sb.AppendLine("LT Test : " + (ltPass ? "PASS" : "FAIL"));
            sb.AppendLine("Key Leak: " + (noKeyLeak ? "PASS" : "FAIL"));
            sb.AppendLine("--- Event Log ---");
            lock (_eventLog)
            {
                foreach (var l in _eventLog) sb.AppendLine(l);
            }

            string report = sb.ToString();
            Console.WriteLine(report);

            try
            {
                string docsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "docs");
                if (!Directory.Exists(docsDir)) docsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs");
                if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);
                string outPath = Path.Combine(docsDir, "test_result_lt.txt");
                File.WriteAllText(outPath, report, Encoding.UTF8);
                Console.WriteLine("Saved: " + outPath);
            }
            catch (Exception ex) { Console.WriteLine("Save failed: " + ex.Message); }
        }
    }
}
