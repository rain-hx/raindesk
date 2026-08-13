using rainspace.Constant;
using Microsoft.UI.Xaml.Data;
using System;

namespace rainspace.Converts
{
    public class UpdateTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (UpdateType)value == (UpdateType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (UpdateType)int.Parse(parameter.ToString());
        }
    }
}
