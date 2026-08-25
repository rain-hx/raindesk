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
            var (icon, backgroundKey) = GetTypeStyle(type);
            Brush background = ThemeBrush(backgroundKey);
            Brush foreground = new SolidColorBrush(Colors.White);

            var iconBlock = new FontIcon
            {
                Glyph = icon,
                FontSize = 18,
                Foreground = foreground,
                VerticalAlignment = VerticalAlignment.Center
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = foreground,
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
                Background = background,
                CornerRadius = new CornerRadius(8),
                Child = stackPanel,
                HorizontalAlignment = HorizontalAlignment.Right,
                Shadow = new ThemeShadow()
            };

            return toast;
        }

        /// <summary>
        /// 获取类型样式：图标与主题资源键。语义色统一从应用主题读取，响应明暗/高对比度。
        /// </summary>
        private static (string icon, string backgroundKey) GetTypeStyle(ToastType type)
        {
            return type switch
            {
                ToastType.Success => ("✓", "raindeskGreenBrush"),
                ToastType.Error => ("✕", "raindeskRedBrush"),
                ToastType.Warning => ("⚠", "raindeskAmberBrush"),
                ToastType.Info => ("ℹ", "raindeskBlueBrush"),
                _ => ("ℹ", "raindeskBlueBrush")
            };
        }

        /// <summary>
        /// 从应用主题资源读取画刷，缺失时回退为透明。
        /// </summary>
        private static Brush ThemeBrush(string resourceKey)
        {
            if (Application.Current?.Resources?.TryGetValue(resourceKey, out object value) == true
                && value is Brush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Colors.Transparent);
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
