using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace HappyHelper
{
    public static class ViGEmInstaller
    {
        private const string VIGEM_REG_PATH = @"SYSTEM\CurrentControlSet\Services\ViGEmBus";

        public static bool IsDriverInstalled()
        {
            // 1. Direct ViGEmClient creation check (Ground truth of active driver stack)
            if (TryCreateViGEmClient()) return true;

            // 2. Check kernel device handle directly (\\\\.\\ViGEmBus)
            try
            {
                IntPtr hDev = CreateFile("\\\\.\\ViGEmBus", 0, 0, IntPtr.Zero, 3, 0, IntPtr.Zero); // OPEN_EXISTING = 3
                if (hDev != IntPtr.Zero && hDev.ToInt64() != -1)
                {
                    CloseHandle(hDev);
                    return true;
                }
            }
            catch { }

            // 3. Fallback: If neither active client nor device handle is responding,
            // even if ViGEmBus.sys file or registry key remains as leftover, the driver service is NOT active.
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryCreateViGEmClient()
        {
            try
            {
                using (var client = new Nefarius.ViGEm.Client.ViGEmClient())
                {
                    return client != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool InstallDriverQuiet()
        {
            return LaunchInstaller();
        }

        public static bool LaunchInstaller()
        {

            string tempInstallerPath = null;
            try
            {
                // 1. Try to extract embedded resource installer (100% hidden inside exe)
                var asm = Assembly.GetExecutingAssembly();
                string resourceName = null;
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("ViGEmBus_Setup.exe", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("ViGEmBus_Setup_1.22.0.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = name;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(resourceName))
                {
                    tempInstallerPath = Path.Combine(Path.GetTempPath(), "ViGEmBus_Setup.exe");
                    using (Stream stream = asm.GetManifestResourceStream(resourceName))
                    using (FileStream fs = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, read);
                        }
                    }
                }

                // 2. Fallback to local external file if resource extraction failed
                if (string.IsNullOrEmpty(tempInstallerPath) || !File.Exists(tempInstallerPath))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string localPath = Path.Combine(baseDir, "ViGEmBus_Setup_1.22.0.exe");
                    if (File.Exists(localPath))
                    {
                        tempInstallerPath = localPath;
                    }
                }

                // 3. Fallback to online download if completely missing
                if (string.IsNullOrEmpty(tempInstallerPath) || !File.Exists(tempInstallerPath))
                {
                    tempInstallerPath = Path.Combine(Path.GetTempPath(), "ViGEmBus_Setup_Downloaded.exe");
                    DownloadInstaller(tempInstallerPath);
                }

                if (File.Exists(tempInstallerPath))
                {
                    var psi = new ProcessStartInfo();
                    psi.FileName = tempInstallerPath;
                    psi.Verb = "runas"; // Request Admin Privileges
                    psi.UseShellExecute = true;

                    // Launch completely independently so it doesn't block or close our app
                    var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        // Wait asynchronously in background or return true immediately
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("ViGEm installer launch error: " + ex.Message);
            }
            return false;
        }

        private static void DownloadInstaller(string targetPath)
        {
            try
            {
                string url = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";
                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadFile(url, targetPath);
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("Download installer error: " + ex.Message);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
