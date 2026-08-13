using Microsoft.UI.Xaml.Data;
using rainspace.Services;
using rainspace.Services.Interfaces;
using System;

namespace rainspace.Converts
{
    public class MenuInfoConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var appData = AppServices.TryGet<IAppDataProvider>()?.Current;
            int menuIndex = appData?.AppConfig.SelectedMenuIndex ?? -1;
            if (appData == null || menuIndex < 0 || menuIndex >= appData.MenuList.Count)
            {
                return "";
            }
            else
            {
                string type = parameter.ToString();
                if ("1".Equals(type))
                {
                    return appData.MenuList[menuIndex].MenuGeometry;
                }
                else
                {
                    return appData.MenuList[menuIndex].MenuName;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
