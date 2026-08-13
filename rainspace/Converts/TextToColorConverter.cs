using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace rainspace.Converts
{
    public class TextToColorConverter : IValueConverter
    {
        // PERF: Cache SolidColorBrush instances to avoid creating new brush objects on every
        // binding update. SolidColorBrush is a WinUI resource; creating one per Convert call
        // causes unnecessary GC pressure and prevents the rendering system from optimizing.
        private static readonly Dictionary<string, SolidColorBrush> _brushCache = new();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            string str = (string)value;
            return GetOrCreateBrush(str);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            string str = (string)value;
            return GetOrCreateBrush(str);
        }

        private static SolidColorBrush GetOrCreateBrush(string colorStr)
        {
            // PERF: Return cached brush if available; avoids repeated SolidColorBrush allocations.
            if (_brushCache.TryGetValue(colorStr, out var cached))
                return cached;

            var brush = new SolidColorBrush(ParseColor(colorStr));
            _brushCache[colorStr] = brush;
            return brush;
        }

        internal static Windows.UI.Color ParseColor(string colorStr)
        {
            // Handle hex colors like #RRGGBB or #AARRGGBB
            if (colorStr.StartsWith("#"))
            {
                // PERF: Use AsSpan + Slice to avoid Substring allocations (zero-copy slicing).
                ReadOnlySpan<char> hex = colorStr.AsSpan(1);
                if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Slice(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Slice(4, 2), System.Globalization.NumberStyles.HexNumber);
                    return Windows.UI.Color.FromArgb(255, r, g, b);
                }
                else if (hex.Length == 8)
                {
                    byte a = byte.Parse(hex.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Slice(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Slice(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Slice(6, 2), System.Globalization.NumberStyles.HexNumber);
                    return Windows.UI.Color.FromArgb(a, r, g, b);
                }
            }

            // Handle named colors
            return colorStr.ToLower() switch
            {
                "white" => Colors.White,
                "black" => Colors.Black,
                "red" => Colors.Red,
                "green" => Colors.Green,
                "blue" => Colors.Blue,
                "transparent" => Colors.Transparent,
                _ => Colors.Black,
            };
        }
    }

    /// <summary>
    /// Converts a hex color string (e.g. "#FF0000") to a Windows.UI.Color value.
    /// Used for GradientStop.Color bindings where a Color struct is required,
    /// not a SolidColorBrush.
    /// </summary>
    public class TextToWindowsUIColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string colorStr)
            {
                return TextToColorConverter.ParseColor(colorStr);
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Windows.UI.Color c)
            {
                return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            return "#FF000000";
        }
    }
}
