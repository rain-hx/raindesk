using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace rainspace.Converts
{
    internal class ReverseBoolConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            bool val = (bool)value;
            return !val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
