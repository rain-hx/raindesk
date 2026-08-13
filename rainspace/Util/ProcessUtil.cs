using rainspace.Constant;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace rainspace.Util
{
    public static class ProcessUtil
    {
        private const int SwShow = 5;
        private const int SeeMaskNocloseprocess = 0x00000040;
        private const int SeeMaskFlagNoUi = 0x00000400;
        private static readonly Regex UriSchemeRegex = new Regex(
            @"^[a-z][a-z0-9+\-.]*:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static void StartIconApp(IconInfo icon, IconStartType startType)
        {
            try
            {
                if (icon == null) return;
                if (icon.IsFolder) return;

                if (startType == IconStartType.SHOW_IN_EXPLORE)
                {
                    OpenInExplorer(icon);
                    return;
                }

                if (ShouldOpenAsUrl(icon))
                {
                    OpenUrl(icon.Path);
                    return;
                }

                string target = icon.Path;
                if (string.IsNullOrWhiteSpace(target)) return;

                string? verb = startType == IconStartType.ADMIN_STARTUP ? "runas" : null;
                LaunchDetachedProcess(target, icon.StartArg, verb);
                HideAppAfterLaunchIfNeeded();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "启动应用失败");
            }
        }

        private static void HideAppAfterLaunchIfNeeded()
        {
            try
            {
                AppHideType? hideType = AppServices.IsConfigured
                    ? AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig.AppHideType
                    : null;
                if (hideType == AppHideType.START_EXE)
                {
                    MainWindow.HideAppStatic();
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "启动后隐藏面板失败");
            }
        }

        private static void OpenUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            LaunchDetachedProcess(NormalizeUrlForLaunch(url), null, null);
            HideAppAfterLaunchIfNeeded();
        }

        internal static bool ShouldOpenAsUrl(IconInfo icon)
        {
            if (icon == null || icon.IconType != IconType.URL)
            {
                return false;
            }

            string path = NormalizeLocalPath(icon.Path);
            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            return !File.Exists(path) && !Directory.Exists(path);
        }

        internal static string NormalizeUrlForLaunch(string url)
        {
            string normalizedUrl = (url ?? "").Trim();
            if (HasUriScheme(normalizedUrl))
            {
                return normalizedUrl;
            }

            return "https://" + normalizedUrl;
        }

        private static void OpenInExplorer(IconInfo icon)
        {
            string path = !string.IsNullOrWhiteSpace(icon.LnkPath) ? icon.LnkPath : icon.Path;
            if (string.IsNullOrWhiteSpace(path)) return;

            if (File.Exists(path))
            {
                LaunchDetachedProcess("explorer.exe", $"/select,\"{path}\"", null);
                return;
            }

            if (Directory.Exists(path))
            {
                LaunchDetachedProcess(path, null, null);
            }
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        public static void ReStartApp()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath))
                {
                    LaunchDetachedProcess(exePath, null, null);
                }
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "重启应用失败");
            }
        }

        private static void LaunchDetachedProcess(string fileName, string? arguments, string? verb)
        {
            TryGetExplorerLaunchTarget(fileName, arguments, verb, out string launchFile, out string? launchArgs, out string? launchVerb);

            var sei = new ShellExecuteInfo
            {
                cbSize = Marshal.SizeOf<ShellExecuteInfo>(),
                fMask = SeeMaskNocloseprocess | SeeMaskFlagNoUi,
                lpVerb = string.IsNullOrWhiteSpace(launchVerb) ? "open" : launchVerb,
                lpFile = launchFile,
                lpParameters = string.IsNullOrWhiteSpace(launchArgs) ? null : launchArgs,
                nShow = SwShow
            };

            if (!ShellExecuteEx(ref sei))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"ShellExecuteEx failed ({error}) for '{launchFile}'.");
            }

            if (sei.hProcess != IntPtr.Zero)
            {
                CloseHandle(sei.hProcess);
            }
        }

        private static bool TryGetExplorerLaunchTarget(
            string fileName,
            string? arguments,
            string? verb,
            out string launchFile,
            out string? launchArgs,
            out string? launchVerb)
        {
            launchFile = fileName;
            launchArgs = arguments;
            launchVerb = verb;

            if (!string.IsNullOrWhiteSpace(verb)
                && !string.Equals(verb, "open", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (fileName.Contains("://", StringComparison.Ordinal))
            {
                return false;
            }

            string normalizedPath = NormalizeLocalPath(fileName);
            if (!File.Exists(normalizedPath) || !ShouldLaunchViaExplorer(normalizedPath))
            {
                return false;
            }

            launchFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            launchArgs = string.IsNullOrWhiteSpace(arguments)
                ? $"\"{normalizedPath}\""
                : $"\"{normalizedPath}\" {arguments}";
            launchVerb = "open";
            return true;
        }

        private static bool ShouldLaunchViaExplorer(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".com", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".msc", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".cpl", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLocalPath(string? path)
        {
            return (path ?? "").Trim().Trim('"');
        }

        private static bool HasUriScheme(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Length >= 3
                && char.IsLetter(value[0])
                && value[1] == ':'
                && (value[2] == '\\' || value[2] == '/'))
            {
                return false;
            }

            return UriSchemeRegex.IsMatch(value);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShellExecuteInfo
        {
            public int cbSize;
            public int fMask;
            public IntPtr hwnd;
            public string? lpVerb;
            public string? lpFile;
            public string? lpParameters;
            public string? lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            public string? lpClass;
            public IntPtr hkeyClass;
            public int dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
    }
}
