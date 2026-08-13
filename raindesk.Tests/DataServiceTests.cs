using System.Collections.ObjectModel;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests;

[TestClass]
public class DataServiceTests
{
    [TestMethod]
    public void IsPlaceholderData_ReturnsTrue_ForDefaultEmptyNewMenu()
    {
        var data = new AppData();
        data.MenuList.Add(new MenuInfo { MenuName = "NewMenu" });

        Assert.IsTrue(DataService.IsPlaceholderData(data));
    }

    [TestMethod]
    public void NormalizeSelectedMenu_SelectsFirstMenuWithIcons()
    {
        var data = new AppData();
        data.MenuList.Add(new MenuInfo { MenuName = "Empty", IconList = new ObservableCollection<IconInfo>() });
        data.MenuList.Add(new MenuInfo
        {
            MenuName = "Apps",
            IconList = new ObservableCollection<IconInfo>
            {
                new IconInfo { Name_NoWrite = "Chrome" }
            }
        });
        data.AppConfig.SelectedMenuIndex = 0;

        DataService.NormalizeSelectedMenu(data);

        Assert.AreEqual(1, data.AppConfig.SelectedMenuIndex);
        Assert.AreSame(data.MenuList[1].IconList, data.AppConfig.SelectedMenuIcons);
    }
}
