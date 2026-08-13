using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace rainspace.Util
{
    /// <summary>
    /// WinUI 3 系统托盘图标
    /// </summary>
    public class TrayIcon : IDisposable
    {
        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 1;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_CONTEXTMENU = 0x007B;
        private const int WM_CLOSE = 0x0010;
        private const int NIM_ADD = 0x00;
        private const int NIM_DELETE = 0x02;
        private const int NIF_MESSAGE = 0x01;
        private const int NIF_ICON = 0x02;
        private const int NIF_TIP = 0x04;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA pnid);

        // ExtractIconW - Unicode version
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ExtractIconW(IntPtr hInst, string pszExeFileName, uint nIconIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImageW(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpWndClassEx);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private NOTIFYICONDATA _nid;
        private IntPtr _hWnd;
        private IntPtr _currentIconHandle = IntPtr.Zero;
        private bool _disposed;
        private Action? _onLeftClick;
        private Action? _onRightClick;
        private WndProcDelegate? _wndProc;

        public bool WindowCreated { get; private set; }
        public bool ShellNotifyIconOk { get; private set; }
        public bool AutoTrayDisabled { get; private set; }

        public static TrayIcon? Current { get; private set; }
        public IntPtr WindowHandle => _hWnd;

        public TrayIcon(string tooltip, Action onLeftClick, Action onRightClick)
        {
            Current = this;
            _onLeftClick = onLeftClick;
            _onRightClick = onRightClick;

            var ready = new System.Threading.ManualResetEventSlim(false);
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    _wndProc = WndProc;
                    string className = "rainspace_Tray_" + Guid.NewGuid().ToString("N");

                    var wcex = new WNDCLASSEX
                    {
                        cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                        lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                        hInstance = GetModuleHandle(null),
                        lpszClassName = className
                    };
                    RegisterClassEx(ref wcex);

                    _hWnd = CreateWindowEx(0, className, "", 0, 0, 0, 0, 0,
                        IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

                    if (_hWnd == IntPtr.Zero)
                    {
                        WindowCreated = false;
                        ready.Set();
                        return;
                    }

                    WindowCreated = true;

                    // 加载图标 - 从 AppX 目录的 ico 文件加载
                    _currentIconHandle = IntPtr.Zero;
                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    string icoPath = System.IO.Path.Combine(appDir, "Assets", "AppIcon.ico");
                    if (System.IO.File.Exists(icoPath))
                    {
                        _currentIconHandle = LoadImageW(IntPtr.Zero, icoPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                        LogUtil.WriteLog($"TrayIcon: LoadImage from ico={_currentIconHandle}");
                    }
                    if (_currentIconHandle == IntPtr.Zero)
                    {
                        // 从 shell32 获取文件夹图标
                        _currentIconHandle = ExtractIconW(IntPtr.Zero, "shell32.dll", 4);
                        LogUtil.WriteLog($"TrayIcon: ExtractIcon shell32={_currentIconHandle}");
                    }
                    if (_currentIconHandle == IntPtr.Zero)
                    {
                        _currentIconHandle = LoadIcon(IntPtr.Zero, (IntPtr)32512);
                    }

                    _nid = new NOTIFYICONDATA
                    {
                        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                        hWnd = _hWnd,
                        uID = 1,
                        uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                        uCallbackMessage = WM_TRAYICON,
                        hIcon = _currentIconHandle,
                        szTip = tooltip
                    };

                    ShellNotifyIconOk = Shell_NotifyIcon(NIM_ADD, ref _nid);
                    LogUtil.WriteLog($"TrayIcon: Shell_NotifyIcon={ShellNotifyIconOk}, Size={_nid.cbSize}, HWND={_hWnd}, Icon={_currentIconHandle}");

                    AutoTrayDisabled = SetTrayIconVisible();

                    ready.Set();

                    // 消息循环
                    while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
                    {
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "TrayIcon thread error");
                    ready.Set();
                }
            });
            thread.IsBackground = true;
            thread.Start();
            ready.Wait(5000);
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                int mouseMsg = (int)(lParam.ToInt64() & 0xFFFF);
                if (mouseMsg == WM_LBUTTONDBLCLK)
                    _onLeftClick?.Invoke();
                else if (mouseMsg is WM_RBUTTONUP or WM_RBUTTONDOWN or WM_CONTEXTMENU)
                    _onRightClick?.Invoke();

                return IntPtr.Zero;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// 注册托盘图标（不再修改系统注册表）。
        /// 用户可以通过 Windows 设置手动配置通知区域图标显示。
        /// </summary>
        private static bool SetTrayIconVisible()
        {
            // 不再修改系统注册表的 EnableAutoTray 设置
            // 用户可以通过 设置 > 个性化 > 任务栏 > 通知区域 来配置图标显示
            return true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (ReferenceEquals(Current, this))
                {
                    Current = null;
                }
                Shell_NotifyIcon(NIM_DELETE, ref _nid);

                // 释放 GDI 图标句柄
                if (_currentIconHandle != IntPtr.Zero)
                {
                    try { DestroyIcon(_currentIconHandle); } catch { }
                    _currentIconHandle = IntPtr.Zero;
                }

                if (_hWnd != IntPtr.Zero)
                    PostMessage(_hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                GC.SuppressFinalize(this);
            }
        }
    }
}
