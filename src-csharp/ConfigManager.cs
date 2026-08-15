using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace HappyHelper
{
    public class SkillSlotConfig
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool enabled { get; set; }
        public string key { get; set; }
        public int keyCode { get; set; }
        public int intervalMs { get; set; }
    }

    public class KeyBindItem
    {
        public string key { get; set; }
        public int keyCode { get; set; }
    }

    public class AppConfig
    {
        public List<SkillSlotConfig> slots { get; set; }
        public KeyBindItem startKey { get; set; }
        public KeyBindItem stopKey { get; set; }
        public List<KeyBindItem> disableKeys { get; set; }
        public bool alwaysOnTop { get; set; }
        public bool soundFeedback { get; set; }


        public AppConfig()
        {
            slots = new List<SkillSlotConfig>();
            disableKeys = new List<KeyBindItem>();
        }

        public static AppConfig CreateDefault()
        {
            var cfg = new AppConfig();
            cfg.slots.Add(new SkillSlotConfig { id = "skillLeft", name = "기본 기술 (좌클릭 / A)", enabled = false, key = "MouseLeft", keyCode = 1001, intervalMs = 300 });
            cfg.slots.Add(new SkillSlotConfig { id = "skillRight", name = "핵심 기술 (우클릭 / X)", enabled = false, key = "MouseRight", keyCode = 1002, intervalMs = 400 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill1", name = "스킬 1 (키보드 1 / Y)", enabled = true, key = "1", keyCode = 2, intervalMs = 1000 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill2", name = "스킬 2 (키보드 2 / RB)", enabled = true, key = "2", keyCode = 3, intervalMs = 1000 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill3", name = "스킬 3 (키보드 3 / RT)", enabled = true, key = "3", keyCode = 4, intervalMs = 1000 });
            cfg.slots.Add(new SkillSlotConfig { id = "skill4", name = "스킬 4 (키보드 4 / LT)", enabled = true, key = "4", keyCode = 5, intervalMs = 1000 });

            cfg.startKey = new KeyBindItem { key = "F5", keyCode = 63 };
            cfg.stopKey = new KeyBindItem { key = "F6", keyCode = 64 };

            cfg.disableKeys.Add(new KeyBindItem { key = "Escape", keyCode = 1 });
            cfg.disableKeys.Add(new KeyBindItem { key = "t", keyCode = 20 });
            cfg.disableKeys.Add(new KeyBindItem { key = "i", keyCode = 23 });
            cfg.disableKeys.Add(new KeyBindItem { key = "Enter", keyCode = 28 });

            cfg.alwaysOnTop = true;
            cfg.soundFeedback = true;

            cfg.soundFeedback = true;

            return cfg;
        }


    }

    public class ConfigManager
    {
        private string baseDir;
        private string configFile;
        private string presetDir;

        public ConfigManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            baseDir = Path.Combine(appData, "happyhelper");
            configFile = Path.Combine(baseDir, "config.json");
            presetDir = Path.Combine(baseDir, "presets");

            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(presetDir);
        }

        public string LoadConfigRaw()
        {
            if (File.Exists(configFile))
            {
                try { return File.ReadAllText(configFile, Encoding.UTF8); } catch {}
            }
            return "";
        }

        public void SaveConfigRaw(string json)
        {
            try
            {
                File.WriteAllText(configFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveConfig error: " + ex.Message);
            }
        }

        public List<string> ListPresets()
        {
            var list = new List<string>();
            try
            {
                foreach (var file in Directory.GetFiles(presetDir, "*.json"))
                {
                    list.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch {}
            return list;
        }

        public void SavePresetRaw(string name, string json)
        {
            try
            {
                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(presetDir, safeName + ".json");
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch {}
        }

        public string LoadPresetRaw(string name)
        {
            string filePath = Path.Combine(presetDir, name + ".json");
            if (File.Exists(filePath))
            {
                try { return File.ReadAllText(filePath, Encoding.UTF8); } catch {}
            }
            return "";
        }
    }
}
