using rainspace.Constant;
using Microsoft.UI.Xaml.Data;
using System;

namespace rainspace.Converts
{
    public class TodoTaskExecConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (TodoTaskExecType)value == (TodoTaskExecType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (TodoTaskExecType)int.Parse(parameter.ToString());
        }
    }
}
