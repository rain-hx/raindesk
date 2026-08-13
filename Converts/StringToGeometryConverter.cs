using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using raindesk.Util;
using System;

namespace raindesk.Converts
{
    public class StringToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Geometry geometry)
            {
                return geometry;
            }

            if (value is not string pathData || string.IsNullOrWhiteSpace(pathData))
            {
                return MenuGeometryHelper.CreateFallbackGeometry();
            }

            return MenuGeometryHelper.ParseGeometryFresh(pathData) ?? MenuGeometryHelper.CreateFallbackGeometry();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
