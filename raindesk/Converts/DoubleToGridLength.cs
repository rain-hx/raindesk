using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class DoubleToGridLength : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double val = double.Parse(value.ToString());
            return new GridLength(val);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            GridLength val = (GridLength)value;
            return val.Value;
        }
    }
}
