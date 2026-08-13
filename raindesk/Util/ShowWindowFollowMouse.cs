using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using WinRT.Interop;

namespace raindesk.Util
{
    /// <summary>
    /// WinUI 3 stub for positioning window near the mouse cursor.
    /// </summary>
    public static class ShowWindowFollowMouse
    {
        public enum MousePosition
        {
            LEFT_CENTER,
            RIGHT_CENTER,
            CENTER,
            TOP_CENTER,
            BOTTOM_CENTER
        }

        public static void Show(Window window, MousePosition position, double offsetX, double offsetY)
        {
            try
            {
                var appWindow = GetAppWindow(window);
                if (appWindow == null) return;

                // Get cursor position via Win32 interop
                GetCursorPos(out POINT cursorPos);

                int windowWidth = appWindow.Size.Width;
                int windowHeight = appWindow.Size.Height;

                int x = cursorPos.X - windowWidth / 2 + (int)offsetX;
                int y = cursorPos.Y - windowHeight / 2 + (int)offsetY;

                // Ensure the window is on screen
                x = Math.Max(0, Math.Min(x, GetSystemMetrics(SM_CXSCREEN) - windowWidth));
                y = Math.Max(0, Math.Min(y, GetSystemMetrics(SM_CYSCREEN) - windowHeight));

                appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ShowWindowFollowMouse] " + ex.Message);
            }
        }

        public static void MoveToMouse(Window window)
        {
            Show(window, MousePosition.CENTER, 0, 0);
        }

        private static AppWindow GetAppWindow(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
