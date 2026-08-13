using rainspace.Constant;
using Microsoft.UI.Xaml.Data;
using System;

namespace rainspace.Converts
{
    public class SortTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (SortType)value == (SortType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (SortType)int.Parse(parameter.ToString());
        }
    }
}
