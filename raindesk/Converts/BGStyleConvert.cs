using raindesk.Constant;
using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class BGStyleConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (BGStyle)value == (BGStyle)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (BGStyle)int.Parse(parameter.ToString());
        }
    }
}
