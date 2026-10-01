using System;
using System.IO;
using System.Threading;

namespace HappyHelper
{
    /// <summary>
    /// File Logger - Writes diagnostic event logs safely with thread-safe file appending.
    /// </summary>
    public static class DebugLog
    {
        private static readonly string _logPath;
        private static readonly object _lock = new object();
        private static bool _enabled = true;

        static DebugLog()
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                _logPath = Path.Combine(dir, "debug_log.txt");
            }
            catch { _enabled = false; }
        }

        public static void Write(string msg)
        {
            if (!_enabled || string.IsNullOrEmpty(_logPath)) return;
            try
            {
                string line = string.Format("[{0}][T{1}] {2}\r\n",
                    DateTime.Now.ToString("HH:mm:ss.fff"),
                    Thread.CurrentThread.ManagedThreadId,
                    msg);
                lock (_lock)
                {
                    File.AppendAllText(_logPath, line);
                }
            }
            catch { }
        }
    }
}
