using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace raindesk.Services;

internal static class SingleInstanceHelper
{
    private const string MutexName = @"Global\raindesk.WinUI3.SingleInstance";
    private static Mutex? _mutex;

    public static bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        return createdNew;
    }

    public static bool TryActivateExistingWindow()
    {
        IntPtr hwnd = FindWindow(null, "raindesk");
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        NativeWindowMethods.ShowWindow(hwnd, NativeWindowMethods.SW_RESTORE);
        NativeWindowMethods.ShowWindow(hwnd, NativeWindowMethods.SW_SHOW);
        NativeWindowMethods.ActivateWindow(hwnd);
        return true;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
}
