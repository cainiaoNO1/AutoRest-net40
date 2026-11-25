using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace AutoRest.Services
{
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "AutoRest";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    var val = key.GetValue(AppName) as string;
                    return !string.IsNullOrEmpty(val);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutoStart(bool enable, bool startMinimized = false)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;

                    if (enable)
                    {
                        string exePath = Process.GetCurrentProcess().MainModule?.FileName;
                        if (string.IsNullOrEmpty(exePath))
                        {
                            exePath = Assembly.GetExecutingAssembly().Location;
                        }

                        string command = $"\"{exePath}\"";
                        if (startMinimized)
                        {
                            command += " --minimized";
                        }
                        key.SetValue(AppName, command);
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch
            {
                // 静默处理注册表权限问题
            }
        }
    }
}
