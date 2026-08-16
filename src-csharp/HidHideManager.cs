using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    /// <summary>
    /// Pure C# Win32 Kernel IOCTL Interface for Nefarius HidHide Filter Driver.
    /// Provides Application Whitelisting, Device Blacklisting (Cloaking), and Active Toggle
    /// without requiring external third-party managed DLL dependencies.
    /// </summary>
    public static class HidHideManager
    {
        private const string HIDHIDE_CONTROL_DEVICE = @"\\.\HidHide";

        // Standard Win32 DesiredAccess and ShareMode flags
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // HidHide IOCTL Function codes (Base DeviceType: 0x8001, Access: READ/WRITE, Method: BUFFERED)
        // CTL_CODE(0x8001, 0x800 + N, METHOD_BUFFERED, FILE_READ_DATA [| FILE_WRITE_DATA])
        public const uint IOCTL_GET_WHITELIST = 0x80016000; // Function 0x800
        public const uint IOCTL_SET_WHITELIST = 0x8001E004; // Function 0x801
        public const uint IOCTL_GET_BLACKLIST = 0x80016008; // Function 0x802
        public const uint IOCTL_SET_BLACKLIST = 0x8001E00C; // Function 0x803
        public const uint IOCTL_GET_ACTIVE    = 0x80016010; // Function 0x804
        public const uint IOCTL_SET_ACTIVE    = 0x8001E014; // Function 0x805

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Checks if the HidHide kernel filter driver is installed and accessible.
        /// </summary>
        public static bool IsDriverInstalled()
        {
            try
            {
                IntPtr hDevice = CreateFile(
                    HIDHIDE_CONTROL_DEVICE,
                    GENERIC_READ,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hDevice == INVALID_HANDLE_VALUE || hDevice == IntPtr.Zero)
                {
                    return false;
                }

                CloseHandle(hDevice);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Serializes a list of strings into a UTF-16LE Multi-String (Null-delimited with double-null termination) byte array.
        /// </summary>
        public static byte[] SerializeMultiString(IEnumerable<string> items)
        {
            if (items == null) return new byte[] { 0, 0, 0, 0 };

            List<byte> bytes = new List<byte>();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item)) continue;
                byte[] strBytes = Encoding.Unicode.GetBytes(item);
                bytes.AddRange(strBytes);
                bytes.Add(0); // Null terminator wchar
                bytes.Add(0);
            }

            // Final additional null terminator wchar to mark end of multi-string
            bytes.Add(0);
            bytes.Add(0);

            return bytes.ToArray();
        }

        /// <summary>
        /// Deserializes a UTF-16LE Multi-String byte array into a list of strings.
        /// </summary>
        public static List<string> DeserializeMultiString(byte[] buffer, int length)
        {
            List<string> result = new List<string>();
            if (buffer == null || length <= 0) return result;

            int actualLen = Math.Min(buffer.Length, length);
            string fullText = Encoding.Unicode.GetString(buffer, 0, actualLen);
            string[] rawParts = fullText.Split('\0');

            foreach (var part in rawParts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    result.Add(part.Trim());
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the current Application Whitelist from HidHide.
        /// </summary>
        public static List<string> GetWhitelist()
        {
            return QueryMultiString(IOCTL_GET_WHITELIST);
        }

        /// <summary>
        /// Sets the Application Whitelist in HidHide.
        /// </summary>
        public static bool SetWhitelist(IEnumerable<string> appPaths)
        {
            return SendMultiString(IOCTL_SET_WHITELIST, appPaths);
        }

        /// <summary>
        /// Gets the current Device Blacklist (Cloaked device instances) from HidHide.
        /// </summary>
        public static List<string> GetBlacklist()
        {
            return QueryMultiString(IOCTL_GET_BLACKLIST);
        }

        /// <summary>
        /// Sets the Device Blacklist (Cloaked device instances) in HidHide.
        /// </summary>
        public static bool SetBlacklist(IEnumerable<string> deviceInstanceIds)
        {
            return SendMultiString(IOCTL_SET_BLACKLIST, deviceInstanceIds);
        }

        /// <summary>
        /// Checks if HidHide Cloaking (Hiding) is currently globally active.
        /// </summary>
        public static bool GetActive()
        {
            IntPtr hDevice = OpenHidHideDevice(GENERIC_READ);
            if (hDevice == INVALID_HANDLE_VALUE) return false;

            try
            {
                IntPtr outBuf = Marshal.AllocHGlobal(1);
                try
                {
                    uint bytesReturned;
                    bool ok = DeviceIoControl(hDevice, IOCTL_GET_ACTIVE, IntPtr.Zero, 0, outBuf, 1, out bytesReturned, IntPtr.Zero);
                    if (ok && bytesReturned >= 1)
                    {
                        byte val = Marshal.ReadByte(outBuf);
                        return val != 0;
                    }
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(outBuf);
                }
            }
            finally
            {
                CloseHandle(hDevice);
            }
        }

        /// <summary>
        /// Enables or disables HidHide Cloaking globally.
        /// </summary>
        public static bool SetActive(bool active)
        {
            IntPtr hDevice = OpenHidHideDevice(GENERIC_READ | GENERIC_WRITE);
            if (hDevice == INVALID_HANDLE_VALUE) return false;

            try
            {
                IntPtr inBuf = Marshal.AllocHGlobal(1);
                try
                {
                    Marshal.WriteByte(inBuf, (byte)(active ? 1 : 0));
                    uint bytesReturned;
                    return DeviceIoControl(hDevice, IOCTL_SET_ACTIVE, inBuf, 1, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
                }
                finally
                {
                    Marshal.FreeHGlobal(inBuf);
                }
            }
            finally
            {
                CloseHandle(hDevice);
            }
        }

        /// <summary>
        /// Automatically cloaks physical gamepads to guarantee exclusive Slot #0 for virtual gamepad.
        /// </summary>
        public static bool AutoCloakConnectedGamepads()
        {
            if (!IsDriverInstalled()) return false;
            try
            {
                EnsureCurrentAppWhitelisted();
                var instances = GetConnectedGamepadInstances();
                if (instances.Count > 0)
                {
                    var currentBl = GetBlacklist();
                    var set = new HashSet<string>(currentBl, StringComparer.OrdinalIgnoreCase);
                    foreach (var inst in instances)
                    {
                        set.Add(inst);
                    }
                    SetBlacklist(set);
                }
                SetActive(true);
                DebugLog.Write("[HidHideManager] AutoCloakConnectedGamepads: Cloaking ACTIVE with " + instances.Count + " devices.");
                return true;
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] AutoCloakConnectedGamepads error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Uncloaks all physical gamepads and restores them to Windows and games.
        /// </summary>
        public static bool UncloakAllGamepads()
        {
            if (!IsDriverInstalled()) return false;
            try
            {
                SetActive(false);
                DebugLog.Write("[HidHideManager] UncloakAllGamepads: Cloaking INACTIVE.");
                return true;
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] UncloakAllGamepads error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Enumerates physical gamepads from Windows registry, skipping virtual controllers.
        /// </summary>
        public static List<string> GetConnectedGamepadInstances()
        {
            var results = new List<string>();
            try
            {
                string[] rootKeys = new[]
                {
                    @"SYSTEM\CurrentControlSet\Enum\HID",
                    @"SYSTEM\CurrentControlSet\Enum\USB"
                };

                foreach (var rk in rootKeys)
                {
                    using (var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(rk))
                    {
                        if (baseKey == null) continue;
                        foreach (var subName in baseKey.GetSubKeyNames())
                        {
                            // Filter common gamepad VID patterns: Microsoft (045E), Sony (054C), PDP (0E6F), Logitech (046D), Razer (1532)
                            bool isPadVid = subName.IndexOf("VID_045E", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            subName.IndexOf("VID_054C", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            subName.IndexOf("VID_0E6F", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            subName.IndexOf("VID_046D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            subName.IndexOf("VID_1532", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            subName.IndexOf("VID_24C6", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            subName.IndexOf("MS_COMP_XUSB", StringComparison.OrdinalIgnoreCase) >= 0;

                            if (!isPadVid) continue;
                            if (subName.IndexOf("ViGEm", StringComparison.OrdinalIgnoreCase) >= 0) continue; // Skip ViGEm virtual pad

                            using (var devKey = baseKey.OpenSubKey(subName))
                            {
                                if (devKey == null) continue;
                                foreach (var instId in devKey.GetSubKeyNames())
                                {
                                    string fullInst = (rk.EndsWith("HID") ? "HID\\" : "USB\\") + subName + "\\" + instId;
                                    results.Add(fullInst);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] GetConnectedGamepadInstances error: " + ex.Message);
            }
            return results;
        }

        /// <summary>
        /// Adds the current executing process (HappyHelper.exe) to the HidHide whitelist if not already present.
        /// </summary>
        public static bool EnsureCurrentAppWhitelisted()
        {
            try
            {
                string exePath = ProcessPathHelper.GetCurrentProcessDosPath();
                if (string.IsNullOrEmpty(exePath)) return false;

                var list = GetWhitelist();
                foreach (var item in list)
                {
                    if (string.Equals(item, exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Already whitelisted
                    }
                }

                list.Add(exePath);
                return SetWhitelist(list);
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] EnsureCurrentAppWhitelisted error: " + ex.Message);
                return false;
            }
        }

        private static IntPtr OpenHidHideDevice(uint desiredAccess)
        {
            try
            {
                return CreateFile(
                    HIDHIDE_CONTROL_DEVICE,
                    desiredAccess,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);
            }
            catch
            {
                return INVALID_HANDLE_VALUE;
            }
        }

        private static List<string> QueryMultiString(uint ioctlCode)
        {
            List<string> result = new List<string>();
            IntPtr hDevice = OpenHidHideDevice(GENERIC_READ);
            if (hDevice == INVALID_HANDLE_VALUE) return result;

            try
            {
                // First call: probe required buffer size
                uint requiredSize = 0;
                DeviceIoControl(hDevice, ioctlCode, IntPtr.Zero, 0, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);
                if (requiredSize == 0) return result;

                IntPtr outBuf = Marshal.AllocHGlobal((int)requiredSize);
                try
                {
                    uint bytesReturned;
                    if (DeviceIoControl(hDevice, ioctlCode, IntPtr.Zero, 0, outBuf, requiredSize, out bytesReturned, IntPtr.Zero))
                    {
                        byte[] data = new byte[bytesReturned];
                        Marshal.Copy(outBuf, data, 0, (int)bytesReturned);
                        return DeserializeMultiString(data, (int)bytesReturned);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(outBuf);
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] QueryMultiString error: " + ex.Message);
            }
            finally
            {
                CloseHandle(hDevice);
            }

            return result;
        }

        private static bool SendMultiString(uint ioctlCode, IEnumerable<string> items)
        {
            IntPtr hDevice = OpenHidHideDevice(GENERIC_READ | GENERIC_WRITE);
            if (hDevice == INVALID_HANDLE_VALUE) return false;

            try
            {
                byte[] rawData = SerializeMultiString(items);
                IntPtr inBuf = Marshal.AllocHGlobal(rawData.Length);
                try
                {
                    Marshal.Copy(rawData, 0, inBuf, rawData.Length);
                    uint bytesReturned;
                    return DeviceIoControl(hDevice, ioctlCode, inBuf, (uint)rawData.Length, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
                }
                finally
                {
                    Marshal.FreeHGlobal(inBuf);
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] SendMultiString error: " + ex.Message);
                return false;
            }
            finally
            {
                CloseHandle(hDevice);
            }
        }
    }

    /// <summary>
    /// Helper for retrieving formatted process paths required by HidHide.
    /// </summary>
    public static class ProcessPathHelper
    {
        public static string GetCurrentProcessDosPath()
        {
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\happyhelper.exe";
            }
        }
    }
}
