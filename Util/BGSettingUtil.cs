using System;
using System.IO;
using raindesk.Constant;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace raindesk.Util
{
    public static class BGSettingUtil
    {
        private const double DarkImageOpacityScale = 0.72;

        private static AppConfig? CurrentConfig => AppServices.IsConfigured
            ? AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig
            : null;

        public static void SetBG(string path)
        {
            ApplyImagePath(path);
            BGSetting();
        }

        public static void ResetBG()
        {
            AppConfig config = CurrentConfig;
            if (config == null) return;

            config.ImageByteArr = Convert.FromBase64String(Constants.DEFAULT_BAC_IMAGE_BASE64);
            config.BacImgName = "系统默认";
            config.BGStyle = BGStyle.ImgBac;
            BGSetting();
        }

        public static void BGSetting()
        {
            AppConfig config = CurrentConfig;
            if (config == null || MainWindow.mainWindow == null) return;

            var window = MainWindow.mainWindow;
            if (window.WindowRoot == null) return;

            bool isDark = ThemeService.IsDarkModeEffective();

            try
            {
                // Root keeps the theme base color; custom wallpaper only paints the image layer.
                window.WindowRoot.Background = CreateThemeBaseBrush(isDark);

                var imageHost = window.BGImageHost ?? window.BGImageLayer;
                if (imageHost != null)
                {
                    imageHost.Background = CreateBackgroundBrush(config, isDark);
                }

                if (window.BGDarkOverlay != null)
                {
                    window.BGDarkOverlay.Opacity = isDark ? 1 : 0;
                }
            }
            catch
            {
                var fallback = CreateThemeBaseBrush(isDark);
                window.WindowRoot.Background = fallback;
                if (window.BGImageHost != null)
                {
                    window.BGImageHost.Background = null;
                }
                else if (window.BGImageLayer != null)
                {
                    window.BGImageLayer.Background = null;
                }

                if (window.BGDarkOverlay != null)
                {
                    window.BGDarkOverlay.Opacity = 0;
                }
            }
        }

        public static void BGSetting(string path)
        {
            ApplyImagePath(path);
            BGSetting();
        }

        public static void BGSetting(string path, int style)
        {
            AppConfig config = CurrentConfig;
            if (config == null) return;

            config.BGStyle = (BGStyle)style;
            if (config.BGStyle == BGStyle.ImgBac)
            {
                ApplyImagePath(path);
            }

            BGSetting();
        }

        private static SolidColorBrush CreateThemeBaseBrush(bool isDark)
        {
            return new SolidColorBrush(ThemeService.GetAppBackgroundColor(
                isDark ? AppThemeMode.Dark : AppThemeMode.Light));
        }

        private static Brush CreateBackgroundBrush(AppConfig config, bool isDark)
        {
            Brush custom = config.BGStyle == BGStyle.GradientBac
                ? CreateGradientBrush(config)
                : CreateImageBrush(config);

            if (custom is ImageBrush imageBrush && imageBrush.ImageSource == null)
            {
                return null;
            }

            if (!isDark)
            {
                return custom;
            }

            return ApplyDarkModeTreatment(custom);
        }

        private static Brush ApplyDarkModeTreatment(Brush brush)
        {
            switch (brush)
            {
                case ImageBrush imageBrush:
                    return new ImageBrush
                    {
                        ImageSource = imageBrush.ImageSource,
                        Stretch = imageBrush.Stretch,
                        Opacity = imageBrush.Opacity * DarkImageOpacityScale
                    };
                case LinearGradientBrush gradientBrush:
                    var darkGradient = new LinearGradientBrush
                    {
                        StartPoint = gradientBrush.StartPoint,
                        EndPoint = gradientBrush.EndPoint,
                        Opacity = gradientBrush.Opacity * DarkImageOpacityScale
                    };
                    foreach (GradientStop stop in gradientBrush.GradientStops)
                    {
                        darkGradient.GradientStops.Add(new GradientStop
                        {
                            Color = stop.Color,
                            Offset = stop.Offset
                        });
                    }
                    return darkGradient;
                default:
                    return brush;
            }
        }

        private static ImageBrush CreateImageBrush(AppConfig config)
        {
            return new ImageBrush
            {
                ImageSource = config.BitmapImage,
                Stretch = Stretch.UniformToFill,
                Opacity = 1
            };
        }

        private static LinearGradientBrush CreateGradientBrush(AppConfig config)
        {
            GradientBGParam param = config.GradientBGParam;
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };

            brush.GradientStops.Add(new GradientStop
            {
                Color = ParseColor(param?.Color1, Color.FromArgb(255, 235, 247, 227)),
                Offset = 0
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = ParseColor(param?.Color2, Color.FromArgb(255, 168, 228, 192)),
                Offset = 1
            });

            return brush;
        }

        private static void ApplyImagePath(string path)
        {
            AppConfig config = CurrentConfig;
            if (config == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

            config.ImageByteArr = File.ReadAllBytes(path);
            config.BacImgName = path;
            config.BGStyle = BGStyle.ImgBac;
        }

        private static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;

            string hex = value.Trim().TrimStart('#');
            try
            {
                byte a = 255;
                int start = 0;
                if (hex.Length == 8)
                {
                    a = Convert.ToByte(hex.Substring(0, 2), 16);
                    start = 2;
                }
                else if (hex.Length != 6)
                {
                    return fallback;
                }

                byte r = Convert.ToByte(hex.Substring(start, 2), 16);
                byte g = Convert.ToByte(hex.Substring(start + 2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(start + 4, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
