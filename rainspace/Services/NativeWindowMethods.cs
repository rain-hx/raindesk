using System;
using System.Runtime.InteropServices;

namespace rainspace.Services
{
    /// <summary>
    /// 窗口管理相关的 Win32 P/Invoke 声明。
    /// 从 MainWindow 提取，集中管理所有原生方法。
    /// </summary>
    internal static class NativeWindowMethods
    {
        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        #region Constants

        public const int GWLP_WNDPROC = -4;
        public const int GWLP_HWNDPARENT = -8;
        public const uint WM_ACTIVATE = 0x0006;
        public const uint WM_KILLFOCUS = 0x0008;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_ACTIVATEAPP = 0x001C;
        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint WM_ENTERSIZEMOVE = 0x0231;
        public const uint WM_EXITSIZEMOVE = 0x0232;
        public const int SC_SIZE = 0xF000;
        public const int SC_MOVE = 0xF010;
        public const int SC_MINIMIZE = 0xF020;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const uint GA_ROOT = 2;

        // ShowWindow 命令常量
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;

        #endregion

        #region Window Management

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Thread / Foreground

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        #endregion

        #region Point / Rect

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        #endregion

        #region Menu

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        public static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);

        #endregion

        #region Helper Methods

        /// <summary>
        /// 将窗口设为最前（带线程附加技巧）
        /// </summary>
        public static void ActivateWindow(IntPtr hwnd)
        {
            IntPtr foreground = GetForegroundWindow();
            uint currentThreadId = GetCurrentThreadId();
            uint foregroundThreadId = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
            uint targetThreadId = GetWindowThreadProcessId(hwnd, out _);

            bool attachedToForeground = foregroundThreadId != 0
                && foregroundThreadId != currentThreadId
                && AttachThreadInput(currentThreadId, foregroundThreadId, true);
            bool attachedToTarget = targetThreadId != 0
                && targetThreadId != currentThreadId
                && AttachThreadInput(currentThreadId, targetThreadId, true);

            try
            {
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
                SetActiveWindow(hwnd);
                SetFocus(hwnd);
            }
            finally
            {
                if (attachedToTarget)
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                if (attachedToForeground)
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }

        /// <summary>
        /// 判断点是否在窗口外部
        /// </summary>
        public static bool IsPointOutsideWindow(IntPtr hwnd, int x, int y)
        {
            if (!GetWindowRect(hwnd, out RECT rect) || !ContainsPoint(rect, x, y)) return true;

            var point = new POINT { X = x, Y = y };
            IntPtr target = WindowFromPoint(point);
            if (target == IntPtr.Zero) return false;

            return GetAncestor(target, GA_ROOT) != hwnd;
        }

        public static bool ContainsPoint(RECT rect, int x, int y)
        {
            return x >= rect.Left && x < rect.Right && y >= rect.Top && y < rect.Bottom;
        }

        public static bool IsPointWithinWindowBounds(IntPtr hwnd, int x, int y)
        {
            return GetWindowRect(hwnd, out RECT rect) && ContainsPoint(rect, x, y);
        }

        public static bool IsForegroundWindowFor(IntPtr hwnd)
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            if (foreground == hwnd) return true;

            return GetAncestor(foreground, GA_ROOT) == hwnd;
        }

        public static bool IsLeftMouseDownOutside(IntPtr hwnd)
        {
            const int vkLButton = 0x01;
            if ((GetAsyncKeyState(vkLButton) & 0x8000) == 0) return false;
            if (!GetWindowRect(hwnd, out RECT rect)) return false;
            if (!GetCursorPos(out POINT point)) return false;

            return IsPointOutsideWindow(hwnd, point.X, point.Y);
        }

        public static void BringWindowAtPointAbove(IntPtr hwnd, int x, int y)
        {
            var point = new POINT { X = x, Y = y };
            IntPtr target = WindowFromPoint(point);
            if (target == IntPtr.Zero) return;

            target = GetAncestor(target, GA_ROOT);
            if (target == IntPtr.Zero || target == hwnd) return;

            BringWindowToTop(target);
            SetWindowPos(hwnd, target, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }

        #endregion
    }
}
