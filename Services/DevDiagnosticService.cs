using System.Reflection;
using System.Text.Json;
using raindesk.Services.Interfaces;
using raindesk.Util;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace raindesk.Services;

/// <summary>
/// Writes runtime snapshots for scripts/dev.ps1 and ai-diagnose.ps1 to consume.
/// Trigger by creating %LOCALAPPDATA%\raindesk\dev\request.json while the app is running.
/// </summary>
public static class DevDiagnosticService
{
    private static DispatcherQueueTimer? _pollTimer;
    private static string DevDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "raindesk", "dev");

    private static string RequestPath => Path.Combine(DevDirectory, "request.json");
    private static string SnapshotPath => Path.Combine(DevDirectory, "snapshot.json");

    public static void Start(DispatcherQueue dispatcherQueue)
    {
        Directory.CreateDirectory(DevDirectory);

        _pollTimer = dispatcherQueue.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromSeconds(1.5);
        _pollTimer.Tick += (_, _) => TryWriteSnapshotIfRequested();
        _pollTimer.Start();

        if (string.Equals(Environment.GetEnvironmentVariable("raindesk_DEV"), "1", StringComparison.OrdinalIgnoreCase))
        {
            WriteSnapshot("startup", null);
        }
    }

    private static void TryWriteSnapshotIfRequested()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        string? requestId = null;
        string reason = "request";
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(RequestPath));
            if (doc.RootElement.TryGetProperty("RequestId", out var requestIdElement))
            {
                requestId = requestIdElement.GetString();
            }

            if (doc.RootElement.TryGetProperty("Reason", out var reasonElement))
            {
                reason = reasonElement.GetString() ?? reason;
            }
        }
        catch
        {
        }

        WriteSnapshot(reason, requestId);

        try
        {
            File.Delete(RequestPath);
        }
        catch
        {
        }
    }

    public static void WriteSnapshot(string reason, string? requestId)
    {
        try
        {
            Directory.CreateDirectory(DevDirectory);

            var snapshot = BuildSnapshot(reason, requestId ?? Guid.NewGuid().ToString("N"));
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SnapshotPath, json);
            LogUtil.WriteLog($"DevDiagnosticService snapshot written: {reason}");
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "DevDiagnosticService snapshot failed");
        }
    }

    private static object BuildSnapshot(string reason, string requestId)
    {
        var appData = AppServices.IsConfigured
            ? AppServices.TryGet<IAppDataProvider>()?.Current
            : null;
        var mainWindow = MainWindow.mainWindow;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var assetsDir = Path.Combine(baseDir, "Assets");

        int menuCount = appData?.MenuList?.Count ?? 0;
        int iconCount = 0;
        if (appData?.MenuList != null)
        {
            foreach (var menu in appData.MenuList)
            {
                iconCount += menu.IconList?.Count ?? 0;
            }
        }

        bool windowVisible = false;
        int windowWidth = 0;
        int windowHeight = 0;
        string windowTitle = string.Empty;
        if (mainWindow != null)
        {
            windowTitle = mainWindow.Title ?? string.Empty;
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
                windowVisible = hwnd != IntPtr.Zero && NativeWindowMethods.IsWindowVisible(hwnd);
            }
            catch
            {
            }

            try
            {
                var size = mainWindow.AppWindow.Size;
                windowWidth = size.Width;
                windowHeight = size.Height;
            }
            catch
            {
            }
        }

        return new
        {
            Schema = "raindesk.dev-snapshot.v1",
            RequestId = requestId,
            Reason = reason,
            CreatedAt = DateTimeOffset.Now.ToString("o"),
            ProcessId = Environment.ProcessId,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            Window = new
            {
                Title = windowTitle,
                Visible = windowVisible,
                Width = windowWidth,
                Height = windowHeight,
                InlineConfigVisible = mainWindow?.InlineConfigPanel?.Visibility == Visibility.Visible,
                InlineTodoVisible = mainWindow?.InlineTodoPanel?.Visibility == Visibility.Visible,
            },
            AppConfig = new
            {
                appData?.AppConfig?.ShowBarIcon,
                appData?.AppConfig?.SelectedMenuIndex,
                appData?.AppConfig?.ShowIconTitle,
                appData?.AppConfig?.AppThemeMode,
                appData?.AppConfig?.AlwaysTopmost,
            },
            Data = new
            {
                MenuCount = menuCount,
                IconCount = iconCount,
                DataPath = Path.Combine(baseDir, "Data"),
                DataExists = File.Exists(Path.Combine(baseDir, "Data")),
            },
            Assets = new
            {
                AssetsPath = assetsDir,
                AppIconExists = File.Exists(Path.Combine(assetsDir, "AppIcon.ico")),
                LogoExists = File.Exists(Path.Combine(assetsDir, "raindeskLogo.png")),
                StoreLogoExists = File.Exists(Path.Combine(assetsDir, "StoreLogo.png")),
            },
            Logs = new
            {
                BaseDirectory = baseDir,
                LocalDevDir = DevDirectory,
            },
            Tray = new
            {
                Configured = appData?.AppConfig?.ShowBarIcon,
                WindowCreated = TrayIcon.Current?.WindowCreated ?? false,
                ShellNotifyIconOk = TrayIcon.Current?.ShellNotifyIconOk ?? false,
                AutoTrayDisabled = TrayIcon.Current?.AutoTrayDisabled ?? false,
            },
            Hotkeys = HotkeyService.CurrentStatus
        };
    }
}
