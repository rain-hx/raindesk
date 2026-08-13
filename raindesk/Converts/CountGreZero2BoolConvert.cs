using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class CountGreZero2BoolConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return false;
            int count = (int)value;
            return count > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
