using raindesk.ViewModel;
using System.Collections.ObjectModel;

namespace raindesk.Tests.ViewModel;

[TestClass]
public class AppConfigTests
{
    [TestMethod]
    public void SettingSameBehaviorValue_DoesNotRaisePropertyChanged()
    {
        var config = new AppConfig();
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.AppAnimation = config.AppAnimation;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentBehaviorValue_RaisesPropertyChangedOnce()
    {
        var config = new AppConfig();
        int changeCount = 0;
        string? propertyName = null;
        config.PropertyChanged += (_, e) =>
        {
            changeCount++;
            propertyName = e.PropertyName;
        };

        config.PMModel = !config.PMModel;

        Assert.AreEqual(1, changeCount);
        Assert.AreEqual(nameof(AppConfig.PMModel), propertyName);
    }

    [TestMethod]
    public void SettingSameThemeValue_DoesNotRaisePropertyChanged()
    {
        var config = new AppConfig();
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.TextColor = config.TextColor;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentTextColor_RaisesTextColorAndBrushChanges()
    {
        var config = new AppConfig();
        var propertyNames = new List<string?>();
        config.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        config.TextColor = "#112233";

        CollectionAssert.AreEqual(
            new[] { nameof(AppConfig.TextColor), nameof(AppConfig.TextColorBrush) },
            propertyNames);
    }

    [TestMethod]
    public void SettingSameLayoutValue_DoesNotRaisePropertyChanged()
    {
        var config = new AppConfig();
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.WindowWidth = config.WindowWidth;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameHotkeyValue_DoesNotRaisePropertyChanged()
    {
        var config = new AppConfig();
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.HotkeyStr = config.HotkeyStr;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameCustomIconUrl_DoesNotRaisePropertyChanged()
    {
        var config = new AppConfig { CustomIconUrl = "https://example.com/icon.json" };
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.CustomIconUrl = config.CustomIconUrl;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameSelectedMenuIcons_DoesNotRaisePropertyChanged()
    {
        var icons = new ObservableCollection<IconInfo>();
        var config = new AppConfig { SelectedMenuIcons = icons };
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.SelectedMenuIcons = icons;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameTrackingUuid_DoesNotRaisePropertyChanged()
    {
        var config = new AppConfig { TrackingUuid = "abc" };
        int changeCount = 0;
        config.PropertyChanged += (_, _) => changeCount++;

        config.TrackingUuid = "abc";

        Assert.AreEqual(0, changeCount);
    }
}
