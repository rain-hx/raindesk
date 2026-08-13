using System;

namespace rainspace.Services
{
    internal static class MainWindowMessagePolicy
    {
        private const int WA_INACTIVE = 0;
        private const int SIZE_MINIMIZED = 1;

        public static bool IsBeginMoveSizeMessage(uint msg, IntPtr wParam)
        {
            if (msg == NativeWindowMethods.WM_ENTERSIZEMOVE) return true;
            if (msg != NativeWindowMethods.WM_SYSCOMMAND) return false;

            long command = wParam.ToInt64() & 0xFFF0;
            return command == NativeWindowMethods.SC_SIZE
                || command == NativeWindowMethods.SC_MOVE;
        }

        public static bool IsEndMoveSizeMessage(uint msg)
        {
            return msg == NativeWindowMethods.WM_EXITSIZEMOVE;
        }

        public static bool ShouldSuppressLostFocusHide(bool isInMoveSizeLoop, bool isIconDragActive)
        {
            return isInMoveSizeLoop || isIconDragActive;
        }

        public static bool ShouldScheduleHideOnLostFocus(uint msg, IntPtr wParam, bool suppressLostFocusHide)
        {
            if (suppressLostFocusHide) return false;

            bool inactive = msg == NativeWindowMethods.WM_ACTIVATE && (wParam.ToInt32() & 0xFFFF) == WA_INACTIVE;
            bool killFocus = msg == NativeWindowMethods.WM_KILLFOCUS;
            bool appInactive = msg == NativeWindowMethods.WM_ACTIVATEAPP && wParam == IntPtr.Zero;

            return inactive || killFocus || appInactive;
        }

        public static bool ShouldHideOnLostFocus(bool isConfiguredForLostFocusHide, bool suppressLostFocusHide)
        {
            return isConfiguredForLostFocusHide && !suppressLostFocusHide;
        }

        public static bool ShouldRedirectMinimize(uint msg, IntPtr wParam)
        {
            bool minimizeCommand = msg == NativeWindowMethods.WM_SYSCOMMAND
                && ((wParam.ToInt64() & 0xFFF0) == NativeWindowMethods.SC_MINIMIZE);
            bool minimized = msg == NativeWindowMethods.WM_SIZE && wParam.ToInt32() == SIZE_MINIMIZED;

            return minimizeCommand || minimized;
        }
    }
}
