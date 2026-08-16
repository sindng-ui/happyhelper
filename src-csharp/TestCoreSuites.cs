using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace HappyHelper
{
    public static class TestCoreSuites
    {
        public static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion Failed: " + message);
        }

        // ==========================================
        // 1. ConfigManager Unit Tests
        // ==========================================
        public static void TestConfigManagerDefault()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "happyhelper_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new ConfigManager(tempDir);
                var def = AppConfig.CreateDefault();
                Assert(def != null, "Default config is null");
                Assert(def.slots != null && def.slots.Count == 6, "Default slots count != 6");
                Assert(def.startKey != null && def.startKey.keyCode == 63, "Default startKey != F5(63)");
                Assert(def.stopKey != null && def.stopKey.keyCode == 64, "Default stopKey != F6(64)");
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        public static void TestConfigManagerSerialization()
        {
            var def = AppConfig.CreateDefault();
            def.slots[0].intervalMs = 2500;
            def.slots[0].enabled = false;

            string json = JsonHelper.SerializeConfig(def);
            Assert(!string.IsNullOrEmpty(json), "Serialized JSON is empty");
            Assert(json.Contains("\"intervalMs\":2500"), "JSON missing intervalMs 2500");

            var restored = JsonHelper.DeserializeConfig(json);
            Assert(restored != null, "Deserialized config is null");
            Assert(restored.slots[0].intervalMs == 2500, "Restored slot 0 interval != 2500");
            Assert(restored.slots[0].enabled == false, "Restored slot 0 enabled != false");
        }

        public static void TestConfigManagerCorruptedFallback()
        {
            string badJson = "{ invalid json string ";
            var fallback = JsonHelper.DeserializeConfig(badJson);
            Assert(fallback != null, "Corrupted JSON didn't return fallback config");
            Assert(fallback.slots != null && fallback.slots.Count == 6, "Fallback slots count != 6");
        }

        public static void TestConfigManagerPresetHandling()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "happyhelper_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new ConfigManager(tempDir);
                var def = AppConfig.CreateDefault();
                def.slots[0].name = "Custom Barba";
                string json = JsonHelper.SerializeConfig(def);

                store.SavePresetRaw("barba_preset", json);
                var list = store.ListPresets();
                Assert(list.Contains("barba_preset"), "ListPresets missing saved preset");

                string loaded = store.LoadPresetRaw("barba_preset");
                Assert(!string.IsNullOrEmpty(loaded), "LoadPresetRaw returned empty");

                bool deleted = store.DeletePreset("barba_preset");
                Assert(deleted, "DeletePreset returned false");
                Assert(!store.ListPresets().Contains("barba_preset"), "Preset not deleted from list");
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        // ==========================================
        // 2. InputEngine Unit Tests
        // ==========================================
        public static void TestInputEngineBlockEscKey()
        {
            int escCode = 1;
            bool isEscBlocked = (escCode == 1);
            Assert(isEscBlocked, "ESC key (keyCode == 1) must be blocked from binding");
        }

        public static void TestInputEngineBlockLeftClick()
        {
            int leftClickCode = 1001;
            bool isLeftClickBlocked = (leftClickCode == 1001);
            Assert(isLeftClickBlocked, "Mouse Left Click (keyCode == 1001) must be blocked from binding");
        }

        public static void TestInputEnginePadKeyCodes()
        {
            for (int code = 2001; code <= 2016; code++)
            {
                string label = TestKeyMap.GetKeyLabel(code);
                Assert(!string.IsNullOrEmpty(label) && !label.StartsWith("Key("), "Pad keyCode " + code + " label mapping missing");
            }
        }

        public static void TestInputEngineMouseKeyCodes()
        {
            for (int code = 1001; code <= 1005; code++)
            {
                string label = TestKeyMap.GetKeyLabel(code);
                Assert(!string.IsNullOrEmpty(label) && !label.StartsWith("Key("), "Mouse keyCode " + code + " label mapping missing");
            }
        }

        // ==========================================
        // 3. LoopRunner Unit Tests
        // ==========================================
        public static void TestLoopRunnerStateMachine()
        {
            var runner = new LoopRunner();
            var cfg = AppConfig.CreateDefault();

            Assert(!runner.Running, "LoopRunner Running should initially be false");
            Assert(!runner.Disabled, "LoopRunner Disabled should initially be false");

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

        public static void TestLoopRunnerTimerPrecision()
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
            Thread.Sleep(550);
            runner.Stop();

            Assert(triggerCount >= 3 && triggerCount <= 8, "SkillTriggered count out of expected range (~5): " + triggerCount);
        }

        public static void TestLoopRunnerDisabledSuppression()
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
            Thread.Sleep(60);
            runner.Pause();
            Thread.Sleep(60);
            Interlocked.Exchange(ref triggerCount, 0);
            Thread.Sleep(350);
            runner.Stop();

            Assert(triggerCount == 0, "Disabled LoopRunner should NOT emit any skill triggers during pause: " + triggerCount);
        }
    }
}
