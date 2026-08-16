using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace HappyHelper
{
    /// <summary>
    /// IPC Message Dispatcher between Frontend WebView2 UI and Backend C# Logic.
    /// Handles all RPC methods, configurations, presets, window events and driver installations.
    /// </summary>
    public class IpcBridge
    {
        private readonly Window _window;
        private readonly WebView2 _webView;
        private readonly ConfigManager _store;
        private readonly LoopRunner _inputLoop;
        private readonly GlobalHook _globalListener;
        private readonly StatusBroadcaster _broadcaster;
        private readonly Action<bool> _setBindingMode;
        private readonly Func<string> _getConfigJson;
        private readonly Action<string> _setConfigJson;

        public IpcBridge(
            Window window,
            WebView2 webView,
            ConfigManager store,
            LoopRunner inputLoop,
            GlobalHook globalListener,
            StatusBroadcaster broadcaster,
            Action<bool> setBindingMode,
            Func<string> getConfigJson,
            Action<string> setConfigJson)
        {
            _window = window;
            _webView = webView;
            _store = store;
            _inputLoop = inputLoop;
            _globalListener = globalListener;
            _broadcaster = broadcaster;
            _setBindingMode = setBindingMode;
            _getConfigJson = getConfigJson;
            _setConfigJson = setConfigJson;
        }

        public void HandleMessage(string rawMessage)
        {
            if (string.IsNullOrEmpty(rawMessage)) return;

            try
            {
                if (rawMessage.Contains("\"method\":\"dragWindow\""))
                {
                    _window.Dispatcher.BeginInvoke((Action)delegate
                    {
                        var helper = new WindowInteropHelper(_window);
                        WindowController.DragWindow(helper.Handle);
                    });
                }
                else if (rawMessage.Contains("\"method\":\"getPadStatus\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    bool vigemInstalled = ViGEmInstaller.IsDriverInstalled();
                    if (vigemInstalled && !VirtualGamepad.IsReady)
                    {
                        try { VirtualGamepad.Initialize(); } catch { }
                    }
                    bool viGEmReady = VirtualGamepad.IsReady;
                    bool xinputLoaded = _globalListener != null && _globalListener.IsXInputLoaded;
                    bool ctrlConnected = (_globalListener != null && _globalListener.IsControllerConnected) || GamepadPassthrough.IsRunning;
                    bool hidHideInstalled = HidHideManager.IsDriverInstalled();
                    bool hidHideActive = hidHideInstalled && HidHideManager.GetActive();

                    string payload = string.Format(
                        "{{\"vigemInstalled\":{0},\"viGEmInstalled\":{0},\"viGEmReady\":{1},\"xinputLoaded\":{2},\"xInputLoaded\":{2},\"controllerConnected\":{3},\"hidHideInstalled\":{4},\"hidHideActive\":{5}}}",
                        vigemInstalled.ToString().ToLower(),
                        viGEmReady.ToString().ToLower(),
                        xinputLoaded.ToString().ToLower(),
                        ctrlConnected.ToString().ToLower(),
                        hidHideInstalled.ToString().ToLower(),
                        hidHideActive.ToString().ToLower());

                    if (_broadcaster != null) _broadcaster.Broadcast();
                    SendResponse(reqId, payload);
                }
                else if (rawMessage.Contains("\"method\":\"getConfig\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string currentJson = _getConfigJson != null ? _getConfigJson() : "";
                    if (string.IsNullOrEmpty(currentJson) || !currentJson.Trim().StartsWith("{"))
                    {
                        currentJson = JsonHelper.SerializeConfig(AppConfig.CreateDefault());
                        if (_setConfigJson != null) _setConfigJson(currentJson);
                        _store.SaveConfigRaw(currentJson);
                    }
                    SendResponse(reqId, currentJson);
                }
                else if (rawMessage.Contains("\"method\":\"saveConfig\"") || rawMessage.Contains("\"method\":\"updateConfig\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string cfg = JsonHelper.ExtractJsonObject(rawMessage, "config");
                    if (!string.IsNullOrEmpty(cfg))
                    {
                        if (_setConfigJson != null) _setConfigJson(cfg);
                        _store.SaveConfigRaw(cfg);
                        _inputLoop.UpdateConfig(JsonHelper.DeserializeConfig(cfg));
                    }
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"startLoop\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string cfg = JsonHelper.ExtractJsonObject(rawMessage, "config");
                    if (!string.IsNullOrEmpty(cfg))
                    {
                        if (_setConfigJson != null) _setConfigJson(cfg);
                        _store.SaveConfigRaw(cfg);
                        _inputLoop.UpdateConfig(JsonHelper.DeserializeConfig(cfg));
                    }
                    string curCfgJson = _getConfigJson != null ? _getConfigJson() : "";
                    var startCfg = JsonHelper.DeserializeConfig(curCfgJson);
                    _inputLoop.Start(startCfg);
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"stopLoop\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    _inputLoop.Stop();
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"toggleDisable\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    _inputLoop.ToggleDisable();
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"startKeyBind\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    if (_setBindingMode != null) _setBindingMode(true);
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"cancelKeyBind\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    if (_setBindingMode != null) _setBindingMode(false);
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"listPresets\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    var list = _store.ListPresets();
                    var sb = new StringBuilder("[");
                    for (int i = 0; i < list.Count; i++)
                    {
                        sb.AppendFormat("\"{0}\"", list[i]);
                        if (i < list.Count - 1) sb.Append(",");
                    }
                    sb.Append("]");
                    SendResponse(reqId, sb.ToString());
                }
                else if (rawMessage.Contains("\"method\":\"savePreset\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string presetName = JsonHelper.ExtractJsonValue(rawMessage, "name");
                    string cfg = JsonHelper.ExtractJsonObject(rawMessage, "config");
                    string curCfgJson = _getConfigJson != null ? _getConfigJson() : "";
                    if (string.IsNullOrEmpty(cfg)) cfg = curCfgJson;
                    else
                    {
                        if (_setConfigJson != null) _setConfigJson(cfg);
                        _store.SaveConfigRaw(cfg);
                    }
                    _store.SavePresetRaw(presetName, cfg);
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"loadPreset\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string presetName = JsonHelper.ExtractJsonValue(rawMessage, "name");
                    string loaded = _store.LoadPresetRaw(presetName);
                    if (!string.IsNullOrEmpty(loaded))
                    {
                        if (_setConfigJson != null) _setConfigJson(loaded);
                        _store.SaveConfigRaw(loaded);
                        _inputLoop.UpdateConfig(JsonHelper.DeserializeConfig(loaded));
                    }
                    SendResponse(reqId, string.IsNullOrEmpty(loaded) ? "null" : loaded);
                }
                else if (rawMessage.Contains("\"method\":\"deletePreset\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string presetName = JsonHelper.ExtractJsonValue(rawMessage, "name");
                    bool deleted = _store.DeletePreset(presetName);
                    SendResponse(reqId, deleted.ToString().ToLower());
                }
                else if (rawMessage.Contains("\"method\":\"setAlwaysOnTop\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    _window.Topmost = JsonHelper.ExtractJsonValue(rawMessage, "alwaysOnTop") == "true";
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"installViGEmDriver\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    bool launched = ViGEmInstaller.LaunchInstaller();
                    SendResponse(reqId, launched.ToString().ToLower());

                    if (launched)
                    {
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            for (int i = 0; i < 60; i++)
                            {
                                Thread.Sleep(1500);
                                if (ViGEmInstaller.IsDriverInstalled())
                                {
                                    VirtualGamepad.Initialize();
                                    if (_broadcaster != null) _broadcaster.Broadcast();
                                    break;
                                }
                            }
                        });
                    }
                }
                else if (rawMessage.Contains("\"method\":\"openExternalUrl\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string url = JsonHelper.ExtractJsonValue(rawMessage, "url");
                    if (!string.IsNullOrEmpty(url))
                    {
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
                    }
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"setWindowMode\""))
                {
                    string reqId = JsonHelper.ExtractJsonValue(rawMessage, "reqId");
                    string mode = JsonHelper.ExtractJsonValue(rawMessage, "mode");
                    WindowController.SetWindowMode(_window, mode);
                    SendResponse(reqId, "true");
                }
                else if (rawMessage.Contains("\"method\":\"minimizeWindow\""))
                {
                    _window.Dispatcher.Invoke(() => { _window.WindowState = WindowState.Minimized; });
                }
                else if (rawMessage.Contains("\"method\":\"closeWindow\""))
                {
                    _window.Dispatcher.Invoke(() => { _window.Close(); });
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("[IpcBridge] HandleMessage error: " + ex.Message);
            }
        }

        public void SendResponse(string reqId, string payloadJson)
        {
            if (_webView == null || _webView.CoreWebView2 == null) return;
            try
            {
                string msg = string.Format("{{\"type\":\"response\",\"reqId\":\"{0}\",\"payload\":{1}}}", reqId, payloadJson);
                _webView.CoreWebView2.PostWebMessageAsJson(msg);
            }
            catch (Exception ex)
            {
                DebugLog.Write("[IpcBridge] SendResponse error: " + ex.Message);
            }
        }
    }
}
