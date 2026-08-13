using raindesk.Services;
using raindesk.ViewModel;
using Microsoft.UI.Xaml;

namespace raindesk.Tests;

[TestClass]
public class ThemeServiceTests
{
    [TestMethod]
    public void AppConfig_AppThemeMode_DefaultsToSystem()
    {
        var config = new AppConfig();

        Assert.AreEqual("System", config.AppThemeMode);
    }

    [DataTestMethod]
    [DataRow(null, AppThemeMode.System)]
    [DataRow("", AppThemeMode.System)]
    [DataRow("unknown", AppThemeMode.System)]
    [DataRow("system", AppThemeMode.System)]
    [DataRow("Light", AppThemeMode.Light)]
    [DataRow("dark", AppThemeMode.Dark)]
    public void NormalizeMode_HandlesPersistedValues(string value, AppThemeMode expected)
    {
        Assert.AreEqual(expected, ThemeService.NormalizeMode(value));
    }

    [DataTestMethod]
    [DataRow(AppThemeMode.System, ElementTheme.Default)]
    [DataRow(AppThemeMode.Light, ElementTheme.Light)]
    [DataRow(AppThemeMode.Dark, ElementTheme.Dark)]
    public void ToElementTheme_MapsAppModes(AppThemeMode mode, ElementTheme expected)
    {
        Assert.AreEqual(expected, ThemeService.ToElementTheme(mode));
    }

    [TestMethod]
    public void SetMode_PreservesNormalizedCurrentMode()
    {
        ThemeService.SetMode("dark");

        Assert.AreEqual(AppThemeMode.Dark, ThemeService.CurrentMode);

        ThemeService.SetMode("not-a-mode");

        Assert.AreEqual(AppThemeMode.System, ThemeService.CurrentMode);
    }

    [TestMethod]
    public void RegisterRoot_IsStableUnderConcurrentRegisterAndUnregister()
    {
        ThemeService.ClearRegistrationsForTests();

        Parallel.For(0, 100, _ =>
        {
            using var registration = ThemeService.RegisterRoot(null);
            ThemeService.SetMode(AppThemeMode.Dark);
            ThemeService.SetMode(AppThemeMode.Light);
        });

        Assert.AreEqual(0, ThemeService.RegisteredRootCount);
    }
}
