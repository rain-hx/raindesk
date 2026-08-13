using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace rainspace.Util
{
    /// <summary>
    /// WinUI 3 global hotkey registration backed by a hidden Win32 message window.
    /// </summary>
    public static class GlobalHotKey
    {
        private const uint WM_HOTKEY = 0x0312;
        private const uint WM_CLOSE = 0x0010;

        private static readonly object SyncRoot = new object();
        private static readonly ConcurrentDictionary<int, Action> Callbacks = new ConcurrentDictionary<int, Action>();
        private static readonly ManualResetEventSlim Ready = new ManualResetEventSlim(false);
        private static Thread? messageThread;
        private static IntPtr messageHwnd;
        private static WndProcDelegate? wndProc;
        private static int currentId;

        public delegate void HotKeyCallBackHandler();

        public static int RegisterHotKey(HotkeyModifiers modifiers, VirtualKey key, HotKeyCallBackHandler callback)
        {
            int id = Interlocked.Increment(ref currentId);
            return Register(id, modifiers, (int)key, callback.Invoke);
        }

        public static int Register(int id, HotkeyModifiers modifiers, int vk, Action callback)
        {
            EnsureMessageWindow();
            if (messageHwnd == IntPtr.Zero) return -1;

            Dispose(id);

            uint nativeModifiers = (uint)(modifiers | HotkeyModifiers.MOD_NOREPEAT);
            if (!RegisterHotKey(messageHwnd, id, nativeModifiers, (uint)vk))
            {
                LogUtil.WriteErrorLog(new InvalidOperationException($"RegisterHotKey failed: id={id}, modifiers={nativeModifiers}, vk={vk}"), "注册全局快捷键失败");
                throw new InvalidOperationException("RegisterHotKey failed");
            }

            Callbacks[id] = callback;
            return id;
        }

        public static bool TryRegister(int id, HotkeyModifiers modifiers, int vk, Action callback, out int registeredId)
        {
            registeredId = -1;
            try
            {
                registeredId = Register(id, modifiers, vk, callback);
                return registeredId >= 0;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static int TryRegisterHotKey(HotkeyModifiers modifiers, VirtualKey key, HotKeyCallBackHandler callback)
        {
            int id = Interlocked.Increment(ref currentId);
            return TryRegister(id, modifiers, (int)key, callback.Invoke, out int registeredId) ? registeredId : -1;
        }

        public static void Dispose(int id)
        {
            if (id < 0) return;

            Callbacks.TryRemove(id, out _);
            if (messageHwnd != IntPtr.Zero)
            {
                UnregisterHotKey(messageHwnd, id);
            }
        }

        private static void EnsureMessageWindow()
        {
            if (messageHwnd != IntPtr.Zero) return;

            lock (SyncRoot)
            {
                if (messageHwnd != IntPtr.Zero) return;

                Ready.Reset();
                messageThread = new Thread(MessageLoop)
                {
                    IsBackground = true,
                    Name = "rainspace_GlobalHotKey"
                };
                messageThread.Start();
            }

            Ready.Wait(5000);
        }

        private static void MessageLoop()
        {
            try
            {
                wndProc = WndProc;
                string className = "rainspace_HotKey_" + Guid.NewGuid().ToString("N");
                IntPtr hInstance = GetModuleHandle(null);

                var wcex = new WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
                    hInstance = hInstance,
                    lpszClassName = className
                };
                RegisterClassEx(ref wcex);

                messageHwnd = CreateWindowEx(0, className, "", 0, 0, 0, 0, 0,
                    IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

                Ready.Set();

                while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "GlobalHotKey thread error");
                Ready.Set();
            }
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY && Callbacks.TryGetValue(wParam.ToInt32(), out Action? callback))
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "执行全局快捷键失败");
                }
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
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

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

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
    }
}
