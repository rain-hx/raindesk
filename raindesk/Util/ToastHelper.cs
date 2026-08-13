using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace raindesk.Util
{
    /// <summary>
    /// Toast 通知类型
    /// </summary>
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// Toast 通知辅助工具类
    /// </summary>
    public static class ToastHelper
    {
        private static readonly CubicEase _fadeInEase = new CubicEase { EasingMode = EasingMode.EaseOut };
        private static readonly CubicEase _fadeOutEase = new CubicEase { EasingMode = EasingMode.EaseIn };

        public static async global::System.Threading.Tasks.Task ShowAsync(string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            try
            {
                var mainWindow = App.MainWindow as MainWindow;
                if (mainWindow?.ToastContainerHost == null) return;

                var toast = CreateToast(message, type);
                mainWindow.ToastContainerHost.Children.Add(toast);

                toast.Opacity = 0;
                toast.Translation = new System.Numerics.Vector3(40, 0, 0);

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = _fadeInEase
                };
                var slideIn = new DoubleAnimation
                {
                    From = 40,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeIn);
                storyboard.Children.Add(slideIn);
                Storyboard.SetTarget(fadeIn, toast);
                Storyboard.SetTargetProperty(fadeIn, "Opacity");
                Storyboard.SetTarget(slideIn, toast);
                Storyboard.SetTargetProperty(slideIn, "Translation.X");
                storyboard.Begin();

                await global::System.Threading.Tasks.Task.Delay(durationMs);

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = _fadeOutEase
                };
                var slideOut = new DoubleAnimation
                {
                    From = 0,
                    To = 20,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = _fadeOutEase
                };
                var fadeOutStoryboard = new Storyboard();
                fadeOutStoryboard.Children.Add(fadeOut);
                fadeOutStoryboard.Children.Add(slideOut);
                Storyboard.SetTarget(fadeOut, toast);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                Storyboard.SetTarget(slideOut, toast);
                Storyboard.SetTargetProperty(slideOut, "Translation.X");
                fadeOutStoryboard.Begin();

                await global::System.Threading.Tasks.Task.Delay(180);
                mainWindow.ToastContainerHost.Children.Remove(toast);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "Toast notification error");
            }
        }

        /// <summary>
        /// 显示 Toast 通知（便捷方法，不等待完成）
        /// </summary>
        public static void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            _ = ShowAsync(message, type, durationMs);
        }

        /// <summary>
        /// 创建 Toast 控件
        /// </summary>
        private static Border CreateToast(string message, ToastType type)
        {
            var (icon, background, foreground) = GetTypeStyle(type);

            var iconBlock = new FontIcon
            {
                Glyph = icon,
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Padding = new Thickness(16, 12, 16, 12)
            };
            stackPanel.Children.Add(iconBlock);
            stackPanel.Children.Add(messageBlock);

            var toast = new Border
            {
                Background = new SolidColorBrush(ParseColor(background)),
                CornerRadius = new CornerRadius(8),
                Child = stackPanel,
                HorizontalAlignment = HorizontalAlignment.Right,
                Shadow = new ThemeShadow()
            };

            return toast;
        }

        /// <summary>
        /// 获取类型样式
        /// </summary>
        private static (string icon, string background, string foreground) GetTypeStyle(ToastType type)
        {
            return type switch
            {
                ToastType.Success => ("✓", "#10C875", "#FFFFFF"),
                ToastType.Error => ("✕", "#EF4444", "#FFFFFF"),
                ToastType.Warning => ("⚠", "#F59E0B", "#FFFFFF"),
                ToastType.Info => ("ℹ", "#087BFF", "#FFFFFF"),
                _ => ("ℹ", "#087BFF", "#FFFFFF")
            };
        }

        /// <summary>
        /// 解析颜色字符串
        /// </summary>
        private static Windows.UI.Color ParseColor(string colorStr)
        {
            if (colorStr.StartsWith("#"))
            {
                var hex = colorStr.Substring(1);
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Windows.UI.Color.FromArgb(255, r, g, b);
                }
            }
            return Colors.Black;
        }

        /// <summary>
        /// 显示成功通知
        /// </summary>
        public static void Success(string message)
        {
            Show(message, ToastType.Success);
        }

        /// <summary>
        /// 显示错误通知
        /// </summary>
        public static void Error(string message)
        {
            Show(message, ToastType.Error);
        }

        /// <summary>
        /// 显示警告通知
        /// </summary>
        public static void Warning(string message)
        {
            Show(message, ToastType.Warning);
        }

        /// <summary>
        /// 显示信息通知
        /// </summary>
        public static void Info(string message)
        {
            Show(message, ToastType.Info);
        }
    }
}
