using System;

namespace ShowSeconds.Common
{
    public class Constants
    {
        public readonly static string MY_UUID = "8400A17AEEF7C029";

        //dark theme
        public readonly static Microsoft.UI.Xaml.Media.SolidColorBrush darkBG
            = new Microsoft.UI.Xaml.Media.SolidColorBrush
            {
                Color = Microsoft.UI.ColorHelper.FromArgb(255, 46, 50, 54),
                Opacity = 0.8
            };
        public readonly static Microsoft.UI.Xaml.Media.SolidColorBrush darkFont
            = new Microsoft.UI.Xaml.Media.SolidColorBrush
            {
                Color = Microsoft.UI.ColorHelper.FromArgb(255, 255, 255, 255)
            };

        //light theme
        public readonly static Microsoft.UI.Xaml.Media.SolidColorBrush lightBG
            = new Microsoft.UI.Xaml.Media.SolidColorBrush
            {
                Color = Microsoft.UI.ColorHelper.FromArgb(255, 236, 244, 251),
                Opacity = 1
            };
        public readonly static Microsoft.UI.Xaml.Media.SolidColorBrush lightFont
            = new Microsoft.UI.Xaml.Media.SolidColorBrush
            {
                Color = Microsoft.UI.ColorHelper.FromArgb(255, 65, 63, 61),
            };
    }
}
