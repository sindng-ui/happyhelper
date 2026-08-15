using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace HappyHelper
{
    /// <summary>
    /// Gamepad Passthrough & Merger Engine
    /// Captures physical gamepad inputs via XInput at 120Hz and seamlessly merges
    /// automated skill triggers (e.g. LT, RT, Buttons) into a single virtual Xbox 360 controller.
    /// </summary>
    public static class GamepadPassthrough
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

        private static Thread _workerThread = null;
        private static volatile bool _running = false;
        private static readonly object _stateLock = new object();

        // Track verified physical slots and virtual slot
        private static readonly List<int> _physicalSlots = new List<int>();
        private static int _virtualSlot = -1;

        // Synthetic auto-skill overlay state
        private static volatile byte _autoLeftTrigger = 0;
        private static volatile byte _autoRightTrigger = 0;
        private static volatile ushort _btnPulseMask = 0;

        // Timers for auto-skill pulses
        private static long _ltPulseExpireTick = 0;
        private static long _rtPulseExpireTick = 0;
        private static long _btnPulseExpireTick = 0;
        private static volatile bool _forceSubmit = false;

        // Last submitted state for Dirty Checking (Zero-copy optimization)
        private static ushort _lastButtons = 0xFFFF;
        private static byte _lastLT = 255;
        private static byte _lastRT = 255;
        private static short _lastLX = 0;
        private static short _lastLY = 0;
        private static short _lastRX = 0;
        private static short _lastRY = 0;

        public static bool IsRunning
        {
            get { return _running; }
        }

        public static int VirtualSlot
        {
            get { return VirtualGamepad.UserIndex; }
        }

        public static void Start()
        {
            if (_running) return;

            LoadXInput();

            // Initialize Virtual Gamepad
            VirtualGamepad.Initialize();

            _running = true;
            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            _workerThread.Start();
            DebugLog.Write("[GamepadPassthrough] Started at 120Hz. (VirtSlot=" + VirtualGamepad.UserIndex + ")");
        }

        public static void Stop()
        {
            _running = false;
            if (_workerThread != null)
            {
                try { _workerThread.Join(200); } catch { }
                _workerThread = null;
            }

            if (_xinputModule != IntPtr.Zero)
            {
                FreeLibrary(_xinputModule);
                _xinputModule = IntPtr.Zero;
                _xinputGetState = null;
            }
            DebugLog.Write("[GamepadPassthrough] Stopped.");
        }

        private static readonly Random _pulseRand = new Random();

        private static double NextGaussian(Random rand, double mean, double stdDev)
        {
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        /// <summary>
        /// Trigger a synthetic gamepad button/trigger pulse that seamlessly merges with physical inputs.
        /// </summary>
        public static void PulseAction(int padCode, int durationMs = 140)
        {
            int randomizedDuration = durationMs;
            lock (_pulseRand)
            {
                randomizedDuration = (int)Math.Round(NextGaussian(_pulseRand, durationMs, 10));
                if (randomizedDuration < 100) randomizedDuration = 100;
                if (randomizedDuration > 190) randomizedDuration = 190;
            }
            long expire = DateTime.UtcNow.Ticks + (randomizedDuration * TimeSpan.TicksPerMillisecond);

            lock (_stateLock)
            {
                switch (padCode)
                {
                    case 2007: // LT (Left Trigger)
                        _autoLeftTrigger = 255;
                        _ltPulseExpireTick = expire;
                        _forceSubmit = true;
                        DebugLog.Write("[GamepadPassthrough] Pulse LT (Left Trigger) ON");
                        break;
                    case 2008: // RT (Right Trigger)
                        _autoRightTrigger = 255;
                        _rtPulseExpireTick = expire;
                        _forceSubmit = true;
                        DebugLog.Write("[GamepadPassthrough] Pulse RT (Right Trigger) ON");
                        break;
                    case 2001: // A
                        SetButtonPulse(0x1000, expire);
                        break;
                    case 2002: // B
                        SetButtonPulse(0x2000, expire);
                        break;
                    case 2003: // X
                        SetButtonPulse(0x4000, expire);
                        break;
                    case 2004: // Y
                        SetButtonPulse(0x8000, expire);
                        break;
                    case 2005: // LB
                        SetButtonPulse(0x0100, expire);
                        break;
                    case 2006: // RB
                        SetButtonPulse(0x0200, expire);
                        break;
                    case 2009: // D-Pad Up
                        SetButtonPulse(0x0001, expire);
                        break;
                    case 2010: // D-Pad Down
                        SetButtonPulse(0x0002, expire);
                        break;
                    case 2011: // D-Pad Left
                        SetButtonPulse(0x0004, expire);
                        break;
                    case 2012: // D-Pad Right
                        SetButtonPulse(0x0008, expire);
                        break;
                    case 2013: // LS
                        SetButtonPulse(0x0040, expire);
                        break;
                    case 2014: // RS
                        SetButtonPulse(0x0080, expire);
                        break;
                    case 2015: // Back
                        SetButtonPulse(0x0020, expire);
                        break;
                    case 2016: // Start
                        SetButtonPulse(0x0010, expire);
                        break;
                }
            }
        }

        private static void SetButtonPulse(ushort mask, long expire)
        {
            _btnPulseMask |= mask;
            _btnPulseExpireTick = Math.Max(_btnPulseExpireTick, expire);
            _forceSubmit = true;
            DebugLog.Write(string.Format("[GamepadPassthrough] Pulse Button Mask 0x{0:X4} ON", mask));
        }

        private static void WorkerLoop()
        {
            while (_running)
            {
                try
                {
                    long now = DateTime.UtcNow.Ticks;

                    // 1. Check auto-skill expirations
                    lock (_stateLock)
                    {
                        if (_autoLeftTrigger > 0 && now >= _ltPulseExpireTick)
                        {
                            _autoLeftTrigger = 0;
                            _forceSubmit = true;
                            DebugLog.Write("[GamepadPassthrough] Pulse LT Expired (OFF)");
                        }
                        if (_autoRightTrigger > 0 && now >= _rtPulseExpireTick)
                        {
                            _autoRightTrigger = 0;
                            _forceSubmit = true;
                            DebugLog.Write("[GamepadPassthrough] Pulse RT Expired (OFF)");
                        }
                        if (_btnPulseMask > 0 && now >= _btnPulseExpireTick)
                        {
                            _btnPulseMask = 0;
                            _forceSubmit = true;
                            DebugLog.Write("[GamepadPassthrough] Pulse Buttons Expired (OFF)");
                        }
                    }

                    // 2. Poll physical gamepads dynamically (any connected slot that is NOT our virtual gamepad)
                    XINPUT_GAMEPAD phys = new XINPUT_GAMEPAD();
                    int virtSlot = VirtualGamepad.UserIndex;

                    if (_xinputGetState != null)
                    {
                        for (int slot = 0; slot < 4; slot++)
                        {
                            // Skip our own virtual controller slot to prevent loopback
                            if (virtSlot >= 0 && slot == virtSlot) continue;

                            XINPUT_STATE state = new XINPUT_STATE();
                            if (_xinputGetState(slot, ref state) == 0)
                            {
                                phys = state.Gamepad;
                                break;
                            }
                        }
                    }



                    // 3. Merge physical inputs with synthetic auto-skill inputs
                    byte mergedLT = (byte)Math.Max((int)phys.bLeftTrigger, (int)_autoLeftTrigger);
                    byte mergedRT = (byte)Math.Max((int)phys.bRightTrigger, (int)_autoRightTrigger);
                    ushort mergedButtons = (ushort)(phys.wButtons | _btnPulseMask);
                    short mergedLX = phys.sThumbLX;
                    short mergedLY = phys.sThumbLY;
                    short mergedRX = phys.sThumbRX;
                    short mergedRY = phys.sThumbRY;

                    // 4. Dirty Checking: submit if state changed or forceSubmit requested
                    bool isDirty = _forceSubmit ||
                                    (mergedButtons != _lastButtons) ||
                                    (mergedLT != _lastLT) ||
                                    (mergedRT != _lastRT) ||
                                    Math.Abs(mergedLX - _lastLX) > 100 ||
                                    Math.Abs(mergedLY - _lastLY) > 100 ||
                                    Math.Abs(mergedRX - _lastRX) > 100 ||
                                    Math.Abs(mergedRY - _lastRY) > 100;

                    if (isDirty)
                    {
                        _forceSubmit = false;
                        _lastButtons = mergedButtons;
                        _lastLT = mergedLT;
                        _lastRT = mergedRT;
                        _lastLX = mergedLX;
                        _lastLY = mergedLY;
                        _lastRX = mergedRX;
                        _lastRY = mergedRY;

                        VirtualGamepad.SubmitFullState(mergedButtons, mergedLT, mergedRT, mergedLX, mergedLY, mergedRX, mergedRY);
                    }

                }
                catch { }

                // ~120Hz polling interval (8ms)
                Thread.Sleep(8);
            }
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
                    DebugLog.Write("[GamepadPassthrough] Loaded XInput from " + dll);
                    return;
                }
                catch { }
            }
        }
    }
}
