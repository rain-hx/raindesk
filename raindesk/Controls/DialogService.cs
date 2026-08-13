using System;
using Windows.Foundation;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk;
using raindesk.Services;
using WinRT.Interop;

namespace raindesk.Controls
{
    /// <summary>
    /// Dialog service for WinUI 3. Creates a borderless popup window
    /// sized exactly to the dialog content.
    /// </summary>
    public static class DialogService
    {
        public static Window Show(FrameworkElement content, string? token = null)
        {
            var window = new Window
            {
                Content = content
            };

            ConfigureBorderlessDialog(window);

            MainWindow.mainWindow?.BeginDialogSuppressLostFocusHide();
            window.Closed += (_, _) => MainWindow.mainWindow?.EndDialogSuppressLostFocusHide();

            bool broughtToFront = false;
            (int Width, int Height)? lastAppliedSize = null;

            void ApplyWindowSize()
            {
                try
                {
                    var size = MeasureContentSize(content);
                    if (lastAppliedSize.HasValue
                        && lastAppliedSize.Value.Width == size.Width
                        && lastAppliedSize.Value.Height == size.Height)
                    {
                        if (!broughtToFront)
                        {
                            broughtToFront = true;
                            BringDialogToFront(window);
                        }

                        return;
                    }

                    lastAppliedSize = size;
                    window.AppWindow.Resize(new SizeInt32(size.Width, size.Height));
                    CenterOnMainWindow(window, size.Width, size.Height);
                    if (!broughtToFront)
                    {
                        broughtToFront = true;
                        BringDialogToFront(window);
                    }
                }
                catch
                {
                    // If AppWindow is not available, continue with default size
                }
            }

            content.Loaded += (_, _) =>
            {
                content.UpdateLayout();
                ApplyWindowSize();

                FrameworkElement animationTarget = DialogAnimationHelper.ResolveAnimationTarget(content);
                Action? onEntranceComplete = content is IDialogPostEntrance postEntrance
                    ? postEntrance.OnEntranceAnimationCompleted
                    : null;

                content.DispatcherQueue?.TryEnqueue(() =>
                {
                    DialogAnimationHelper.PlayEntrance(animationTarget, onEntranceComplete);
                });
            };

            ApplyWindowSize();
            window.Activate();
            return window;
        }

        private static void ConfigureBorderlessDialog(Window window)
        {
            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
        }

        private static (int Width, int Height) MeasureContentSize(FrameworkElement content)
        {
            double width = content.Width;
            double height = content.Height;

            if (double.IsNaN(width) || width <= 0d
                || double.IsNaN(height) || height <= 0d)
            {
                content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (double.IsNaN(width) || width <= 0d)
                {
                    width = content.DesiredSize.Width;
                }

                if (double.IsNaN(height) || height <= 0d)
                {
                    height = content.DesiredSize.Height;
                }
            }

            if (content.ActualWidth > 0d)
            {
                width = Math.Max(width, content.ActualWidth);
            }

            if (content.ActualHeight > 0d)
            {
                height = Math.Max(height, content.ActualHeight);
            }

            // AppWindow sizes are in physical pixels; XAML uses effective pixels.
            double scale = ResolveRasterizationScale(content);
            int resolvedWidth = Math.Max(1, (int)Math.Ceiling(width * scale));
            int resolvedHeight = Math.Max(1, (int)Math.Ceiling(height * scale));
            return (resolvedWidth, resolvedHeight);
        }

        private static double ResolveRasterizationScale(FrameworkElement content)
        {
            if (content.XamlRoot?.RasterizationScale is double contentScale and > 0d)
            {
                return contentScale;
            }

            if (MainWindow.mainWindow?.Content is FrameworkElement hostContent
                && hostContent.XamlRoot?.RasterizationScale is double hostScale and > 0d)
            {
                return hostScale;
            }

            return 1.0d;
        }

        private static void CenterOnMainWindow(Window dialog, int windowWidth, int windowHeight)
        {
            MainWindow? host = MainWindow.mainWindow;
            if (host == null) return;

            try
            {
                var mainSize = host.AppWindow.Size;
                var mainPos = host.AppWindow.Position;
                int x = mainPos.X + Math.Max(0, (mainSize.Width - windowWidth) / 2);
                int y = mainPos.Y + Math.Max(0, (mainSize.Height - windowHeight) / 2);
                dialog.AppWindow.Move(new PointInt32(x, y));
            }
            catch
            {
            }
        }

        private static void BringDialogToFront(Window window)
        {
            try
            {
                IntPtr dialogHwnd = WindowNative.GetWindowHandle(window);
                if (dialogHwnd == IntPtr.Zero) return;

                MainWindow? host = MainWindow.mainWindow;
                if (host != null)
                {
                    IntPtr ownerHwnd = WindowNative.GetWindowHandle(host);
                    if (ownerHwnd != IntPtr.Zero)
                    {
                        NativeWindowMethods.SetWindowLongPtr(
                            dialogHwnd,
                            NativeWindowMethods.GWLP_HWNDPARENT,
                            ownerHwnd);
                    }
                }

                NativeWindowMethods.SetWindowPos(
                    dialogHwnd,
                    NativeWindowMethods.HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    NativeWindowMethods.SWP_NOMOVE
                        | NativeWindowMethods.SWP_NOSIZE
                        | NativeWindowMethods.SWP_SHOWWINDOW
                        | NativeWindowMethods.SWP_NOOWNERZORDER);

                NativeWindowMethods.ActivateWindow(dialogHwnd);
            }
            catch
            {
            }
        }
    }
}
