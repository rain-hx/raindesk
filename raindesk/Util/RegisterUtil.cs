using raindesk.Constant;
using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace raindesk.Util
{
    public static class RegisterUtil
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void SetAutoStart(bool enable)
        {
            SetSelfStarting(enable, Constants.MY_NAME);
        }

        public static bool IsAutoStart()
        {
            return IsAutoStart(Constants.MY_NAME);
        }

        public static bool IsAutoStart(string appName)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(NormalizeAppName(appName)) is string value
                    && !string.IsNullOrWhiteSpace(value);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "读取开机自启动状态失败");
                return false;
            }
        }

        public static void SetSelfStart(bool enable)
        {
            SetSelfStarting(enable, Constants.MY_NAME);
        }

        public static void SetSelfStarting(bool enable, string appName)
        {
            try
            {
                string valueName = NormalizeAppName(appName);
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
                if (enable)
                {
                    key.SetValue(valueName, Quote(GetExecutablePath()), RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(valueName, false);
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, enable ? "设置开机自启动失败" : "取消开机自启动失败");
            }
        }

        private static string NormalizeAppName(string appName)
        {
            return string.IsNullOrWhiteSpace(appName) ? Constants.MY_NAME : appName.Trim();
        }

        private static string GetExecutablePath()
        {
            string? path = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                path = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                path = AppContext.BaseDirectory.TrimEnd('\\') + "\\" + Constants.MY_NAME + ".exe";
            }

            return path;
        }

        private static string Quote(string path)
        {
            if (path.StartsWith("\"", StringComparison.Ordinal) && path.EndsWith("\"", StringComparison.Ordinal))
            {
                return path;
            }

            return "\"" + path + "\"";
        }
    }
}
