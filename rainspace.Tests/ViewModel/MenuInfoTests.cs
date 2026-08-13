using Microsoft.UI.Xaml;
using rainspace.ViewModel;

namespace rainspace.Tests.ViewModel;

[TestClass]
public class MenuInfoTests
{
    [TestMethod]
    public void SettingSameMenuName_DoesNotRaisePropertyChanged()
    {
        var menu = new MenuInfo { MenuName = "Tools" };
        int changeCount = 0;
        menu.PropertyChanged += (_, _) => changeCount++;

        menu.MenuName = "Tools";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameMenuEdit_DoesNotRaisePropertyChanged()
    {
        var menu = new MenuInfo { MenuEdit = Visibility.Visible };
        int changeCount = 0;
        menu.PropertyChanged += (_, _) => changeCount++;

        menu.MenuEdit = Visibility.Visible;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentMenuEdit_RaisesMenuEditAndInverseChanges()
    {
        var menu = new MenuInfo();
        var propertyNames = new List<string?>();
        menu.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        menu.MenuEdit = Visibility.Visible;

        CollectionAssert.AreEqual(
            new[] { nameof(MenuInfo.MenuEdit), nameof(MenuInfo.NotMenuEdit) },
            propertyNames);
    }

    [TestMethod]
    public void SettingSameSelectedState_DoesNotRaisePropertyChanged()
    {
        var menu = new MenuInfo { IsSelected_NoWrite = true };
        int changeCount = 0;
        menu.PropertyChanged += (_, _) => changeCount++;

        menu.IsSelected_NoWrite = true;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentSelectedState_RaisesSelectionDerivedChanges()
    {
        var menu = new MenuInfo();
        var propertyNames = new List<string?>();
        menu.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        menu.IsSelected_NoWrite = true;

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(MenuInfo.IsSelected_NoWrite),
                nameof(MenuInfo.SelectionIndicatorOpacity_NoWrite),
                nameof(MenuInfo.GroupTabLabelBrush_NoWrite)
            },
            propertyNames);
    }
}
