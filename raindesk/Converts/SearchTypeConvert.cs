using raindesk.Constant;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class SearchTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (SearchType)value == (SearchType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (SearchType)int.Parse(parameter.ToString());
        }
    }
}
