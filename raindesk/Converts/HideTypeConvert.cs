using raindesk.Constant;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class HideTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (AppHideType)value == (AppHideType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (AppHideType)int.Parse(parameter.ToString());
        }
    }
}
