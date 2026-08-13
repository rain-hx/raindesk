using raindesk.Constant;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace raindesk.MyThread
{
    public class RelativePathThread
    {
        public static void MakeRelativePath()
        {
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    IAppDataProvider? dataProvider = AppServices.IsConfigured
                        ? AppServices.TryGet<IAppDataProvider>()
                        : null;
                    AppData? appData = dataProvider?.Current;
                    if (appData == null)
                    {
                        return;
                    }

                    ObservableCollection<MenuInfo> menuList = appData.MenuList;

                    string myExePath = Constants.APP_DIR + "raindesk.exe";
                    foreach (MenuInfo mi in menuList)
                    {
                        ObservableCollection<IconInfo> iconList = mi.IconList;
                        if (iconList == null) continue;
                        foreach (IconInfo icon in iconList)
                        {
                            if (icon == null) continue;
                            string relativePath = FileUtil.MakeRelativePath(myExePath, icon.Path);
                            if (File.Exists(icon.Path)
                                && !string.IsNullOrEmpty(relativePath)
                                && !relativePath.Equals(icon.Path))
                            {
                                icon.RelativePath_NoWrite = relativePath;
                            }
                        }
                    }
                    AppServices.TryGet<ISaveService>()?.SaveDebounced();
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "init相对路径出错!");
                }
            });
        }
    }
}
