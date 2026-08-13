using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;

namespace rainspace.Converts
{
    public class ValueConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                Dictionary<bool, string> dict = (Dictionary<bool, string>)parameter;
                bool val = (bool)value;
                return dict[val];
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                Dictionary<bool, string> dict = (Dictionary<bool, string>)parameter;
                bool val = (bool)value;
                return dict[val];
            }
            return null;
        }
    }
}
