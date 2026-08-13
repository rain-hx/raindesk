using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using raindesk.Constant;
using ShapesPath = Microsoft.UI.Xaml.Shapes.Path;
using System;
using System.Text;

namespace raindesk.Util
{
    public static class MenuGeometryHelper
    {
        private const int MaxXamlAttributeLength = 30000;

        /// <summary>
        /// 从 SVG path 字符串创建独立 Geometry（不与其他 Path 共享实例）。
        /// </summary>
        public static Geometry? ParseGeometryFresh(string? pathData)
        {
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return null;
            }

            Geometry? geometry = TryLoadPathGeometry(pathData);
            if (geometry != null)
            {
                return geometry;
            }

            geometry = TryLoadDetachedPathData(pathData);
            return geometry;
        }

        private static Geometry? TryLoadPathGeometry(string pathData)
        {
            if (pathData.Length > MaxXamlAttributeLength)
            {
                return null;
            }

            try
            {
                string escaped = EscapeXmlAttribute(pathData);
                string xaml = $"""
                    <PathGeometry xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                  Figures="{escaped}" />
                    """;
                return (Geometry)XamlReader.Load(xaml);
            }
            catch
            {
                return null;
            }
        }

        private static Geometry? TryLoadDetachedPathData(string pathData)
        {
            if (pathData.Length > MaxXamlAttributeLength)
            {
                return null;
            }

            try
            {
                string escaped = EscapeXmlAttribute(pathData);
                string xaml = $"""
                    <Path xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                          Data="{escaped}" />
                    """;
                var path = (ShapesPath)XamlReader.Load(xaml);
                Geometry? geometry = path.Data;
                path.ClearValue(ShapesPath.DataProperty);
                return geometry;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string EscapeXmlAttribute(string value)
        {
            var sb = new StringBuilder(value.Length + 16);
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        public static Geometry CreateFallbackGeometry()
        {
            return ParseGeometryFresh(Constants.DEFAULT_MENU_GEOMETRY)
                ?? new RectangleGeometry { Rect = new Windows.Foundation.Rect(2, 2, 12, 12) };
        }

        public static void ApplyToPath(ShapesPath? target, string? pathData)
        {
            if (target == null)
            {
                return;
            }

            Geometry? geometry = ParseGeometryFresh(pathData);
            if (geometry == null)
            {
                geometry = CreateFallbackGeometry();
            }

            try
            {
                target.Data = geometry;
                target.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                try
                {
                    target.Data = new RectangleGeometry { Rect = new Windows.Foundation.Rect(2, 2, 12, 12) };
                    target.Visibility = Visibility.Visible;
                }
                catch
                {
                    target.ClearValue(ShapesPath.DataProperty);
                    target.Visibility = Visibility.Collapsed;
                }
            }
        }

        public static T? FindDescendantByName<T>(DependencyObject? root, string name) where T : FrameworkElement
        {
            if (root == null)
            {
                return null;
            }

            if (root is FrameworkElement fe && fe.Name == name && fe is T match)
            {
                return match;
            }

            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var found = FindDescendantByName<T>(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
