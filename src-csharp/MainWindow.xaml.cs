using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace HappyHelper
{
    /// <summary>
    /// Main Application Window Coordinator.
    /// Thin orchestrator connecting WebView2 UI, IPC Bridge, Input Loops, and Status Services.
    /// </summary>
    public class MainWindow : Window
    {
        private ConfigManager _store;
        private LoopRunner _inputLoop;
        private GlobalHook _globalListener;
        private StatusBroadcaster _broadcaster;
        private IpcBridge _ipc;

        private string _currentConfigJson;
        private bool _bindingMode = false;
        private bool _webViewReady = false;
        private WebView2 webView;

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

            _broadcaster = new StatusBroadcaster(_globalListener, this.Dispatcher, SendToJs);

            _currentConfigJson = _store.LoadConfigRaw();
            if (string.IsNullOrEmpty(_currentConfigJson) || !_currentConfigJson.Trim().StartsWith("{") || !_currentConfigJson.Trim().EndsWith("}"))
            {
                _currentConfigJson = JsonHelper.SerializeConfig(AppConfig.CreateDefault());
                _store.SaveConfigRaw(_currentConfigJson);
            }

            _ipc = new IpcBridge(
                this,
                this.webView,
                _store,
                _inputLoop,
                _globalListener,
                _broadcaster,
                (mode) => _bindingMode = mode,
                () => _currentConfigJson,
                (cfg) => _currentConfigJson = cfg
            );

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
            this.webView = new WebView2();
            this.webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.webView.VerticalAlignment = VerticalAlignment.Stretch;
            grid.Children.Add(this.webView);
            this.Content = grid;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView.CoreWebView2.WebMessageReceived += (s, args) =>
                {
                    _ipc.HandleMessage(args.TryGetWebMessageAsString());
                };

                _webViewReady = true;

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "renderer", "index.html");
                if (!File.Exists(htmlPath))
                {
                    string altPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "renderer", "index.html");
                    if (File.Exists(altPath)) htmlPath = altPath;
                }

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.NavigationCompleted += (s, args) =>
                    {
                        _broadcaster.Broadcast();
                        _broadcaster.Start(2000);
                    };
                    webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    MessageBox.Show("renderer/index.html not found: " + htmlPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 init failed: " + ex.Message);
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _webViewReady = false;
            if (_broadcaster != null)
            {
                _broadcaster.Dispose();
                _broadcaster = null;
            }
            _inputLoop.Stop();
            _globalListener.Stop();
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

        private void OnGlobalKeyPressed(int keyCode, bool isMouse)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                try
                {
                    if (_bindingMode)
                    {
                        if (keyCode == 1) // ESC key: cancel binding
                        {
                            _bindingMode = false;
                            TrySendToJs("key-bound-cancel", "{}");
                            return;
                        }

                        if (keyCode == 1001) // Left Click: ignored for hotkey binding
                        {
                            return;
                        }

                        var payload = string.Format("{{\"keyCode\":{0},\"isMouse\":{1}}}",
                            keyCode, isMouse.ToString().ToLower());
                        bool sent = TrySendToJs("key-bound", payload);
                        if (sent) _bindingMode = false;
                        return;
                    }

                    var currentCfg = JsonHelper.DeserializeConfig(_currentConfigJson);
                    int startCode = (currentCfg != null && currentCfg.startKey != null) ? currentCfg.startKey.keyCode : 63;
                    int stopCode = (currentCfg != null && currentCfg.stopKey != null) ? currentCfg.stopKey.keyCode : 64;

                    if (startCode == stopCode && keyCode == startCode)
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
                    DebugLog.Write("[MainWindow] OnGlobalKeyPressed error: " + ex.Message);
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
                DebugLog.Write("[MainWindow] TrySendToJs error: " + ex.Message);
                return false;
            }
        }

        private void SendToJs(string type, string payloadJson)
        {
            TrySendToJs(type, payloadJson);
        }
    }
}
