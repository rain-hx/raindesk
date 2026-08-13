using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace raindesk.Controls
{
    internal sealed class FloatingWindowOptions
    {
        public bool Topmost { get; set; }
        public bool ShowInTaskbar { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Left { get; set; }
        public double? Top { get; set; }
        public Brush WindowBackground { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        public Brush ContentBackground { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.White);
        public Thickness Padding { get; set; }
        public Thickness Margin { get; set; }
        public CornerRadius CornerRadius { get; set; } = new CornerRadius(8);
        public bool AllowDrag { get; set; } = true;
    }

    internal static class FloatingWindowService
    {
        public static Window Show(FrameworkElement content, FloatingWindowOptions options)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            options = options ?? new FloatingWindowOptions();

            var window = new Window();
            // WinUI3: Window properties are limited compared to WPF

            if (options.Width.HasValue)
            {
                // Note: WinUI3 Window doesn't have Width property directly
            }

            if (options.Height.HasValue)
            {
                // Note: WinUI3 Window doesn't have Height property directly
            }

            var border = new Border
            {
                Background = options.ContentBackground,
                CornerRadius = options.CornerRadius,
                Margin = options.Margin,
                Padding = options.Padding,
                Child = content
            };

            window.Content = border;
            window.Activate();
            return window;
        }
    }
}
