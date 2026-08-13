using System;
using rainspace.Constant;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;
using static rainspace.Services.NativeWindowMethods;

namespace rainspace.Services;

/// <summary>
/// 窗口位置和大小的持久化服务。
/// 从 MainWindow 提取的窗口几何管理逻辑。
/// </summary>
public sealed class WindowGeometryService
{
    private readonly IAppDataProvider _dataProvider;
    private readonly ISaveService _saveService;

    public WindowGeometryService(IAppDataProvider dataProvider, ISaveService saveService)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    /// <summary>
    /// 从配置恢复窗口位置和大小。
    /// </summary>
    public void Restore(Microsoft.UI.Windowing.AppWindow appWindow, IntPtr hwnd)
    {
        try
        {
            var config = _dataProvider.Read(d => d.AppConfig);
            if (config == null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(1080, 680));
                return;
            }

            int width = (int)(config.WindowWidth > 0 ? config.WindowWidth : 1080);
            int height = (int)(config.WindowHeight > 0 ? config.WindowHeight : 680);

            // 确保窗口大小在合理范围内
            width = Math.Max(480, Math.Min(width, 2560));
            height = Math.Max(600, Math.Min(height, 1440));

            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

            // 恢复窗口位置（如果有保存的话）
            if (config.WindowLeft >= 0 && config.WindowTop >= 0 && hwnd != IntPtr.Zero)
            {
                MoveWindow(hwnd, (int)config.WindowLeft, (int)config.WindowTop, width, height, true);
            }
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "恢复窗口大小失败");
            appWindow.Resize(new Windows.Graphics.SizeInt32(1080, 680));
        }
    }

    /// <summary>
    /// 保存当前窗口位置和大小到配置。
    /// </summary>
    public void Save(Microsoft.UI.Windowing.AppWindow appWindow)
    {
        try
        {
            _dataProvider.Write(data =>
            {
                var config = data.AppConfig;
                if (config == null) return;

                var size = appWindow.Size;
                var position = appWindow.Position;

                config.WindowWidth = size.Width;
                config.WindowHeight = size.Height;
                config.WindowLeft = position.X;
                config.WindowTop = position.Y;
            });
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "保存窗口大小失败");
        }
    }

    /// <summary>
    /// 立即保存窗口几何并刷新到磁盘。
    /// 用于窗口关闭、隐藏等需要确保数据落盘的场景。
    /// </summary>
    public void PersistImmediate(Microsoft.UI.Windowing.AppWindow appWindow)
    {
        Save(appWindow);
        _saveService.Flush();
    }

    /// <summary>
    /// 创建一个事件处理器，用于 AppWindow.Changed 事件。
    /// </summary>
    public Windows.Foundation.TypedEventHandler<Microsoft.UI.Windowing.AppWindow, Microsoft.UI.Windowing.AppWindowChangedEventArgs> CreateChangedHandler(
        Microsoft.UI.Windowing.AppWindow appWindow)
    {
        return (sender, args) =>
        {
            try
            {
                if (args.DidSizeChange || args.DidPositionChange)
                {
                    Save(appWindow);
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "窗口属性变化处理失败");
            }
        };
    }
}
