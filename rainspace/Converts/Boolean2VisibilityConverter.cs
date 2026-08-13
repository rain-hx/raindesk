using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace rainspace.Converts
{
    public class Boolean2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool param = false;
            if (value != null)
            {
                param = (bool)value;
            }
            if (param)
            {
                return Visibility.Visible;
            }
            else
            {
                if (parameter is Visibility v)
                    return v;
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
}
