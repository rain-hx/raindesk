using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class OpcityConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double val = double.Parse(value.ToString());
            return Math.Round(val / 100.0, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
