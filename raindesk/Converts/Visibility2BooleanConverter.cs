using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class Visibility2BooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter == null)
            {
                return (Visibility)value == Visibility.Visible;
            }
            else
            {
                return !((Visibility)value == Visibility.Visible);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
