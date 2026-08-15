using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace HappyHelper
{
    public class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_NCLBUTTONDOWN = 0xA1;
        private const uint HTCAPTION = 0x2;

        private ConfigManager _store;
        private LoopRunner _inputLoop;
        private GlobalHook _globalListener;

        private string _currentConfigJson;
        private bool _bindingMode = false;
        private bool _webViewReady = false;

        private Microsoft.Web.WebView2.Wpf.WebView2 webView;
        private System.Threading.Timer _statusPollTimer;
        private string _lastDiagnosticPayload = "";

        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new Application();
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public MainWindow()
        {
            InitWindowUI();

            _store = new ConfigManager();
            _inputLoop = new LoopRunner();

            _globalListener = new GlobalHook();
            _globalListener.KeyPressed += OnGlobalKeyPressed;
            _globalListener.Start();

            _inputLoop.StateChanged += OnLoopStateChanged;
            _inputLoop.SkillTriggered += OnSkillTriggered;

            // Pre-initialize VirtualGamepad and GamepadPassthrough engine with Slot 0 guarantee
            try
            {
                DeviceManager.EnsureVirtualPadIsSlot0();
            }
            catch (Exception ex)
            {
                DebugLog.Write("[MainWindow] DeviceManager start failed: " + ex.Message);
            }

            _currentConfigJson = _store.LoadConfigRaw();
            if (string.IsNullOrEmpty(_currentConfigJson) || !_currentConfigJson.Trim().StartsWith("{") || !_currentConfigJson.Trim().EndsWith("}"))
            {
                _currentConfigJson = SimpleJsonSerialize(AppConfig.CreateDefault());
                _store.SaveConfigRaw(_currentConfigJson);
            }

            this.Loaded += OnWindowLoaded;
            this.Closed += OnWindowClosed;
        }

        private void InitWindowUI()
        {
            this.Title = "Diablo IV Auto-Skill Helper";
            this.Width = 420;
            this.Height = 730;
            this.MinWidth = 390;
            this.MinHeight = 680;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.CanResize;
            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d0f12"));
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Topmost = true;


            var grid = new Grid();
            this.webView = new Microsoft.Web.WebView2.Wpf.WebView2();
            this.webView.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            this.webView.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            grid.Children.Add(this.webView);
            this.Content = grid;
        }





        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Mark WebView ready immediately after CoreWebView2 is initialized
                // (NOT in DOMContentLoaded — that's too late and causes race conditions)
                _webViewReady = true;

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "renderer", "index.html");
                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.NavigationCompleted += (s, args) =>
                    {
                        SendDiagnosticStatus();
                        StartStatusPollTimer();
                    };
                    webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                    MessageBox.Show("renderer/index.html not found: " + htmlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 init failed: " + ex.Message);
            }
        }

        private void StartStatusPollTimer()
        {
            if (_statusPollTimer == null)
            {
                _statusPollTimer = new System.Threading.Timer((state) =>
                {
                    SendDiagnosticStatus();
                }, null, 2000, 2000);
            }
        }

        private void SendDiagnosticStatus()
        {
            if (!_webViewReady || webView == null) return;
            try
            {
                bool xLoaded = _globalListener != null && _globalListener.IsXInputLoaded;
                bool ctrlConnected = (_globalListener != null && _globalListener.IsControllerConnected) || GamepadPassthrough.IsRunning;
                bool viGEmInstalled = ViGEmInstaller.IsDriverInstalled();

                if (viGEmInstalled && !VirtualGamepad.IsReady)
                {
                    try { VirtualGamepad.Initialize(); } catch { }
                }

                bool viGEmReady = VirtualGamepad.IsReady;

                string payload = string.Format("{{\"xInputLoaded\":{0},\"controllerConnected\":{1},\"viGEmInstalled\":{2},\"viGEmReady\":{3},\"vigemInstalled\":{2}}}",
                    xLoaded.ToString().ToLower(), ctrlConnected.ToString().ToLower(),
                    viGEmInstalled.ToString().ToLower(), viGEmReady.ToString().ToLower());

                if (payload != _lastDiagnosticPayload)
                {
                    _lastDiagnosticPayload = payload;
                    Dispatcher.BeginInvoke((Action)delegate
                    {
                        SendToJs("pad-status", payload);
                    });
                }
            }
            catch { }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _webViewReady = false;
            if (_statusPollTimer != null)
            {
                _statusPollTimer.Dispose();
                _statusPollTimer = null;
            }
            _inputLoop.Stop();
            _globalListener.Stop();

            // Restore physical gamepad to Slot #0 upon app exit
            DeviceManager.RestorePhysicalPadToSlot0();
        }

        private void OnLoopStateChanged(bool running, bool disabled)
        {
            var json = string.Format("{{\"running\":{0},\"disabled\":{1}}}",
                running.ToString().ToLower(), disabled.ToString().ToLower());
            SendToJs("state-changed", json);
        }

        private void OnSkillTriggered(string slotId, int keyCode)
        {
            if (!_webViewReady || webView == null || webView.CoreWebView2 == null) return;
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string script = string.Format("window.triggerSkillPulse && window.triggerSkillPulse('{0}');", slotId);
                    webView.CoreWebView2.ExecuteScriptAsync(script);
                }
                catch { }

                try
                {
                    string payload = string.Format("{{\"slotId\":\"{0}\",\"keyCode\":{1}}}", slotId, keyCode);
                    TrySendToJs("skill-triggered", payload);
                }
                catch { }
            }));
        }



        // ★ Key handler: pad, keyboard, mouse all handled here
        private void OnGlobalKeyPressed(int keyCode, bool isMouse)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                try
                {
                    if (_bindingMode)
                    {
                        // ★ If mouse click occurs INSIDE App Window bounds, ignore it as UI interaction!
                        if (isMouse && IsMouseOverAppWindow())
                        {
                            return;
                        }

                        // ★ Block ESC key (keyCode == 1) from being bound as a hotkey! Cancel binding instead.
                        if (keyCode == 1)
                        {
                            _bindingMode = false;
                            TrySendToJs("key-bound-cancel", "{}");
                            return;
                        }

                        // ★ Block Mouse Left Click (keyCode == 1001) from being bound as a hotkey!
                        if (keyCode == 1001)
                        {
                            return;
                        }

                        var payload = string.Format("{{\"keyCode\":{0},\"isMouse\":{1}}}",
                            keyCode, isMouse.ToString().ToLower());
                        bool sent = TrySendToJs("key-bound", payload);
                        if (sent) _bindingMode = false;
                        return;
                    }


                    var currentCfg = SimpleJsonDeserialize(_currentConfigJson);
                    int startCode = (currentCfg != null && currentCfg.startKey != null) ? currentCfg.startKey.keyCode : 63;
                    int stopCode = (currentCfg != null && currentCfg.stopKey != null) ? currentCfg.stopKey.keyCode : 64;

                    if (startCode == stopCode && keyCode == startCode)
                    {
                        // When Start and Stop are bound to the same key:
                        // 1. If not running -> Start
                        // 2. If running but paused (disabled) -> Resume (Start)
                        // 3. If running and active -> Stop
                        if (!_inputLoop.Running)
                        {
                            _inputLoop.Start(currentCfg);
                            TrySendToJs("hotkey-notice", "\"start\"");
                        }
                        else if (_inputLoop.Disabled)
                        {
                            _inputLoop.Resume();
                            TrySendToJs("hotkey-notice", "\"enable\"");
                        }
                        else
                        {
                            _inputLoop.Stop();
                            TrySendToJs("hotkey-notice", "\"stop\"");
                        }
                    }

                    else if (keyCode == startCode)
                    {
                        if (!_inputLoop.Running)
                        {
                            _inputLoop.Start(currentCfg);
                            TrySendToJs("hotkey-notice", "\"start\"");
                        }
                        else if (_inputLoop.Disabled)
                        {
                            _inputLoop.Resume();
                            TrySendToJs("hotkey-notice", "\"enable\"");
                        }
                    }
                    else if (keyCode == stopCode)
                    {
                        if (_inputLoop.Running)
                        {
                            _inputLoop.Stop();
                            TrySendToJs("hotkey-notice", "\"stop\"");
                        }
                    }

                    else if (currentCfg != null && currentCfg.disableKeys != null && currentCfg.disableKeys.Count > 0)
                    {
                        bool isDisableKey = false;
                        foreach (var dk in currentCfg.disableKeys)
                        {
                            if (dk.keyCode == keyCode)
                            {
                                isDisableKey = true;
                                break;
                            }
                        }

                        if (isDisableKey && _inputLoop.Running && !_inputLoop.Disabled)
                        {
                            _inputLoop.Pause();
                            TrySendToJs("hotkey-notice", "\"disable\"");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("OnGlobalKeyPressed error: " + ex.Message);
                }
            });
        }

        private bool TrySendToJs(string type, string payloadJson)
        {
            if (!_webViewReady || webView == null || webView.CoreWebView2 == null)
                return false;
            try
            {
                string msg = string.Format("{{\"type\":\"{0}\",\"payload\":{1}}}", type, payloadJson);
                webView.CoreWebView2.PostWebMessageAsJson(msg);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendToJs error: " + ex.Message);
                return false;
            }
        }

        private void SendToJs(string type, string payloadJson)
        {
            TrySendToJs(type, payloadJson);
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string raw = e.TryGetWebMessageAsString();

                if (raw.Contains("\"method\":\"dragWindow\""))
                {
                    Dispatcher.BeginInvoke((Action)delegate {
                        try
                        {
                            var helper = new WindowInteropHelper(this);
                            ReleaseCapture();
                            SendMessage(helper.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                        }
                        catch { }
                    });
                }
                else if (raw.Contains("\"method\":\"getPadStatus\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    bool vigemInstalled = ViGEmInstaller.IsDriverInstalled();
                    if (vigemInstalled && !VirtualGamepad.IsReady)
                    {
                        try { VirtualGamepad.Initialize(); } catch { }
                    }
                    bool viGEmReady = VirtualGamepad.IsReady;
                    bool xinputLoaded = _globalListener != null && _globalListener.IsXInputLoaded;
                    bool ctrlConnected = (_globalListener != null && _globalListener.IsControllerConnected) || GamepadPassthrough.IsRunning;
                    string payload = string.Format("{{\"vigemInstalled\":{0},\"viGEmInstalled\":{0},\"viGEmReady\":{1},\"xinputLoaded\":{2},\"xInputLoaded\":{2},\"controllerConnected\":{3}}}",
                        vigemInstalled.ToString().ToLower(), viGEmReady.ToString().ToLower(), xinputLoaded.ToString().ToLower(), ctrlConnected.ToString().ToLower());
                    SendDiagnosticStatus();
                    SendResponse(reqId, payload);
                }

                else if (raw.Contains("\"method\":\"getConfig\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    if (string.IsNullOrEmpty(_currentConfigJson) || !_currentConfigJson.Trim().StartsWith("{"))
                    {
                        _currentConfigJson = SimpleJsonSerialize(AppConfig.CreateDefault());
                        _store.SaveConfigRaw(_currentConfigJson);
                    }
                    SendResponse(reqId, _currentConfigJson);
                }
                else if (raw.Contains("\"method\":\"saveConfig\"") || raw.Contains("\"method\":\"updateConfig\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    string cfg = ExtractJsonObject(raw, "config");
                    if (!string.IsNullOrEmpty(cfg))
                    {
                        _currentConfigJson = cfg;
                        _store.SaveConfigRaw(cfg);
                        _inputLoop.UpdateConfig(SimpleJsonDeserialize(cfg));
                    }
                    SendResponse(reqId, "true");
                }
                else if (raw.Contains("\"method\":\"startLoop\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    string cfg = ExtractJsonObject(raw, "config");
                    DebugLog.Write("[startLoop] cfg length=" + (cfg == null ? "null" : cfg.Length.ToString()));
                    if (!string.IsNullOrEmpty(cfg))
                    {
                        _currentConfigJson = cfg;
                        _store.SaveConfigRaw(cfg);
                        var parsedCfg = SimpleJsonDeserialize(cfg);
                        if (parsedCfg != null && parsedCfg.slots != null)
                        {
                            foreach (var s in parsedCfg.slots)
                                DebugLog.Write("[startLoop] slot id=" + s.id + " enabled=" + s.enabled + " keyCode=" + s.keyCode + " key=" + s.key + " intervalMs=" + s.intervalMs);

                        }
                        _inputLoop.UpdateConfig(parsedCfg);
                    }
                    var startCfg = SimpleJsonDeserialize(_currentConfigJson);
                    DebugLog.Write("[startLoop] calling Start() with " + (startCfg == null ? "null" : startCfg.slots.Count + " slots"));
                    _inputLoop.Start(startCfg);
                    SendResponse(reqId, "true");
                }

                else if (raw.Contains("\"method\":\"stopLoop\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    _inputLoop.Stop();
                    SendResponse(reqId, "true");
                }
                else if (raw.Contains("\"method\":\"toggleDisable\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    _inputLoop.ToggleDisable();
                    SendResponse(reqId, "true");
                }
                else if (raw.Contains("\"method\":\"startKeyBind\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    _bindingMode = true;
                    SendResponse(reqId, "true");
                }
                else if (raw.Contains("\"method\":\"cancelKeyBind\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    _bindingMode = false;
                    SendResponse(reqId, "true");
                }
                else if (raw.Contains("\"method\":\"listPresets\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    var list = _store.ListPresets();
                    var sb = new System.Text.StringBuilder("[");
                    for (int i = 0; i < list.Count; i++)
                    {
                        sb.AppendFormat("\"{0}\"", list[i]);
                        if (i < list.Count - 1) sb.Append(",");
                    }
                    sb.Append("]");
                    SendResponse(reqId, sb.ToString());
                }
                else if (raw.Contains("\"method\":\"savePreset\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    string presetName = ExtractJsonValue(raw, "name");
                    string cfg = ExtractJsonObject(raw, "config");
                    if (string.IsNullOrEmpty(cfg)) cfg = _currentConfigJson;
                    else
                    {
                        _currentConfigJson = cfg;
                        _store.SaveConfigRaw(cfg);
                    }
                    _store.SavePresetRaw(presetName, cfg);
                    SendResponse(reqId, "true");
                }
                else if (raw.Contains("\"method\":\"loadPreset\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    string presetName = ExtractJsonValue(raw, "name");
                    string loaded = _store.LoadPresetRaw(presetName);
                    if (!string.IsNullOrEmpty(loaded))
                    {
                        _currentConfigJson = loaded;
                        _store.SaveConfigRaw(loaded);
                        _inputLoop.UpdateConfig(SimpleJsonDeserialize(loaded));
                    }
                    SendResponse(reqId, string.IsNullOrEmpty(loaded) ? "null" : loaded);
                }
                else if (raw.Contains("\"method\":\"deletePreset\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    string presetName = ExtractJsonValue(raw, "name");
                    bool deleted = _store.DeletePreset(presetName);
                    SendResponse(reqId, deleted.ToString().ToLower());
                }
                else if (raw.Contains("\"method\":\"setAlwaysOnTop\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    this.Topmost = ExtractJsonValue(raw, "alwaysOnTop") == "true";
                    SendResponse(reqId, "true");
                }

                else if (raw.Contains("\"method\":\"installViGEmDriver\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    bool launched = ViGEmInstaller.LaunchInstaller();
                    SendResponse(reqId, launched.ToString().ToLower());

                    if (launched)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(delegate
                        {
                            for (int i = 0; i < 60; i++)
                            {
                                System.Threading.Thread.Sleep(1500);
                                if (ViGEmInstaller.IsDriverInstalled())
                                {
                                    VirtualGamepad.Initialize();
                                    this.Dispatcher.BeginInvoke((Action)delegate
                                    {
                                        SendDiagnosticStatus();
                                    });
                                    break;
                                }
                            }
                        });
                    }
                }

                else if (raw.Contains("\"method\":\"setWindowMode\""))
                {
                    string reqId = ExtractJsonValue(raw, "reqId");
                    string mode = ExtractJsonValue(raw, "mode");

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (mode == "mini")
                        {
                            this.MinWidth = 180;
                            this.MinHeight = 40;
                            this.Width = 195;
                            this.Height = 46;
                            this.ResizeMode = ResizeMode.NoResize;
                        }

                        else
                        {
                            this.MinWidth = 390;
                            this.MinHeight = 680;
                            this.Width = 420;
                            this.Height = 730;
                            this.ResizeMode = ResizeMode.CanResize;
                        }
                    }));
                    SendResponse(reqId, "true");
                }




                else if (raw.Contains("\"method\":\"minimizeWindow\""))
                {
                    this.WindowState = WindowState.Minimized;
                }
                else if (raw.Contains("\"method\":\"closeWindow\""))
                {
                    this.Close();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("IPC error: " + ex.Message);
            }
        }

        private void SendResponse(string reqId, string payloadJson)
        {
            if (webView == null || webView.CoreWebView2 == null) return;
            try
            {
                string msg = string.Format("{{\"type\":\"response\",\"reqId\":\"{0}\",\"payload\":{1}}}", reqId, payloadJson);
                webView.CoreWebView2.PostWebMessageAsJson(msg);
            }
            catch (Exception ex) { Console.WriteLine("SendResponse error: " + ex.Message); }
        }

        private List<string> ExtractJsonObjectList(string json, string arrayKey)
        {
            var list = new List<string>();
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
                        if (bracketDepth == 0) break; // End of target array
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

        private string ExtractJsonObject(string json, string key)
        {
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

        private string ExtractJsonValue(string json, string key)
        {
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

        private string SimpleJsonSerialize(AppConfig config)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"slots\":[");
            for (int i = 0; i < config.slots.Count; i++)
            {
                var s = config.slots[i];
                sb.AppendFormat("{{\"id\":\"{0}\",\"name\":\"{1}\",\"enabled\":{2},\"key\":\"{3}\",\"keyCode\":{4},\"intervalMs\":{5}}}",
                    s.id, s.name, s.enabled.ToString().ToLower(), s.key, s.keyCode, s.intervalMs);
                if (i < config.slots.Count - 1) sb.Append(",");
            }
            sb.Append("],");
            sb.AppendFormat("\"startKey\":{{\"key\":\"{0}\",\"keyCode\":{1}}},", config.startKey.key, config.startKey.keyCode);
            sb.AppendFormat("\"stopKey\":{{\"key\":\"{0}\",\"keyCode\":{1}}},", config.stopKey.key, config.stopKey.keyCode);
            sb.Append("\"disableKeys\":[");
            for (int i = 0; i < config.disableKeys.Count; i++)
            {
                var dk = config.disableKeys[i];
                sb.AppendFormat("{{\"key\":\"{0}\",\"keyCode\":{1}}}", dk.key, dk.keyCode);
                if (i < config.disableKeys.Count - 1) sb.Append(",");
            }
            sb.Append("],");
            sb.AppendFormat("\"alwaysOnTop\":{0},\"soundFeedback\":{1}}}",
                config.alwaysOnTop.ToString().ToLower(),
                config.soundFeedback.ToString().ToLower());
            return sb.ToString();
        }

        private AppConfig SimpleJsonDeserialize(string json)
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

        private bool IsMouseOverAppWindow()
        {
            try
            {
                POINT pt;
                if (GetCursorPos(out pt))
                {
                    double left = this.Left;
                    double top = this.Top;
                    double right = left + this.Width;
                    double bottom = top + this.Height;

                    if (pt.X >= left && pt.X <= right && pt.Y >= top && pt.Y <= bottom)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}



