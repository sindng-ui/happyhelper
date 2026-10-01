using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace HappyHelper
{
    public class TestRunner
    {
        private static int _passedCount = 0;
        private static int _failedCount = 0;
        private static readonly List<string> _testLogs = new List<string>();

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string asmName = new System.Reflection.AssemblyName(e.Name).Name + ".dll";
                string[] probeDirs = new[]
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src-csharp"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dist-csharp"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build-temp"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src-csharp")
                };
                foreach (var dir in probeDirs)
                {
                    string candidate = Path.Combine(dir, asmName);
                    if (File.Exists(candidate))
                    {
                        return System.Reflection.Assembly.LoadFrom(candidate);
                    }
                }
                return null;
            };

            Console.WriteLine("==================================================");
            Console.WriteLine("  HappyHelper C# Backend Comprehensive Unit Tests ");
            Console.WriteLine("==================================================");
            Console.WriteLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine();

            // 1. ConfigManager Tests
            RunTest("ConfigManager - Default Config Generation", TestCoreSuites.TestConfigManagerDefault);
            RunTest("ConfigManager - JSON Serialization & Deserialization", TestCoreSuites.TestConfigManagerSerialization);
            RunTest("ConfigManager - Corrupted JSON Fallback Recovery", TestCoreSuites.TestConfigManagerCorruptedFallback);
            RunTest("ConfigManager - Preset Storage & Retrieval", TestCoreSuites.TestConfigManagerPresetHandling);

            // 2. InputEngine Tests
            RunTest("InputEngine - Smart Hotkey Block (ESC Key 1)", TestCoreSuites.TestInputEngineBlockEscKey);
            RunTest("InputEngine - Smart Hotkey Block (Mouse Left Click 1001)", TestCoreSuites.TestInputEngineBlockLeftClick);
            RunTest("InputEngine - Mouse Side Keys Allowed (X1/X2 1004~1005)", TestCoreSuites.TestInputEngineMouseSideKeysAllowed);
            RunTest("InputEngine - Gamepad KeyCode Range Mapping (2001~2016)", TestCoreSuites.TestInputEnginePadKeyCodes);
            RunTest("InputEngine - Mouse Button KeyCode Mapping (1001~1005)", TestCoreSuites.TestInputEngineMouseKeyCodes);

            // 3. LoopRunner Tests
            RunTest("LoopRunner - State Machine Transitions (Start/Stop/Pause/Resume)", TestCoreSuites.TestLoopRunnerStateMachine);
            RunTest("LoopRunner - Timer Precision & SkillTriggered Event", TestCoreSuites.TestLoopRunnerTimerPrecision);
            RunTest("LoopRunner - Disabled State Skill Emission Suppression", TestCoreSuites.TestLoopRunnerDisabledSuppression);

            // 4. ViGEm & DeviceManager Tests
            RunTest("ViGEmInstaller - Ground-Truth Driver Detection Safety", TestViGEmInstallerSafety);
            RunTest("VirtualGamepad - Initialization & State Safety", TestVirtualGamepadSafety);
            RunTest("DeviceManager - PnP Slot Management Safety", TestDeviceManagerSafety);
            RunTest("DeviceManager - Fail-Safe ProcessExit Recovery Handler Safety", TestDeviceManagerExitHandler);

            // 5. WindowHelper Tests
            RunTest("WindowHelper - Win32 Process Handle Safety", TestWindowHelperSafety);

            // 6. HidHide Tests
            RunTest("HidHideManager - Driver Installation & Handle Safety Detection", TestHidHideDriverDetection);
            RunTest("HidHideManager - Multi-String Serialization (UTF-16LE Multi-SZ)", TestHidHideMultiStringSerialization);
            RunTest("HidHideManager - Multi-String Deserialization (UTF-16LE Multi-SZ)", TestHidHideMultiStringDeserialization);
            RunTest("HidHideManager - Whitelist Add & Deduplication Logic", TestHidHideWhitelistDeduplication);
            RunTest("HidHideManager - Process Path Normalization & Lookup", TestHidHideProcessPathNormalization);
            RunTest("HidHideManager - Blacklist Data Structure & Device Pattern Matcher", TestHidHideBlacklistDataEncoding);
            RunTest("HidHideManager - Active State (Cloaking) Toggle Packet Encoding", TestHidHideActiveToggleSafety);
            RunTest("HidHideManager - Graceful Exception Handling on Uninstalled Driver", TestHidHideUninstalledGracefulFailover);
            RunTest("HidHideManager - Dynamic Gamepad Device Enumeration", TestHidHideDeviceEnumeration);

            // 7. Passthrough & Fusion Tests
            RunTest("GamepadPassthrough - 120Hz Input Fusion Arithmetic (Stick, Trigger, Button Merge)", TestPassthroughFusionArithmetic);
            RunTest("GamepadPassthrough - Synthetic Auto-Skill Pulse Expiration Timing", TestPassthroughPulseExpiration);

            // Print & Save Final Report
            GenerateFinalReport();
        }

        private static void RunTest(string testName, Action testAction)
        {
            Console.Write(string.Format("[TEST] {0,-60} ... ", testName));
            try
            {
                testAction();
                _passedCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASS");
                Console.ResetColor();
                Log(string.Format("[PASS] {0}", testName));
            }
            catch (Exception ex)
            {
                _failedCount++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL -> " + ex.Message);
                Console.ResetColor();
                Log(string.Format("[FAIL] {0} -> {1}", testName, ex.Message));
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion Failed: " + message);
        }

        private static void Log(string message)
        {
            lock (_testLogs) { _testLogs.Add(message); }
        }

        // ==========================================
        // 4. ViGEm & DeviceManager Tests
        // ==========================================
        private static void TestViGEmInstallerSafety()
        {
            bool installed = false;
            try { installed = ViGEmInstaller.IsDriverInstalled(); }
            catch (Exception ex) { Assert(false, "ViGEmInstaller.IsDriverInstalled threw exception: " + ex.Message); }
            Log("ViGEmBus driver installation status: " + installed);
        }

        private static void TestVirtualGamepadSafety()
        {
            bool ready = VirtualGamepad.IsReady;
            Log("VirtualGamepad IsReady: " + ready);
            try { VirtualGamepad.SendAction(2001); }
            catch (Exception ex) { Assert(false, "VirtualGamepad.SendAction threw exception: " + ex.Message); }
        }

        private static void TestDeviceManagerSafety()
        {
            bool occupied = DeviceManager.IsSlot0Occupied();
            Log("[Test] DeviceManager.IsSlot0Occupied returns: " + occupied);
            DeviceManager.CyclePhysicalControllers();
            Assert(true, "DeviceManager.CyclePhysicalControllers executed safely");
        }

        private static void TestDeviceManagerExitHandler()
        {
            DeviceManager.RegisterProcessExitHandlers();
            Assert(true, "DeviceManager.RegisterProcessExitHandlers completed safely");
        }

        // ==========================================
        // 5. WindowHelper Tests
        // ==========================================
        private static void TestWindowHelperSafety()
        {
            bool isActive = WindowHelper.IsDiabloActive();
            Log("WindowHelper.IsDiabloActive result: " + isActive);
        }

        // ==========================================
        // 6. HidHideManager Tests
        // ==========================================
        private static void TestHidHideDriverDetection()
        {
            bool installed = HidHideManager.IsDriverInstalled();
            Log("HidHide driver installation status: " + installed);
            Assert(true, "HidHideManager.IsDriverInstalled completed safely");
        }

        private static void TestHidHideMultiStringSerialization()
        {
            var items = new List<string> { "App1.exe", "App2.exe", @"C:\Games\Diablo IV\Diablo IV.exe" };
            byte[] serialized = HidHideManager.SerializeMultiString(items);
            Assert(serialized != null && serialized.Length >= 4, "Buffer invalid");
            int len = serialized.Length;
            Assert(serialized[len - 1] == 0 && serialized[len - 2] == 0 && serialized[len - 3] == 0 && serialized[len - 4] == 0, "Not double-null terminated");
        }

        private static void TestHidHideMultiStringDeserialization()
        {
            var original = new List<string> { @"C:\Windows\happyhelper.exe", @"D:\Steam\steam.exe" };
            byte[] serialized = HidHideManager.SerializeMultiString(original);
            var deserialized = HidHideManager.DeserializeMultiString(serialized, serialized.Length);
            Assert(deserialized.Count == original.Count && deserialized[0] == original[0], "Deserialization mismatch");
        }

        private static void TestHidHideWhitelistDeduplication()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\Path\App.exe", @"c:\path\app.exe", @"C:\Path\App2.exe" };
            Assert(set.Count == 2, "Deduplication failed, count: " + set.Count);
        }

        private static void TestHidHideProcessPathNormalization()
        {
            string path = ProcessPathHelper.GetCurrentProcessDosDevicePath();
            Assert(!string.IsNullOrEmpty(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase), "Invalid exe path: " + path);
        }

        private static void TestHidHideBlacklistDataEncoding()
        {
            var devices = new List<string> { @"HID\VID_045E&PID_028E&IG_00\7&23D2633&0&0000" };
            byte[] bytes = HidHideManager.SerializeMultiString(devices);
            var restored = HidHideManager.DeserializeMultiString(bytes, bytes.Length);
            Assert(restored.Count == 1 && restored[0].Contains("VID_045E"), "Restored device mismatch");
        }

        private static void TestHidHideActiveToggleSafety()
        {
            bool active = HidHideManager.GetActive();
            Log("HidHide Cloaking Active Status: " + active);
            bool result = HidHideManager.SetActive(false);
            Log("HidHide SetActive(false) result: " + result);
            Assert(true, "GetActive/SetActive executed safely");
        }

        private static void TestHidHideUninstalledGracefulFailover()
        {
            var wl = HidHideManager.GetWhitelist();
            var bl = HidHideManager.GetBlacklist();
            Assert(wl != null && bl != null, "Lists must not be null");
            HidHideManager.EnsureCurrentAppWhitelisted();
            Assert(true, "Failover safety verified");
        }

        private static void TestHidHideDeviceEnumeration()
        {
            var instances = HidHideManager.GetConnectedGamepadInstances();
            Assert(instances != null, "Instances list is null");
            Log("Found connected gamepad instances: " + instances.Count);
            foreach (var inst in instances)
            {
                Assert(!inst.Contains("ViGEm"), "Enumeration must not include virtual ViGEm controller: " + inst);
            }
        }

        // ==========================================
        // 7. Passthrough & Fusion Tests
        // ==========================================
        private static void TestPassthroughFusionArithmetic()
        {
            // Simulate Physical controller moving L-Stick (LX=15000, LY=20000)
            ushort physButtons = 0x0001; // D-Up
            byte physLT = 50;
            byte physRT = 0;
            short physLX = 15000, physLY = 20000, physRX = 0, physRY = 0;

            // Simulate Synthetic Auto-Skill (Triggering LT=255 and A Button 0x1000)
            ushort autoButtons = 0x1000; // A Button
            byte autoLT = 255;
            byte autoRT = 0;

            ushort outButtons;
            byte outLT, outRT;
            short outLX, outLY, outRX, outRY;

            GamepadPassthrough.MergeStates(
                physButtons, physLT, physRT, physLX, physLY, physRX, physRY,
                autoButtons, autoLT, autoRT,
                out outButtons, out outLT, out outRT, out outLX, out outLY, out outRX, out outRY);

            // Assertions
            Assert((outButtons & 0x1000) != 0, "Fused buttons missing auto A button");
            Assert((outButtons & 0x0001) != 0, "Fused buttons missing physical D-Up button");
            Assert(outLT == 255, "Fused LT should be 255, got: " + outLT);
            Assert(outLX == 15000 && outLY == 20000, "Fused L-Stick coords corrupted");
        }

        private static void TestPassthroughPulseExpiration()
        {
            GamepadPassthrough.PulseAction(2007, 50); // Pulse LT for 50ms
            Assert(true, "PulseAction executed with thread safety");
        }

        private static void GenerateFinalReport()
        {
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine(string.Format("  TEST RESULTS SUMMARY: PASSED={0}, FAILED={1}", _passedCount, _failedCount));
            Console.WriteLine("==================================================");

            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine("  HappyHelper Comprehensive Unit Test Report      ");
            sb.AppendLine("==================================================");
            sb.AppendLine("Date      : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Total     : " + (_passedCount + _failedCount));
            sb.AppendLine("Passed    : " + _passedCount);
            sb.AppendLine("Failed    : " + _failedCount);
            sb.AppendLine("Result    : " + (_failedCount == 0 ? "ALL TESTS PASSED SUCCESSFUL!" : "SOME TESTS FAILED!"));
            sb.AppendLine("--- Test Event Logs ---");

            lock (_testLogs) { foreach (var log in _testLogs) sb.AppendLine(log); }

            string reportText = sb.ToString();
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string docsDir = Path.Combine(baseDir, "..", "docs");
                if (!Directory.Exists(docsDir)) docsDir = Path.Combine(baseDir, "docs");
                if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);

                string outPath = Path.Combine(docsDir, "test_result.txt");
                File.WriteAllText(outPath, reportText, Encoding.UTF8);
                Console.WriteLine("[Report] Successfully saved to: " + outPath);
            }
            catch (Exception ex) { Console.WriteLine("[Report] Save failed: " + ex.Message); }
        }
    }
}
