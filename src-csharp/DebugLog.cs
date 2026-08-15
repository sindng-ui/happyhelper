using System;
using System.IO;
using System.Threading;

namespace HappyHelper
{
    /// <summary>
    /// 간단한 파일 로거 - 디버그 로그를 파일에 기록
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
                // Clear old log on startup
                File.WriteAllText(_logPath, "=== HappyHelper Debug Log " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===\r\n");
            }
            catch { _enabled = false; }
        }

        public static void Write(string msg)
        {
            if (!_enabled) return;
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
