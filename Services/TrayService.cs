using System;
using System.Threading;
using raindesk.Util;
using raindesk.Features.Appearance.Infrastructure;
using raindesk.Features.Tray.Contracts;

namespace raindesk.Services;

/// <summary>
/// 系统托盘图标服务。
/// 从 MainWindow 提取的托盘图标管理逻辑。
/// </summary>
public sealed class TrayService : ITrayController
{
    private TrayIcon? _trayIcon;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    /// <summary>
    /// 托盘左键点击回调（在 UI 线程执行）。
    /// </summary>
    public Action? OnShowRequested { get; set; }

    /// <summary>
    /// 托盘右键菜单"打开面板"回调。
    /// </summary>
    public Action? OnOpenPanel { get; set; }

    /// <summary>
    /// 托盘右键菜单"备份数据"回调。
    /// </summary>
    public Func<System.Threading.Tasks.Task>? OnExportBackup { get; set; }

    /// <summary>
    /// 托盘右键菜单"导入备份"回调。
    /// </summary>
    public Func<System.Threading.Tasks.Task>? OnImportBackup { get; set; }

    /// <summary>
    /// 托盘右键菜单"退出"回调。
    /// </summary>
    public Action? OnExitRequested { get; set; }

    public TrayService(Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// 初始化托盘图标。如果配置禁用托盘图标，则不创建。
    /// </summary>
    public void Initialize(bool showBarIcon)
    {
        if (!showBarIcon)
        {
            LogUtil.WriteLog("TrayService.Initialize skipped: ShowBarIcon=false");
            return;
        }

        try
        {
            DisposeIcon();
            _trayIcon = new TrayIcon("raindesk", HandleLeftClick, HandleRightClick);
            LogUtil.WriteLog("TrayService initialized");
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "创建托盘图标失败");
        }
    }

    /// <summary>
    /// 刷新托盘图标（重新创建）。
    /// </summary>
    public void Refresh(bool showBarIcon)
    {
        Initialize(showBarIcon);
    }

    public void Dispose()
    {
        DisposeIcon();
    }

    private void DisposeIcon()
    {
        var icon = Interlocked.Exchange(ref _trayIcon, null);
        icon?.Dispose();
    }

    private void HandleLeftClick()
    {
        _dispatcher.TryEnqueue(() => OnShowRequested?.Invoke());
    }

    private void HandleRightClick()
    {
        _dispatcher.TryEnqueue(() =>
        {
            try
            {
                ShowContextMenu();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "显示托盘菜单失败");
            }
        });
    }

    private void ShowContextMenu()
    {
        NativeWindowMethods.GetCursorPos(out NativeWindowMethods.POINT screenPos);
        IntPtr menu = NativeWindowMethods.CreatePopupMenu();
        if (menu == IntPtr.Zero) return;

        const uint MF_STRING = 0x0000;
        const uint MF_SEPARATOR = 0x0800;
        const uint TPM_RIGHTBUTTON = 0x0002;
        const uint TPM_RETURNCMD = 0x0100;

        const int openPanel = 1;
        const int exportBackup = 2;
        const int importBackup = 3;
        const int openAppDir = 4;
        const int restartApp = 5;
        const int exitApp = 6;

        NativeWindowMethods.AppendMenu(menu, MF_STRING, (UIntPtr)openPanel, "打开面板");
        NativeWindowMethods.AppendMenu(menu, MF_STRING, (UIntPtr)exportBackup, "备份数据");
        NativeWindowMethods.AppendMenu(menu, MF_STRING, (UIntPtr)importBackup, "导入备份");
        NativeWindowMethods.AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
        NativeWindowMethods.AppendMenu(menu, MF_STRING, (UIntPtr)openAppDir, "程序目录");
        NativeWindowMethods.AppendMenu(menu, MF_STRING, (UIntPtr)restartApp, "重启应用");
        NativeWindowMethods.AppendMenu(menu, MF_STRING, (UIntPtr)exitApp, "退出应用");

        // 使用主窗口 HWND；MainWindow 未注册到 DI，需通过静态实例获取
        var window = MainWindow.mainWindow;
        IntPtr hwnd = window != null
            ? WinRT.Interop.WindowNative.GetWindowHandle(window)
            : _trayIcon?.WindowHandle ?? NativeWindowMethods.GetForegroundWindow();

        if (hwnd == IntPtr.Zero) return;

        NativeWindowMethods.ActivateWindow(hwnd);
        int command = NativeWindowMethods.TrackPopupMenu(
            menu,
            TPM_RIGHTBUTTON | TPM_RETURNCMD,
            screenPos.X,
            screenPos.Y,
            0,
            hwnd,
            IntPtr.Zero);
        NativeWindowMethods.DestroyMenu(menu);

        switch (command)
        {
            case openPanel:
                OnOpenPanel?.Invoke();
                break;
            case exportBackup:
                _ = OnExportBackup?.Invoke();
                break;
            case importBackup:
                OnOpenPanel?.Invoke();
                _ = OnImportBackup?.Invoke();
                break;
            case openAppDir:
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        "explorer.exe", AppDomain.CurrentDomain.BaseDirectory)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "打开程序目录失败");
                }
                break;
            case restartApp:
                ProcessUtil.ReStartApp();
                break;
            case exitApp:
                OnExitRequested?.Invoke();
                break;
        }
    }
}
