using raindesk.Converts;
using raindesk.Services;
using raindesk.Util;
using Microsoft.UI.Xaml;
using static raindesk.Services.NativeWindowMethods;
using raindesk.Features.Plugins.Contracts;

namespace raindesk;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// 获取主窗口实例
    /// </summary>
    public static Window? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object. This is the first authored code executed.
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += (s, e) =>
        {
            LogUtil.WriteErrorLog(e.Exception, "App 未处理异常");
            e.Handled = true;
        };
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            if (!SingleInstanceHelper.TryAcquire())
            {
                LogUtil.WriteLog("Existing raindesk instance detected, activating it");
                SingleInstanceHelper.TryActivateExistingWindow();
                Exit();
                return;
            }

            LogUtil.WriteLog("OnLaunched start");

            // G1: 在 DI 配置之前迁移数据目录（从安装目录到 %LOCALAPPDATA%）
            DataDirectoryMigration.EnsureMigrated();

            // 初始化 DI 容器（必须在创建 MainWindow 之前）
            AppServices.ConfigureServices();
            await AppServices.Get<IPluginCatalog>().InitializeAsync();
            LogUtil.WriteLog("DI container configured");

            LogUtil.WriteLog("Before MainWindow alloc");
            _window = new MainWindow();
            LogUtil.WriteLog("After MainWindow alloc");
            MainWindow = _window;
            _window.Activate();

            bool showOnStart = AppServices.TryGet<raindesk.Services.Interfaces.IAppDataProvider>()?
                .Current.AppConfig.StartedShowPanel ?? true;
            if (_window is raindesk.MainWindow mainWin)
            {
                if (showOnStart)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_RESTORE);
                        ShowWindow(hwnd, SW_SHOW);
                        SetForegroundWindow(hwnd);
                    }
                }
                else
                {
                    mainWin.HideApp();
                    LogUtil.WriteLog("OnLaunched: StartedShowPanel=false, panel hidden");
                }
            }

            LogUtil.WriteLog("OnLaunched window activated");
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "OnLaunched fatal");
            try
            {
                // 尝试向用户显示错误信息（如果 XAML 基础设施尚未就绪则静默失败）
                if (_window != null)
                {
                    var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "启动失败",
                        Content = $"raindesk 启动时遇到致命错误，即将退出。\n\n{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = (_window as MainWindow)?.Content?.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                }
            }
            catch
            {
                // 对话框自身失败，直接退出
            }

            // 致命启动错误必须终止进程，不留无窗口幽灵进程
            Environment.FailFast(ex.ToString());
        }
    }
}
