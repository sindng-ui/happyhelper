using System;
using System.Threading;
using System.Windows.Threading;

namespace HappyHelper
{
    /// <summary>
    /// Background diagnostic status poller and broadcaster.
    /// Periodically queries XInput, ViGEm, VirtualGamepad and HidHide statuses and broadcasts to UI.
    /// </summary>
    public class StatusBroadcaster : IDisposable
    {
        private Timer _pollTimer;
        private string _lastPayload = "";
        private readonly GlobalHook _globalListener;
        private readonly Action<string, string> _sendToJs;
        private readonly Dispatcher _dispatcher;

        public StatusBroadcaster(GlobalHook globalListener, Dispatcher dispatcher, Action<string, string> sendToJs)
        {
            _globalListener = globalListener;
            _dispatcher = dispatcher;
            _sendToJs = sendToJs;
        }

        public void Start(int intervalMs = 2000)
        {
            if (_pollTimer == null)
            {
                _pollTimer = new Timer((state) =>
                {
                    Broadcast();
                }, null, 500, intervalMs);
            }
        }

        public void Broadcast()
        {
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
                bool hidHideInstalled = HidHideManager.IsDriverInstalled();
                bool hidHideActive = hidHideInstalled && HidHideManager.GetActive();

                string payload = string.Format(
                    "{{\"xInputLoaded\":{0},\"xinputLoaded\":{0},\"controllerConnected\":{1},\"viGEmInstalled\":{2},\"vigemInstalled\":{2},\"viGEmReady\":{3},\"hidHideInstalled\":{4},\"hidHideActive\":{5}}}",
                    xLoaded.ToString().ToLower(),
                    ctrlConnected.ToString().ToLower(),
                    viGEmInstalled.ToString().ToLower(),
                    viGEmReady.ToString().ToLower(),
                    hidHideInstalled.ToString().ToLower(),
                    hidHideActive.ToString().ToLower());

                if (payload != _lastPayload)
                {
                    _lastPayload = payload;
                    if (_dispatcher != null && _sendToJs != null)
                    {
                        _dispatcher.BeginInvoke((Action)delegate
                        {
                            _sendToJs("pad-status", payload);
                        });
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }
    }
}
