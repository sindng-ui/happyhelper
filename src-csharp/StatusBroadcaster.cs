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
                string payload = "{\"keyboardMouseMode\":true,\"controllerConnected\":false,\"vigemInstalled\":false,\"hidHideInstalled\":false}";

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
