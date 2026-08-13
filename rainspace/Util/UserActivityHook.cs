using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.ComponentModel;

namespace rainspace.Util
{
    /// <summary>
    /// Global mouse and keyboard hook using Win32 API.
    /// WinUI 3 version - no System.Windows.Forms dependency.
    /// </summary>
    public class UserActivityHook : IDisposable
    {
        #region Windows structure definitions

        [StructLayout(LayoutKind.Sequential)]
        private class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MouseLLHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        #endregion

        #region Windows function imports

        [DllImport("user32.dll", CharSet = CharSet.Auto,
           CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
             CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(
            IntPtr idHook,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        private delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32")]
        private static extern int ToAscii(
            int uVirtKey,
            int uScanCode,
            byte[] lpbKeyState,
            byte[] lpwTransKey,
            int fuState);

        [DllImport("user32")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        #endregion

        #region Windows constants

        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;

        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDBLCLK = 0x209;
        private const int WM_MOUSEWHEEL = 0x020A;

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;

        private const byte VK_SHIFT = 0x10;
        private const byte VK_CAPITAL = 0x14;
        private const byte VK_NUMLOCK = 0x90;

        #endregion

        public UserActivityHook()
        {
            Start();
        }

        public UserActivityHook(bool InstallMouseHook, bool InstallKeyboardHook)
        {
            Start(InstallMouseHook, InstallKeyboardHook);
        }

        public bool EnableMouse = false;

        // Custom delegates for WinUI 3 (no System.Windows.Forms dependency)
        public delegate void MouseDownEventHandler(object sender, int button, int clickCount, int x, int y, int delta);
        public delegate void MouseUpEventHandler(object sender, int button, int clickCount, int x, int y, int delta);
        public delegate void MouseWheelEventHandler(object sender, int delta, int x, int y);
        public delegate void KeyDownEventHandler(object sender, int vkCode);
        public delegate void KeyUpEventHandler(object sender, int vkCode);
        public delegate void KeyPressEventHandler(object sender, char keyChar);

        public event MouseDownEventHandler? OnMouseLeftDown;
        public event MouseUpEventHandler? OnMouseLeftUp;
        public event MouseWheelEventHandler? OnMouseWheel;
        public event MouseDownEventHandler? OnMouseWheelDown;
        public event MouseUpEventHandler? OnMouseWheelUp;

        public bool MouseLeftDownEnable() { return OnMouseLeftDown != null; }
        public bool MouseLeftUpEnable() { return OnMouseLeftUp != null; }
        public bool MouseWheelDownEnable() { return OnMouseWheelDown != null; }
        public bool MouseWheelUpEnable() { return OnMouseWheelUp != null; }

        public event KeyDownEventHandler? KeyDown;
        public event KeyPressEventHandler? KeyPress;
        public event KeyUpEventHandler? KeyUp;
        public Func<int, bool>? ShouldSuppressKeyDown;

        private IntPtr hMouseHook = IntPtr.Zero;
        private IntPtr hKeyboardHook = IntPtr.Zero;

        // 实例字段而非 static，避免多实例互相覆盖导致 GC 回收旧 delegate
        private HookProc? _mouseHookProc;
        private HookProc? _keyboardHookProc;
        private bool _disposed;

        public void Start()
        {
            this.Start(true, true);
        }

        public void Start(bool InstallMouseHook, bool InstallKeyboardHook)
        {
            this.EnableMouse = InstallMouseHook;
            if (hMouseHook == IntPtr.Zero && EnableMouse)
            {
                _mouseHookProc = new HookProc(MouseHookProc);
                hMouseHook = SetWindowsHookEx(
                    WH_MOUSE_LL,
                    _mouseHookProc,
                    Marshal.GetHINSTANCE(
                        Assembly.GetExecutingAssembly().GetModules()[0]),
                    0);
                if (hMouseHook == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Stop(true, false, false);
                    throw new Win32Exception(errorCode);
                }
            }

            if (hKeyboardHook == IntPtr.Zero && InstallKeyboardHook)
            {
                _keyboardHookProc = new HookProc(KeyboardHookProc);
                hKeyboardHook = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _keyboardHookProc,
                    Marshal.GetHINSTANCE(
                    Assembly.GetExecutingAssembly().GetModules()[0]),
                    0);
                if (hKeyboardHook == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Stop(false, true, false);
                    throw new Win32Exception(errorCode);
                }
            }
        }

        public void Stop()
        {
            this.Stop(true, true, true);
        }

        public void Stop(bool UninstallMouseHook, bool UninstallKeyboardHook, bool ThrowExceptions)
        {
            if (hMouseHook != IntPtr.Zero && UninstallMouseHook)
            {
                bool success = UnhookWindowsHookEx(hMouseHook);
                hMouseHook = IntPtr.Zero;
                _mouseHookProc = null;
                if (!success && ThrowExceptions)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }

            if (hKeyboardHook != IntPtr.Zero && UninstallKeyboardHook)
            {
                bool success = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = IntPtr.Zero;
                _keyboardHookProc = null;
                if (!success && ThrowExceptions)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop(true, true, false);
                GC.SuppressFinalize(this);
            }
        }

        private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && EnableMouse)
            {
                try
                {
                    MouseLLHookStruct mouseHookStruct = Marshal.PtrToStructure<MouseLLHookStruct>(lParam);

                    int button = 0; // 0=None, 1=Left, 2=Right, 3=Middle
                    int clickCount = 0;
                    short mouseDelta = 0;
                    int wParamInt = (int)wParam;

                    switch (wParamInt)
                    {
                        case WM_LBUTTONDOWN:
                            button = 1;
                            clickCount = 1;
                            OnMouseLeftDown?.Invoke(this, button, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta);
                            break;
                        case WM_LBUTTONUP:
                            button = 1;
                            clickCount = 1;
                            OnMouseLeftUp?.Invoke(this, button, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta);
                            break;
                        case WM_RBUTTONDOWN:
                            button = 2;
                            clickCount = 1;
                            break;
                        case WM_RBUTTONUP:
                            button = 2;
                            clickCount = 1;
                            break;
                        case WM_MOUSEWHEEL:
                            mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);
                            OnMouseWheel?.Invoke(this, mouseDelta, mouseHookStruct.pt.x, mouseHookStruct.pt.y);
                            break;
                        case WM_MBUTTONDOWN:
                            button = 3;
                            clickCount = 1;
                            OnMouseWheelDown?.Invoke(this, button, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta);
                            break;
                        case WM_MBUTTONUP:
                            button = 3;
                            clickCount = 1;
                            OnMouseWheelUp?.Invoke(this, button, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "MouseHookProc 异常");
                }
            }
            return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
        }

        private int KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool handled = false;
            if (nCode >= 0 && (KeyDown != null || KeyUp != null || KeyPress != null))
            {
                try
                {
                    KeyboardHookStruct MyKeyboardHookStruct = Marshal.PtrToStructure<KeyboardHookStruct>(lParam);
                    int wParamInt = (int)wParam;

                    if (KeyDown != null && (wParamInt == WM_KEYDOWN || wParamInt == WM_SYSKEYDOWN))
                    {
                        KeyDown?.Invoke(this, MyKeyboardHookStruct.vkCode);
                        try
                        {
                            handled = ShouldSuppressKeyDown?.Invoke(MyKeyboardHookStruct.vkCode) == true;
                        }
                        catch (Exception ex)
                        {
                            LogUtil.WriteErrorLog(ex, "ShouldSuppressKeyDown 回调异常");
                        }
                    }

                    if (KeyPress != null && wParamInt == WM_KEYDOWN)
                    {
                        bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
                        bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);

                        byte[] keyState = new byte[256];
                        GetKeyboardState(keyState);
                        byte[] inBuffer = new byte[2];
                        if (ToAscii(MyKeyboardHookStruct.vkCode,
                                  MyKeyboardHookStruct.scanCode,
                                  keyState,
                                  inBuffer,
                                  MyKeyboardHookStruct.flags) == 1)
                        {
                            char key = (char)inBuffer[0];
                            if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key)) key = Char.ToUpper(key);
                            KeyPress?.Invoke(this, key);
                        }
                    }

                    if (KeyUp != null && (wParamInt == WM_KEYUP || wParamInt == WM_SYSKEYUP))
                    {
                        KeyUp?.Invoke(this, MyKeyboardHookStruct.vkCode);
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "KeyboardHookProc 异常");
                }
            }

            if (handled)
                return 1;
            else
                return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }
    }
}
