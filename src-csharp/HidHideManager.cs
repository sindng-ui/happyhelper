using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HappyHelper
{
    /// <summary>
    /// Direct Win32 / Kernel Driver Bridge for Nefarius HidHide.
    /// Interacts directly with \\.\HidHide to dynamically whitelist HappyHelper.exe
    /// and cloak physical gamepads so that only the ViGEm Virtual Controller is visible to Windows and games.
    /// </summary>
    public static class HidHideManager
    {
        private const string HIDHIDE_CONTROL_DEVICE = @"\\.\HidHide";
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // Official HidHide IOCTL Codes: CTL_CODE(32769, 2048 + N, METHOD_BUFFERED, FILE_READ_DATA)
        // (32769 << 16) | (1 << 14) | ((2048 + N) << 2) | 0
        public const uint IOCTL_GET_WHITELIST = 0x80016000; // Function 2048 (0x800)
        public const uint IOCTL_SET_WHITELIST = 0x80016004; // Function 2049 (0x801)
        public const uint IOCTL_GET_BLACKLIST = 0x80016008; // Function 2050 (0x802)
        public const uint IOCTL_SET_BLACKLIST = 0x8001600C; // Function 2051 (0x803)
        public const uint IOCTL_GET_ACTIVE    = 0x80016010; // Function 2052 (0x804)
        public const uint IOCTL_SET_ACTIVE    = 0x80016014; // Function 2053 (0x805)

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
                    GENERIC_READ | GENERIC_WRITE,
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
            IntPtr hDevice = OpenHidHideDevice(GENERIC_READ | GENERIC_WRITE);
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
        /// Checks if current process is running with elevated Administrator privileges.
        /// </summary>
        public static bool IsAdministrator()
        {
            try
            {
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
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
                    bool ok = DeviceIoControl(hDevice, IOCTL_SET_ACTIVE, inBuf, 1, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        DebugLog.Write("[HidHideManager] SetActive(" + active + ") failed. Win32 ErrorCode=" + err);
                    }
                    return ok;
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
                        // Ensure ViGEm virtual controller is never cloaked
                        if (!IsViGEmVirtualPadInstance(inst))
                        {
                            set.Add(inst);
                        }
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

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            string Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId,
            int DeviceInstanceIdSize,
            out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_ALLCLASSES = 0x00000004;

        /// <summary>
        /// Determines whether a given device instance path belongs to a virtual ViGEm controller.
        /// </summary>
        public static bool IsViGEmVirtualPadInstance(string instId)
        {
            if (string.IsNullOrEmpty(instId)) return false;
            if (instId.IndexOf("ViGEm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            
            // ViGEm creates standard Xbox 360 controller with PID_028E with virtual bus instance IDs
            if (instId.IndexOf("PID_028E", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (instId.EndsWith("\\01") || instId.EndsWith("\\02") || instId.EndsWith("\\03") || instId.EndsWith("\\04") ||
                    instId.Contains("&01") || instId.Contains("&02") || instId.Contains("3&9541963"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Enumerates physical gamepads using SetupAPI and Registry fallback, skipping virtual controllers.
        /// </summary>
        public static List<string> GetConnectedGamepadInstances()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. SetupAPI Enumeration for HID and XUSB/USB devices
            try
            {
                IntPtr hDevInfo = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
                if (hDevInfo != INVALID_HANDLE_VALUE && hDevInfo != IntPtr.Zero)
                {
                    try
                    {
                        SP_DEVINFO_DATA devData = new SP_DEVINFO_DATA();
                        devData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                        uint index = 0;
                        StringBuilder sb = new StringBuilder(1024);

                        while (SetupDiEnumDeviceInfo(hDevInfo, index, ref devData))
                        {
                            int reqSize;
                            if (SetupDiGetDeviceInstanceId(hDevInfo, ref devData, sb, sb.Capacity, out reqSize))
                            {
                                string instId = sb.ToString();
                                if (IsTargetGamepadInstance(instId))
                                {
                                    results.Add(instId);
                                }
                            }
                            index++;
                        }
                    }
                    finally
                    {
                        SetupDiDestroyDeviceInfoList(hDevInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] SetupAPI Enum error: " + ex.Message);
            }

            // 2. Registry Enumeration fallback if SetupAPI didn't catch everything
            try
            {
                string[] rootKeys = new[]
                {
                    @"SYSTEM\CurrentControlSet\Enum\HID",
                    @"SYSTEM\CurrentControlSet\Enum\USB",
                    @"SYSTEM\CurrentControlSet\Enum\BTHENUM"
                };

                foreach (var rk in rootKeys)
                {
                    using (var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(rk))
                    {
                        if (baseKey == null) continue;
                        foreach (var subName in baseKey.GetSubKeyNames())
                        {
                            if (!IsTargetGamepadVid(subName)) continue;
                            if (subName.IndexOf("ViGEm", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                            using (var devKey = baseKey.OpenSubKey(subName))
                            {
                                if (devKey == null) continue;
                                foreach (var instId in devKey.GetSubKeyNames())
                                {
                                    string prefix = rk.Substring(rk.LastIndexOf('\\') + 1) + "\\";
                                    string fullInst = prefix + subName + "\\" + instId;
                                    if (IsTargetGamepadInstance(fullInst) && !results.Contains(fullInst))
                                    {
                                        results.Add(fullInst);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] Registry Enum error: " + ex.Message);
            }

            return new List<string>(results);
        }

        private static bool IsTargetGamepadVid(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.IndexOf("VID_045E", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("VID_054C", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("VID_0E6F", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("VID_046D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("VID_1532", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("VID_24C6", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("VID_2DC8", StringComparison.OrdinalIgnoreCase) >= 0 || // 8BitDo
                   name.IndexOf("VID_057E", StringComparison.OrdinalIgnoreCase) >= 0 || // Nintendo Switch Pro
                   name.IndexOf("VID_2C22", StringComparison.OrdinalIgnoreCase) >= 0 || // Qanba / ThirdParty
                   name.IndexOf("MS_COMP_XUSB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("IG_", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsTargetGamepadInstance(string instId)
        {
            if (string.IsNullOrEmpty(instId)) return false;
            if (IsViGEmVirtualPadInstance(instId)) return false;
            return (instId.StartsWith("HID\\", StringComparison.OrdinalIgnoreCase) || 
                    instId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) || 
                    instId.StartsWith("BTHENUM\\", StringComparison.OrdinalIgnoreCase) ||
                    instId.StartsWith("BTHLEDEVICE\\", StringComparison.OrdinalIgnoreCase)) &&
                   IsTargetGamepadVid(instId);
        }

        /// <summary>
        /// Adds the current executing process (HappyHelper.exe) to the HidHide whitelist if not already present.
        /// Translates Win32 path to DOS Device Path required by HidHide.
        /// </summary>
        public static bool EnsureCurrentAppWhitelisted()
        {
            try
            {
                string exeDosPath = ProcessPathHelper.GetCurrentProcessDosDevicePath();
                if (string.IsNullOrEmpty(exeDosPath)) return false;

                var list = GetWhitelist();
                foreach (var item in list)
                {
                    if (string.Equals(item, exeDosPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Already whitelisted
                    }
                }

                list.Add(exeDosPath);
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
                IntPtr hDevice = CreateFile(
                    HIDHIDE_CONTROL_DEVICE,
                    desiredAccess,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hDevice == INVALID_HANDLE_VALUE || hDevice == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    DebugLog.Write("[HidHideManager] OpenHidHideDevice failed. Access=0x" + desiredAccess.ToString("X8") + " Win32 ErrorCode=" + err + " (IsAdmin=" + IsAdministrator() + ")");
                }
                return hDevice;
            }
            catch (Exception ex)
            {
                DebugLog.Write("[HidHideManager] OpenHidHideDevice exception: " + ex.Message);
                return INVALID_HANDLE_VALUE;
            }
        }

        private static List<string> QueryMultiString(uint ioctlCode)
        {
            List<string> result = new List<string>();
            IntPtr hDevice = OpenHidHideDevice(GENERIC_READ | GENERIC_WRITE);
            if (hDevice == INVALID_HANDLE_VALUE) return result;

            try
            {
                uint bufferSize = 65536; // 64KB direct buffer
                IntPtr outBuf = Marshal.AllocHGlobal((int)bufferSize);
                try
                {
                    uint bytesReturned;
                    if (DeviceIoControl(hDevice, ioctlCode, IntPtr.Zero, 0, outBuf, bufferSize, out bytesReturned, IntPtr.Zero))
                    {
                        if (bytesReturned > 0)
                        {
                            byte[] data = new byte[bytesReturned];
                            Marshal.Copy(outBuf, data, 0, (int)bytesReturned);
                            return DeserializeMultiString(data, (int)bytesReturned);
                        }
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
                    bool ok = DeviceIoControl(hDevice, ioctlCode, inBuf, (uint)rawData.Length, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        DebugLog.Write("[HidHideManager] SendMultiString IOCTL 0x" + ioctlCode.ToString("X8") + " failed. Win32 ErrorCode=" + err + " (IsAdmin=" + IsAdministrator() + ")");
                    }
                    return ok;
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
    /// Helper for retrieving formatted process paths required by HidHide (DOS Device notation e.g. \Device\HarddiskVolumeX\...).
    /// </summary>
    public static class ProcessPathHelper
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        public static string GetCurrentProcessDosDevicePath()
        {
            try
            {
                string fullPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                return ConvertToDosDevicePath(fullPath);
            }
            catch
            {
                string fallback = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\happyhelper.exe";
                return ConvertToDosDevicePath(fallback);
            }
        }

        public static string ConvertToDosDevicePath(string win32Path)
        {
            if (string.IsNullOrEmpty(win32Path)) return win32Path;

            try
            {
                string drive = Path.GetPathRoot(win32Path).TrimEnd('\\'); // e.g. "K:"
                if (drive.Length == 2 && drive[1] == ':')
                {
                    StringBuilder sb = new StringBuilder(1024);
                    uint result = QueryDosDevice(drive, sb, sb.Capacity);
                    if (result > 0)
                    {
                        string dosDevice = sb.ToString(); // e.g. "\Device\HarddiskVolume3"
                        string relative = win32Path.Substring(drive.Length);
                        return dosDevice + relative;
                    }
                }
            }
            catch { }

            return win32Path;
        }
    }
}
