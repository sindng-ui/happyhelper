using System;
using System.Windows;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    /// <summary>
    /// Manages WPF Window interactions, frameless window dragging, mini-HUD mode toggling,
    /// and mouse hit-testing for the application window.
    /// </summary>
    public static class WindowController
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private const uint WM_NCLBUTTONDOWN = 0xA1;
        private const uint HTCAPTION = 0x2;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Initiates Win32 Native window dragging for frameless WPF windows.
        /// </summary>
        public static void DragWindow(IntPtr hwnd)
        {
            try
            {
                ReleaseCapture();
                SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
            catch { }
        }

        /// <summary>
        /// Checks if current mouse cursor position is within the application window bounds.
        /// </summary>
        public static bool IsMouseOverWindow(Window window)
        {
            if (window == null) return false;
            try
            {
                POINT pt;
                if (GetCursorPos(out pt))
                {
                    double left = window.Left;
                    double top = window.Top;
                    double right = left + window.Width;
                    double bottom = top + window.Height;

                    return (pt.X >= left && pt.X <= right && pt.Y >= top && pt.Y <= bottom);
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Switches window between standard Full view (420x730) and compact Mini-HUD (195x46).
        /// </summary>
        public static void SetWindowMode(Window window, string mode)
        {
            if (window == null) return;
            window.Dispatcher.Invoke(new Action(() =>
            {
                if (mode == "mini")
                {
                    window.MinWidth = 180;
                    window.MinHeight = 40;
                    window.Width = 195;
                    window.Height = 46;
                    window.ResizeMode = ResizeMode.NoResize;
                }
                else
                {
                    window.MinWidth = 390;
                    window.MinHeight = 680;
                    window.Width = 420;
                    window.Height = 730;
                    window.ResizeMode = ResizeMode.CanResize;
                }
            }));
        }
    }
}
