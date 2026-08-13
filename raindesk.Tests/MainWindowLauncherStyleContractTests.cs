namespace raindesk.Tests;

[TestClass]
public class MainWindowLauncherStyleContractTests
{
    [TestMethod]
    public void TopBar_UsesLauncherScaleForBrandSearchAndCloseButton()
    {
        string xaml = ReadProjectFile("raindesk", "MainWindow.xaml");

        Assert.IsTrue(xaml.Contains("<RowDefinition x:Name=\"TopBarRow\" Height=\"96\"/>", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Name=\"BrandLogo\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Width=\"44\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Height=\"44\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Name=\"BrandTitle\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("FontSize=\"28\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Name=\"SearchBoxHost\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("MaxWidth=\"700\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Height=\"54\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("CornerRadius=\"27\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Name=\"PageCloseButton\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Name=\"TopSettingsButton\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Width=\"52\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Height=\"52\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Navigation_UsesTopSettingsAndLeftAlignedHomeContent()
    {
        string xaml = ReadProjectFile("raindesk", "MainWindow.xaml");

        Assert.IsTrue(xaml.Contains("<ColumnDefinition x:Name=\"RailColumn\" Width=\"0\"/>", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("x:Name=\"AppRail\"", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("x:Name=\"TodoRailButton\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Click=\"SettingsRailClick\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Margin=\"12,0,28,28\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void LauncherThemeResources_AreDefined()
    {
        string xaml = ReadProjectFile("raindesk", "Resource", "Dictionary", "CommonTheme.xaml");

        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherShellBackgroundBrush\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherShellBorderBrush\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherIconSurfaceBrush\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherFolderPanelBackgroundBrush\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherMutedBorderBrush\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherSearchBackgroundBrush\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("x:Key=\"LauncherRailSelectedBrush\"", StringComparison.Ordinal));
    }

    private static string ReadProjectFile(params string[] pathParts)
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(new[] { directory }.Concat(pathParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail($"Could not locate {Path.Combine(pathParts)} from the test output directory.");
        return string.Empty;
    }
}
