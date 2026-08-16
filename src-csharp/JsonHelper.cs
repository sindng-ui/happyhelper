using System;
using System.Text;
using System.Collections.Generic;

namespace HappyHelper
{
    /// <summary>
    /// Pure C# lightweight JSON serializer/deserializer and extractor for AppConfig and IPC payloads.
    /// Eliminates heavy third-party JSON dependencies.
    /// </summary>
    public static class JsonHelper
    {
        public static string SerializeConfig(AppConfig config)
        {
            if (config == null) config = AppConfig.CreateDefault();
            var sb = new StringBuilder();
            sb.Append("{\"slots\":[");
            for (int i = 0; i < config.slots.Count; i++)
            {
                var s = config.slots[i];
                sb.AppendFormat("{{\"id\":\"{0}\",\"name\":\"{1}\",\"enabled\":{2},\"key\":\"{3}\",\"keyCode\":{4},\"intervalMs\":{5}}}",
                    s.id, s.name, s.enabled.ToString().ToLower(), s.key, s.keyCode, s.intervalMs);
                if (i < config.slots.Count - 1) sb.Append(",");
            }
            sb.Append("],");
            sb.AppendFormat("\"startKey\":{{\"key\":\"{0}\",\"keyCode\":{1}}},", config.startKey != null ? config.startKey.key : "F5", config.startKey != null ? config.startKey.keyCode : 63);
            sb.AppendFormat("\"stopKey\":{{\"key\":\"{0}\",\"keyCode\":{1}}},", config.stopKey != null ? config.stopKey.key : "F6", config.stopKey != null ? config.stopKey.keyCode : 64);
            sb.Append("\"disableKeys\":[");
            if (config.disableKeys != null)
            {
                for (int i = 0; i < config.disableKeys.Count; i++)
                {
                    var dk = config.disableKeys[i];
                    sb.AppendFormat("{{\"key\":\"{0}\",\"keyCode\":{1}}}", dk.key, dk.keyCode);
                    if (i < config.disableKeys.Count - 1) sb.Append(",");
                }
            }
            sb.Append("],");
            sb.AppendFormat("\"alwaysOnTop\":{0},\"soundFeedback\":{1}}}",
                config.alwaysOnTop.ToString().ToLower(),
                config.soundFeedback.ToString().ToLower());
            return sb.ToString();
        }

        public static AppConfig DeserializeConfig(string json)
        {
            var def = AppConfig.CreateDefault();
            if (string.IsNullOrEmpty(json)) return def;
            try
            {
                var cfg = new AppConfig();
                var slotObjs = ExtractJsonObjectList(json, "slots");
                foreach (string slotJson in slotObjs)
                {
                    string id = ExtractJsonValue(slotJson, "id");
                    string name = ExtractJsonValue(slotJson, "name");
                    string key = ExtractJsonValue(slotJson, "key");
                    string enabledStr = ExtractJsonValue(slotJson, "enabled");
                    bool enabled = (enabledStr == "true" || enabledStr == "True");
                    int kc = 2, iv = 1000;
                    int.TryParse(ExtractJsonValue(slotJson, "keyCode"), out kc);
                    int.TryParse(ExtractJsonValue(slotJson, "intervalMs"), out iv);

                    foreach (var defSlot in def.slots)
                    {
                        if (defSlot.id == id)
                        {
                            name = defSlot.name;
                            break;
                        }
                    }

                    cfg.slots.Add(new SkillSlotConfig
                    {
                        id = id,
                        name = name,
                        enabled = enabled,
                        key = key,
                        keyCode = kc,
                        intervalMs = iv
                    });
                }
                if (cfg.slots.Count == 0) cfg.slots = def.slots;

                string startKeyObj = ExtractJsonObject(json, "startKey");
                if (!string.IsNullOrEmpty(startKeyObj))
                {
                    int kc = 63;
                    int.TryParse(ExtractJsonValue(startKeyObj, "keyCode"), out kc);
                    cfg.startKey = new KeyBindItem { key = ExtractJsonValue(startKeyObj, "key"), keyCode = kc };
                }
                else cfg.startKey = def.startKey;

                string stopKeyObj = ExtractJsonObject(json, "stopKey");
                if (!string.IsNullOrEmpty(stopKeyObj))
                {
                    int kc = 64;
                    int.TryParse(ExtractJsonValue(stopKeyObj, "keyCode"), out kc);
                    cfg.stopKey = new KeyBindItem { key = ExtractJsonValue(stopKeyObj, "key"), keyCode = kc };
                }
                else cfg.stopKey = def.stopKey;

                var disObjs = ExtractJsonObjectList(json, "disableKeys");
                foreach (string disJson in disObjs)
                {
                    string key = ExtractJsonValue(disJson, "key");
                    int kc = 1;
                    if (int.TryParse(ExtractJsonValue(disJson, "keyCode"), out kc))
                    {
                        cfg.disableKeys.Add(new KeyBindItem { key = key, keyCode = kc });
                    }
                }
                if (cfg.disableKeys.Count == 0) cfg.disableKeys = def.disableKeys;

                cfg.alwaysOnTop = ExtractJsonValue(json, "alwaysOnTop") == "true";
                cfg.soundFeedback = ExtractJsonValue(json, "soundFeedback") != "false";
                return cfg;
            }
            catch { return def; }
        }

        public static List<string> ExtractJsonObjectList(string json, string arrayKey)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;
            string target = "\"" + arrayKey + "\":";
            int idx = json.IndexOf(target);
            if (idx == -1) return list;
            int arrayStart = json.IndexOf('[', idx + target.Length);
            if (arrayStart == -1) return list;

            int braceDepth = 0;
            int bracketDepth = 1;
            bool inQuote = false;
            int objStart = -1;

            for (int i = arrayStart + 1; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && inQuote)
                {
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote)
                {
                    if (c == '[')
                    {
                        bracketDepth++;
                    }
                    else if (c == ']')
                    {
                        bracketDepth--;
                        if (bracketDepth == 0) break;
                    }
                    else if (c == '{')
                    {
                        if (braceDepth == 0) objStart = i;
                        braceDepth++;
                    }
                    else if (c == '}')
                    {
                        braceDepth--;
                        if (braceDepth == 0 && objStart != -1)
                        {
                            list.Add(json.Substring(objStart, i - objStart + 1));
                            objStart = -1;
                        }
                    }
                }
            }
            return list;
        }

        public static string ExtractJsonObject(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";
            string target = "\"" + key + "\":";
            int idx = json.IndexOf(target);
            if (idx == -1) return "";
            int start = json.IndexOf('{', idx + target.Length);
            if (start == -1) return "";
            int depth = 0;
            bool inQuote = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && inQuote)
                {
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inQuote = !inQuote;
                }
                else if (!inQuote)
                {
                    if (c == '{') depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return json.Substring(start, i - start + 1);
                        }
                    }
                }
            }
            return "";
        }

        public static string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";
            string target = "\"" + key + "\":";
            int idx = json.IndexOf(target);
            if (idx == -1) return "";
            int start = idx + target.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            if (start >= json.Length) return "";
            if (json[start] == '"')
            {
                int end = -1;
                for (int i = start + 1; i < json.Length; i++)
                {
                    if (json[i] == '\\') { i++; continue; }
                    if (json[i] == '"') { end = i; break; }
                }
                return (end != -1) ? json.Substring(start + 1, end - start - 1) : "";
            }
            int endChar = json.IndexOfAny(new char[] { ',', '}', ']' }, start);
            return (endChar != -1) ? json.Substring(start, endChar - start).Trim() : json.Substring(start).Trim();
        }
    }
}
