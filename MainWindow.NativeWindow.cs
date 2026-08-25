using System;
using System.Runtime.InteropServices;
using raindesk.Services;
using raindesk.Util;
using static raindesk.Services.NativeWindowMethods;

namespace raindesk;

public sealed partial class MainWindow
{
    private delegate IntPtr WindowProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>安装主窗口消息钩子，用于拦截移动/缩放结束、失焦隐藏与最小化重定向。</summary>
    private void InstallWindowMessageHook()
    {
        try
        {
            if (_windowProcDelegate != null) return;

            _windowSubclassHwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (_windowSubclassHwnd == IntPtr.Zero) return;

            _windowProcDelegate = WindowMessageProc;
            IntPtr proc = Marshal.GetFunctionPointerForDelegate(_windowProcDelegate);
            _originalWndProc = NativeWindowMethods.SetWindowLongPtr(_windowSubclassHwnd, NativeWindowMethods.GWLP_WNDPROC, proc);
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "安装主窗口消息监听失败");
        }
    }

    /// <summary>恢复原始窗口过程，卸载消息钩子。</summary>
    private void RestoreWindowMessageHook()
    {
        try
        {
            if (_windowSubclassHwnd == IntPtr.Zero || _originalWndProc == IntPtr.Zero) return;

            NativeWindowMethods.SetWindowLongPtr(_windowSubclassHwnd, NativeWindowMethods.GWLP_WNDPROC, _originalWndProc);
            _windowSubclassHwnd = IntPtr.Zero;
            _originalWndProc = IntPtr.Zero;
            _windowProcDelegate = null;
        }
        catch (Exception ex) { LogUtil.WriteErrorLog(ex, "恢复窗口消息钩子失败"); }
    }

    private IntPtr WindowMessageProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (MainWindowMessagePolicy.IsBeginMoveSizeMessage(msg, wParam))
        {
            _isInWindowMoveSizeLoop = true;
            CancelScheduledLostFocusHide();
        }
        else if (MainWindowMessagePolicy.IsEndMoveSizeMessage(msg))
        {
            _isInWindowMoveSizeLoop = false;
            CancelScheduledLostFocusHide();
            if (_geometryService != null)
            {
                _geometryService.PersistImmediate(this.AppWindow);
            }
            else
            {
                PersistWindowGeometryImmediate();
            }
        }

        if (MainWindowMessagePolicy.ShouldScheduleHideOnLostFocus(msg, wParam, ShouldSuppressLostFocusHide))
        {
            ReleaseTopMost(hwnd, false);
            ScheduleHideOnLostFocus();
        }

        if (!_allowClose && !_isRedirectingMinimize && MainWindowMessagePolicy.ShouldRedirectMinimize(msg, wParam))
        {
            try
            {
                _isRedirectingMinimize = true;
                NativeWindowMethods.ShowWindow(hwnd, SW_HIDE);
                LogUtil.WriteLog("MainWindow minimize redirected to tray");
            }
            finally
            {
                _isRedirectingMinimize = false;
            }

            return IntPtr.Zero;
        }

        return NativeWindowMethods.CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
    }
}
