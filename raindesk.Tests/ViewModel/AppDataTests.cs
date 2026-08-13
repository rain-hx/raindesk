using System.Collections.ObjectModel;
using raindesk.ViewModel;

namespace raindesk.Tests.ViewModel;

[TestClass]
public class AppDataTests
{
    [TestMethod]
    public void SettingSameAppConfig_DoesNotRaisePropertyChanged()
    {
        var data = new AppData();
        int changeCount = 0;
        data.PropertyChanged += (_, _) => changeCount++;

        data.AppConfig = data.AppConfig;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentTasks_RaisesPropertyChangedOnce()
    {
        var data = new AppData();
        int changeCount = 0;
        string? propertyName = null;
        data.PropertyChanged += (_, e) =>
        {
            changeCount++;
            propertyName = e.PropertyName;
        };

        data.Tasks = new List<ToDoTask> { new() { Title = "New task" } };

        Assert.AreEqual(1, changeCount);
        Assert.AreEqual(nameof(AppData.Tasks), propertyName);
    }

    [TestMethod]
    public void SettingNullTasks_NormalizesToEmptyList()
    {
        var data = new AppData
        {
            Tasks = null!
        };

        Assert.IsNotNull(data.Tasks);
        Assert.AreEqual(0, data.Tasks.Count);
    }

    [TestMethod]
    public void SettingSameMenuList_DoesNotRaisePropertyChanged()
    {
        var data = new AppData();
        ObservableCollection<MenuInfo> menuList = data.MenuList;
        int changeCount = 0;
        data.PropertyChanged += (_, _) => changeCount++;

        data.MenuList = menuList;

        Assert.AreEqual(0, changeCount);
    }
}
