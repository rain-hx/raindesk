using raindesk.Services;
using raindesk.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace raindesk.Util
{
    /// <summary>
    /// 全窗口毛玻璃背景与卡片半透明样式。
    /// </summary>
    public static class GlassBackdropUtil
    {
        private static bool BlurEnabled => !AppServices.IsConfigured
            || AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig.BlurEffect != false;

        private static int BackgroundOpacity => !AppServices.IsConfigured
            ? 100
            : AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig.BgOpacity ?? 100;

        public static void ApplyMainWindowGlass(MainWindow window)
        {
            if (window?.ContentGlassLayer == null) return;

            bool blurEnabled = BlurEnabled;
            bool isDark = ThemeService.IsDarkModeEffective();
            double opacityScale = Math.Clamp(BackgroundOpacity, 0, 100) / 100.0;
            window.ContentGlassLayer.Background = CreateShellBrush(blurEnabled, isDark);
            window.ContentGlassLayer.Visibility = blurEnabled && opacityScale > 0.01
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            window.ContentGlassLayer.Opacity = opacityScale;
        }

        public static void ApplyCardGlass(Border card)
        {
            if (card == null) return;

            bool blurEnabled = BlurEnabled;
            bool isDark = ThemeService.IsDarkModeEffective();
            card.Background = CreateCardBrush(blurEnabled, isDark);
            card.BorderBrush = CreateCardBorderBrush(blurEnabled, isDark);
        }

        public static Brush CreateShellBrush(bool blurEnabled, bool isDark)
        {
            if (!blurEnabled)
            {
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }

            return new AcrylicBrush
            {
                AlwaysUseFallback = false,
                TintColor = isDark
                    ? Color.FromArgb(255, 18, 28, 44)
                    : Color.FromArgb(255, 244, 249, 255),
                TintOpacity = isDark ? 0.28 : 0.18,
                TintLuminosityOpacity = 0.48,
                FallbackColor = isDark
                    ? Color.FromArgb(200, 14, 23, 38)
                    : Color.FromArgb(200, 246, 249, 254)
            };
        }

        public static Brush CreateCardBrush(bool blurEnabled, bool isDark)
        {
            if (!blurEnabled)
            {
                return new SolidColorBrush(isDark
                    ? Color.FromArgb(234, 24, 35, 55)
                    : Color.FromArgb(234, 251, 253, 255));
            }

            // 卡片保持透明，让全页毛玻璃层透出模糊效果。
            return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        public static Brush CreateCardBorderBrush(bool blurEnabled, bool isDark)
        {
            if (!blurEnabled)
            {
                return new SolidColorBrush(isDark
                    ? Color.FromArgb(255, 43, 58, 85)
                    : Color.FromArgb(255, 229, 236, 247));
            }

            return new SolidColorBrush(isDark
                ? Color.FromArgb(90, 120, 150, 190)
                : Color.FromArgb(90, 180, 200, 230));
        }
    }
}
