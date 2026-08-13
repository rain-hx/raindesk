using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace rainspace.Converts
{
    public class IntToCornerRadius : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int val = int.Parse(value.ToString());
            var param = 0;
            if (parameter != null)
            {
                param = int.Parse(parameter.ToString());
            }
            if (val + param > 0)
            {
                val += param;
            }
            return new CornerRadius(val);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
