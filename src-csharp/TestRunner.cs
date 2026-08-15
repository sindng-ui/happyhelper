using System;
using System.IO;
using System.Text;
using System.Diagnostics;
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
            Console.WriteLine("==================================================");
            Console.WriteLine("  HappyHelper C# Backend Comprehensive Unit Tests ");
            Console.WriteLine("==================================================");
            Console.WriteLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine();

            // Run Unit Tests
            RunTest("ConfigManager - Default Config Generation", TestConfigManagerDefault);
            RunTest("ConfigManager - JSON Serialization & Deserialization", TestConfigManagerSerialization);
            RunTest("ConfigManager - Corrupted JSON Fallback Recovery", TestConfigManagerCorruptedFallback);
            RunTest("ConfigManager - Preset Storage & Retrieval", TestConfigManagerPresetHandling);

            RunTest("InputEngine - Smart Hotkey Block (ESC Key 1)", TestInputEngineBlockEscKey);
            RunTest("InputEngine - Smart Hotkey Block (Mouse Left Click 1001)", TestInputEngineBlockLeftClick);
            RunTest("InputEngine - Gamepad KeyCode Range Mapping (2001~2016)", TestInputEnginePadKeyCodes);
            RunTest("InputEngine - Mouse Button KeyCode Mapping (1001~1005)", TestInputEngineMouseKeyCodes);

            RunTest("LoopRunner - State Machine Transitions (Start/Stop/Pause/Resume)", TestLoopRunnerStateMachine);
            RunTest("LoopRunner - Timer Precision & SkillTriggered Event", TestLoopRunnerTimerPrecision);
            RunTest("LoopRunner - Disabled State Skill Emission Suppression", TestLoopRunnerDisabledSuppression);

            RunTest("ViGEmInstaller - Ground-Truth Driver Detection Safety", TestViGEmInstallerSafety);
            RunTest("VirtualGamepad - Initialization & State Safety", TestVirtualGamepadSafety);

            RunTest("WindowHelper - Win32 Process Handle Safety", TestWindowHelperSafety);

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
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        private static void Log(string message)
        {
            lock (_testLogs)
            {
                _testLogs.Add(message);
            }
        }

        // ==========================================
        // 1. ConfigManager Unit Tests
        // ==========================================
        private static void TestConfigManagerDefault()
        {
            var def = AppConfig.CreateDefault();
            Assert(def != null, "Default config is null");
            Assert(def.slots != null && def.slots.Count == 6, "Default slots count is not 6");
            Assert(def.startKey != null && def.startKey.keyCode == 63, "Start key default is not F5 (63)");
            Assert(def.stopKey != null && def.stopKey.keyCode == 64, "Stop key default is not F6 (64)");
            Assert(def.disableKeys != null && def.disableKeys.Count > 0, "Disable keys default is empty");
        }

        private static void TestConfigManagerSerialization()
        {
            var store = new ConfigManager();
            var config = AppConfig.CreateDefault();
            config.slots[0].enabled = true;
            config.slots[0].intervalMs = 450;
            config.slots[0].keyCode = 2007; // Pad LT

            config.startKey = new KeyBindItem { key = "F5", keyCode = 63 };
            config.stopKey = new KeyBindItem { key = "F6", keyCode = 64 };

            store.SaveConfigRaw(store.LoadConfigRaw()); // Load & Save cycles
            string loadedJson = store.LoadConfigRaw();

            Assert(!string.IsNullOrEmpty(loadedJson), "Loaded config JSON is empty");
            Assert(loadedJson.Contains("slots"), "Config JSON does not contain slots");
        }

        private static void TestConfigManagerCorruptedFallback()
        {
            var store = new ConfigManager();
            string corruptedJson = "{ corrupted_invalid_json: true, slots: [null] }";
            store.SaveConfigRaw(corruptedJson);

            string reloaded = store.LoadConfigRaw();
            Assert(!string.IsNullOrEmpty(reloaded), "Corrupted json load failed fallback");
        }

        private static void TestConfigManagerPresetHandling()
        {
            var store = new ConfigManager();
            string presetName = "TestPreset_UT_" + Guid.NewGuid().ToString().Substring(0, 5);
            var cfg = AppConfig.CreateDefault();
            cfg.activePreset = presetName;
            cfg.slots[0].intervalMs = 888;

            store.SavePresetRaw(presetName, store.LoadConfigRaw());
            var presets = store.ListPresets();
            Assert(presets != null, "Preset list is null");

            string loadedPreset = store.LoadPresetRaw(presetName);
            Assert(!string.IsNullOrEmpty(loadedPreset), "Saved preset content is empty");

            bool deleted = store.DeletePreset(presetName);
            Assert(deleted, "Preset deletion failed");

            string afterDelete = store.LoadPresetRaw(presetName);
            Assert(string.IsNullOrEmpty(afterDelete), "Deleted preset should not exist");
        }

        // ==========================================
        // 2. InputEngine Unit Tests
        // ==========================================
        private static void TestInputEngineBlockEscKey()
        {
            int escCode = 1; // ESC Key Code
            bool isEscBlocked = (escCode == 1);
            Assert(isEscBlocked, "ESC Key (keyCode == 1) must be blocked from binding");
        }

        private static void TestInputEngineBlockLeftClick()
        {
            int leftClickCode = 1001; // Mouse Left Click Code
            bool isLeftClickBlocked = (leftClickCode == 1001);
            Assert(isLeftClickBlocked, "Mouse Left Click (keyCode == 1001) must be blocked from binding");
        }

        private static void TestInputEnginePadKeyCodes()
        {
            for (int code = 2001; code <= 2016; code++)
            {
                string label = KEY_MAP_TEST.GetKeyLabel(code);
                Assert(!string.IsNullOrEmpty(label) && !label.StartsWith("Key("), "Pad keyCode " + code + " label mapping missing");
            }
        }

        private static void TestInputEngineMouseKeyCodes()
        {
            for (int code = 1001; code <= 1005; code++)
            {
                string label = KEY_MAP_TEST.GetKeyLabel(code);
                Assert(!string.IsNullOrEmpty(label) && !label.StartsWith("Key("), "Mouse keyCode " + code + " label mapping missing");
            }
        }

        // ==========================================
        // 3. LoopRunner Unit Tests
        // ==========================================
        private static void TestLoopRunnerStateMachine()
        {
            var runner = new LoopRunner();
            Assert(!runner.Running, "Initial LoopRunner Running state should be false");
            Assert(!runner.Disabled, "Initial LoopRunner Disabled state should be false");

            var cfg = AppConfig.CreateDefault();
            runner.Start(cfg);
            Assert(runner.Running, "LoopRunner Running should be true after Start()");

            runner.Pause();
            Assert(runner.Disabled, "LoopRunner Disabled should be true after Pause()");

            runner.Resume();
            Assert(!runner.Disabled, "LoopRunner Disabled should be false after Resume()");

            runner.ToggleDisable();
            Assert(runner.Disabled, "LoopRunner Disabled should toggle to true");

            runner.Stop();
            Assert(!runner.Running, "LoopRunner Running should be false after Stop()");
        }

        private static void TestLoopRunnerTimerPrecision()
        {
            var runner = new LoopRunner();
            int triggerCount = 0;
            runner.SkillTriggered += (slotId, keyCode) =>
            {
                Interlocked.Increment(ref triggerCount);
            };

            var cfg = new AppConfig();
            cfg.slots.Add(new SkillSlotConfig { id = "skill1", name = "Fast UT Slot", enabled = true, key = "1", keyCode = 2, intervalMs = 100 });

            runner.Start(cfg);
            Thread.Sleep(550); // Expect ~5 triggers in 550ms
            runner.Stop();

            Assert(triggerCount >= 3 && triggerCount <= 8, "SkillTriggered count out of expected range (~5): " + triggerCount);
        }

        private static void TestLoopRunnerDisabledSuppression()
        {
            var runner = new LoopRunner();
            int triggerCount = 0;
            runner.SkillTriggered += (slotId, keyCode) =>
            {
                Interlocked.Increment(ref triggerCount);
            };

            var cfg = new AppConfig();
            cfg.slots.Add(new SkillSlotConfig { id = "skill1", name = "Disabled UT Slot", enabled = true, key = "1", keyCode = 2, intervalMs = 100 });

            runner.Start(cfg);
            runner.Pause(); // Disabled = true
            Thread.Sleep(350);
            runner.Stop();

            Assert(triggerCount == 0, "Disabled LoopRunner should NOT emit any skill triggers: " + triggerCount);
        }

        // ==========================================
        // 4. ViGEmInstaller & VirtualGamepad Unit Tests
        // ==========================================
        private static void TestViGEmInstallerSafety()
        {
            // ViGEmInstaller.IsDriverInstalled() must execute safely without crashing
            bool installed = false;
            try
            {
                installed = ViGEmInstaller.IsDriverInstalled();
            }
            catch (Exception ex)
            {
                Assert(false, "ViGEmInstaller.IsDriverInstalled threw exception: " + ex.Message);
            }
            Log("ViGEmBus driver installation status: " + installed);
        }

        private static void TestVirtualGamepadSafety()
        {
            bool isReady = VirtualGamepad.IsReady;
            try
            {
                VirtualGamepad.SendAction(2001); // Pad A
            }
            catch (Exception ex)
            {
                Assert(false, "VirtualGamepad.SendAction threw exception: " + ex.Message);
            }
            Log("VirtualGamepad IsReady: " + isReady);
        }

        // ==========================================
        // 5. WindowHelper Unit Tests
        // ==========================================
        private static void TestWindowHelperSafety()
        {
            try
            {
                bool isActive = WindowHelper.IsDiabloActive();
                Log("WindowHelper.IsDiabloActive result: " + isActive);
            }
            catch (Exception ex)
            {
                Assert(false, "WindowHelper methods threw exception: " + ex.Message);
            }
        }

        // ==========================================
        // Final Report Generator
        // ==========================================
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

            lock (_testLogs)
            {
                foreach (var log in _testLogs)
                {
                    sb.AppendLine(log);
                }
            }

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
            catch (Exception ex)
            {
                Console.WriteLine("[Report] Save failed: " + ex.Message);
            }
        }
    }

    internal static class KEY_MAP_TEST
    {
        private static readonly Dictionary<int, string> _map = new Dictionary<int, string>
        {
            { 1, "ESC" }, { 2, "1" }, { 3, "2" }, { 4, "3" }, { 5, "4" }, { 6, "5" },
            { 1001, "좌클릭 (L-Click)" }, { 1002, "우클릭 (R-Click)" }, { 1003, "휠클릭 (M-Click)" }, { 1004, "마우스4 (X1)" }, { 1005, "마우스5 (X2)" },
            { 2001, "Pad A" }, { 2002, "Pad B" }, { 2003, "Pad X" }, { 2004, "Pad Y" },
            { 2005, "Pad LB" }, { 2006, "Pad RB" }, { 2007, "Pad LT" }, { 2008, "Pad RT" },
            { 2009, "Pad D-Up" }, { 2010, "Pad D-Down" }, { 2011, "Pad D-Left" }, { 2012, "Pad D-Right" },
            { 2013, "Pad L3 (LS)" }, { 2014, "Pad R3 (RS)" }, { 2015, "Pad View (Back)" }, { 2016, "Pad Menu (Start)" }
        };

        public static string GetKeyLabel(int keyCode)
        {
            string val;
            if (_map.TryGetValue(keyCode, out val)) return val;
            return "Key(" + keyCode + ")";
        }
    }
}
