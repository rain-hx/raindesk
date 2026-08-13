namespace rainspace.Tests;

[TestClass]
public class IconfontWindowContractTests
{
    [TestMethod]
    public void IconPicker_DragHandlersAreLimitedToTitleBar()
    {
        string source = ReadProjectFile("rainspace", "Control", "Windows", "IconfontWindow.xaml.cs");

        Assert.IsTrue(source.Contains("TitleBarHost.AddHandler(UIElement.PointerPressedEvent", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("RootGrid.AddHandler(UIElement.PointerPressedEvent", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("TitleBarHost.CapturePointer(e.Pointer)", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Confirm_RefreshesBothGroupSurfacesAndPersistsSelection()
    {
        string source = ReadProjectFile("rainspace", "Control", "Windows", "IconfontWindow.xaml.cs");
        int confirmIndex = source.IndexOf("private void Confirm_Click", StringComparison.Ordinal);
        int selectionIndex = source.IndexOf("private void IconSelectionChanged", confirmIndex, StringComparison.Ordinal);
        Assert.IsTrue(confirmIndex >= 0 && selectionIndex > confirmIndex);

        string confirmBody = source[confirmIndex..selectionIndex];
        Assert.IsTrue(confirmBody.Contains("_menuInfo.MenuGeometry = _systemIcons[index].Text", StringComparison.Ordinal));
        Assert.IsTrue(confirmBody.Contains("RefreshMenuIcon(_menuInfo)", StringComparison.Ordinal));
        Assert.IsTrue(confirmBody.Contains("RefreshGroupIcon(_menuInfo)", StringComparison.Ordinal));
        Assert.IsTrue(confirmBody.Contains("_saveService.SaveDebounced()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void IconPicker_RequiresASelectionAndProtectsNewWindowInstance()
    {
        string xaml = ReadProjectFile("rainspace", "Control", "Windows", "IconfontWindow.xaml");
        string source = ReadProjectFile("rainspace", "Control", "Windows", "IconfontWindow.xaml.cs");

        Assert.IsTrue(xaml.Contains("x:Name=\"ConfirmButton\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("IsEnabled=\"False\"", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("ConfirmButton.IsEnabled = SystemIcon.IconListBox.SelectedIndex >= 0", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("if (ReferenceEquals(window, newWindow))", StringComparison.Ordinal));
    }

    private static string ReadProjectFile(params string[] relativeSegments)
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(new[] { directory }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail($"Could not locate {string.Join('/', relativeSegments)} from the test output directory.");
        return string.Empty;
    }
}
