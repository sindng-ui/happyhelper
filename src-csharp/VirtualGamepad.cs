using System;
using System.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace HappyHelper
{
    public static class VirtualGamepad
    {
        private static ViGEmClient _client;
        private static IXbox360Controller _controller;
        private static bool _isReady = false;
        private static readonly object _syncLock = new object();

        public static bool IsReady
        {
            get { return _isReady; }
        }

        public static int UserIndex
        {
            get
            {
                if (_controller == null) return -1;
                try { return _controller.UserIndex; }
                catch { return -1; }
            }
        }

        public static bool Initialize()
        {
            lock (_syncLock)
            {
                if (_isReady && _controller != null) return true;

                try
                {
                    if (_client == null)
                    {
                        _client = new ViGEmClient();
                    }

                    if (_controller == null)
                    {
                        _controller = _client.CreateXbox360Controller();
                        _controller.Connect();
                    }

                    _isReady = true;
                    int slot = -1;
                    try { slot = _controller.UserIndex; } catch { }
                    DebugLog.Write(string.Format("[VirtualGamepad] SUCCESS: Connected (Slot=#{0})", slot >= 0 ? slot.ToString() : "Assigned by OS"));
                    return true;
                }
                catch (Exception ex)
                {
                    DebugLog.Write("[VirtualGamepad] Init failed: " + ex.Message);
                    _isReady = false;
                    return false;
                }
            }
        }


        public static void Shutdown()
        {
            lock (_syncLock)
            {
                try
                {
                    if (_controller != null)
                    {
                        _controller.Disconnect();
                        _controller = null;
                    }
                    if (_client != null)
                    {
                        _client.Dispose();
                        _client = null;
                    }
                }
                catch { }
                _isReady = false;
            }
        }

        public static void SendAction(int padCode)
        {
            if (!_isReady)
            {
                if (!Initialize()) return;
            }

            // Route through GamepadPassthrough for seamless physical + virtual merging
            if (GamepadPassthrough.IsRunning)
            {
                GamepadPassthrough.PulseAction(padCode);
                return;
            }

            lock (_syncLock)
            {
                try
                {
                    switch (padCode)
                    {
                        case 2007: // LT (Left Trigger)
                            SendTrigger(Xbox360Slider.LeftTrigger, 255);
                            break;
                        case 2008: // RT (Right Trigger)
                            SendTrigger(Xbox360Slider.RightTrigger, 255);
                            break;
                        case 2001: // A Button
                            SendButton(Xbox360Button.A);
                            break;
                        case 2002: // B Button
                            SendButton(Xbox360Button.B);
                            break;
                        case 2003: // X Button
                            SendButton(Xbox360Button.X);
                            break;
                        case 2004: // Y Button
                            SendButton(Xbox360Button.Y);
                            break;
                        case 2005: // LB Button
                            SendButton(Xbox360Button.LeftShoulder);
                            break;
                        case 2006: // RB Button
                            SendButton(Xbox360Button.RightShoulder);
                            break;
                        case 2009: // D-Pad Up
                            SendButton(Xbox360Button.Up);
                            break;
                        case 2010: // D-Pad Down
                            SendButton(Xbox360Button.Down);
                            break;
                        case 2011: // D-Pad Left
                            SendButton(Xbox360Button.Left);
                            break;
                        case 2012: // D-Pad Right
                            SendButton(Xbox360Button.Right);
                            break;
                        case 2013: // LS (Left Stick Click)
                            SendButton(Xbox360Button.LeftThumb);
                            break;
                        case 2014: // RS (Right Stick Click)
                            SendButton(Xbox360Button.RightThumb);
                            break;
                        case 2015: // View / Back
                            SendButton(Xbox360Button.Back);
                            break;
                        case 2016: // Menu / Start
                            SendButton(Xbox360Button.Start);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.Write("[VirtualGamepad] SendAction error: " + ex.Message);
                }
            }
        }

        public static void SubmitFullState(ushort buttons, byte lt, byte rt, short lx, short ly, short rx, short ry)
        {
            if (!_isReady || _controller == null) return;

            lock (_syncLock)
            {
                try
                {
                    _controller.SetButtonState(Xbox360Button.A, (buttons & 0x1000) != 0);
                    _controller.SetButtonState(Xbox360Button.B, (buttons & 0x2000) != 0);
                    _controller.SetButtonState(Xbox360Button.X, (buttons & 0x4000) != 0);
                    _controller.SetButtonState(Xbox360Button.Y, (buttons & 0x8000) != 0);
                    _controller.SetButtonState(Xbox360Button.LeftShoulder, (buttons & 0x0100) != 0);
                    _controller.SetButtonState(Xbox360Button.RightShoulder, (buttons & 0x0200) != 0);
                    _controller.SetButtonState(Xbox360Button.Back, (buttons & 0x0020) != 0);
                    _controller.SetButtonState(Xbox360Button.Start, (buttons & 0x0010) != 0);
                    _controller.SetButtonState(Xbox360Button.LeftThumb, (buttons & 0x0040) != 0);
                    _controller.SetButtonState(Xbox360Button.RightThumb, (buttons & 0x0080) != 0);
                    _controller.SetButtonState(Xbox360Button.Up, (buttons & 0x0001) != 0);
                    _controller.SetButtonState(Xbox360Button.Down, (buttons & 0x0002) != 0);
                    _controller.SetButtonState(Xbox360Button.Left, (buttons & 0x0004) != 0);
                    _controller.SetButtonState(Xbox360Button.Right, (buttons & 0x0008) != 0);

                    _controller.SetSliderValue(Xbox360Slider.LeftTrigger, lt);
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, rt);

                    _controller.SetAxisValue(Xbox360Axis.LeftThumbX, lx);
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ly);
                    _controller.SetAxisValue(Xbox360Axis.RightThumbX, rx);
                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, ry);

                    _controller.SubmitReport();
                    DebugLog.Write(string.Format("[VirtualGamepad] SubmitFullState: BTN=0x{0:X4}, LT={1}, RT={2}, LX={3}, LY={4}", buttons, lt, rt, lx, ly));
                }
                catch (Exception ex)
                {
                    DebugLog.Write("[VirtualGamepad] SubmitFullState error: " + ex.Message);
                }


            }
        }


        private static void SendTrigger(Xbox360Slider slider, byte val)
        {
            if (_controller == null)
            {
                DebugLog.Write("[VirtualGamepad] SendTrigger failed: _controller is null");
                return;
            }
            DebugLog.Write(string.Format("[VirtualGamepad] SendTrigger {0}={1} DOWN", slider, val));
            _controller.SetSliderValue(slider, val);
            _controller.SubmitReport();

            Thread.Sleep(100);

            _controller.SetSliderValue(slider, 0);
            _controller.SubmitReport();
            DebugLog.Write(string.Format("[VirtualGamepad] SendTrigger {0} UP", slider));
        }

        private static void SendButton(Xbox360Button button)
        {
            if (_controller == null)
            {
                DebugLog.Write("[VirtualGamepad] SendButton failed: _controller is null");
                return;
            }
            DebugLog.Write(string.Format("[VirtualGamepad] SendButton {0} DOWN", button));
            _controller.SetButtonState(button, true);
            _controller.SubmitReport();

            Thread.Sleep(100);

            _controller.SetButtonState(button, false);
            _controller.SubmitReport();
            DebugLog.Write(string.Format("[VirtualGamepad] SendButton {0} UP", button));
        }
    }
}
