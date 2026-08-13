using Microsoft.UI.Xaml.Data;
using System;

namespace raindesk.Converts
{
    public class SearchResWidth : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string param = parameter as string;
            double windowWidth = MainWindow.mainWindow.AppWindow.Size.Width;
            if ("1".Equals(param))
            {
                double menuLeftWidth = double.Parse(value.ToString());
                return windowWidth - menuLeftWidth;
            }
            else
            {
                double menuLeftWidth = double.Parse(value.ToString());
                return (windowWidth - menuLeftWidth) / 2;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
