using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using rainspace.Constant;
using rainspace.Services;
using WinRT.Interop;
using Win32Interop = Microsoft.UI.Win32Interop;
using WindowId = Microsoft.UI.WindowId;

namespace rainspace.Control.Windows
{
    /// <summary>
    /// WinUI 3 window drag helper - replaces WPF DragMove()
    /// </summary>
    public static class WindowDragHelper
    {
        private static readonly HashSet<Type> BlockedDragTypes = new()
        {
            typeof(Button),
            typeof(TextBox),
            typeof(ToggleSwitch),
            typeof(CheckBox),
            typeof(RadioButton),
            typeof(ListViewItem),
            typeof(ListBoxItem),
            typeof(Slider),
            typeof(HyperlinkButton),
            typeof(ComboBox),
            typeof(NumberBox),
            typeof(PasswordBox),
            typeof(RichEditBox),
            typeof(ScrollBar),
            typeof(Thumb),
            typeof(SelectorBar),
            typeof(SelectorBarItem),
        };

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HT_CAPTION = 0x0002;

        /// <summary>
        /// 从空白/背景区域发起窗口拖动，自动跳过按钮、输入框等交互控件。
        /// </summary>
        public static bool TryBeginDrag(Window window, PointerRoutedEventArgs e)
        {
            if (window == null || e == null)
            {
                return false;
            }

            if (RunTimeStatus.LOCK_APP_PANEL || HotkeyService.IsCapturingHotkey)
            {
                return false;
            }

            PointerPoint point = e.GetCurrentPoint(null);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return false;
            }

            if (!IsDragSafeSource(e.OriginalSource as DependencyObject))
            {
                return false;
            }

            DragMove(window);
            e.Handled = true;
            return true;
        }

        /// <summary>
        /// Start dragging the window. Call from PointerPressed event handler.
        /// </summary>
        public static void DragMove(Window window)
        {
            ReleaseCapture();
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
        }

        /// <summary>
        /// Center the window on screen using AppWindow APIs.
        /// Replaces WPF WindowStartupLocation.CenterScreen.
        /// </summary>
        public static void CenterOnScreen(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow?.Move(new global::Windows.Graphics.PointInt32(
                (int)(Microsoft.UI.Windowing.DisplayArea.Primary.WorkArea.Width - appWindow.Size.Width) / 2,
                (int)(Microsoft.UI.Windowing.DisplayArea.Primary.WorkArea.Height - appWindow.Size.Height) / 2));
        }

        /// <summary>
        /// Disable window resizing. Replaces WPF ResizeMode.NoResize.
        /// </summary>
        public static void DisableResize(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }

        /// <summary>
        /// Hide window from taskbar. Replaces WPF ShowInTaskbar=False.
        /// </summary>
        public static void HideFromTaskbar(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                appWindow.IsShownInSwitchers = false;
            }
        }

        /// <summary>
        /// Hide the window. Replaces WPF window.Hide().
        /// </summary>
        public static void HideWindow(Window window)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow?.Hide();
        }

        private static bool IsDragSafeSource(DependencyObject? source)
        {
            for (DependencyObject? current = source; current != null; current = VisualTreeHelper.GetParent(current))
            {
                Type type = current.GetType();
                foreach (Type blocked in BlockedDragTypes)
                {
                    if (blocked.IsAssignableFrom(type))
                    {
                        return false;
                    }
                }

                if (current is TextBox textBox)
                {
                    string? tag = textBox.Tag?.ToString();
                    if (tag is "Main" or "ToDo")
                    {
                        return false;
                    }
                }

                if (current is FrameworkElement element && element.Name == "SearchBox")
                {
                    return false;
                }
            }

            return true;
        }
    }
}
