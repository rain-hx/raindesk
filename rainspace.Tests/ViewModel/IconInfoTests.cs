using rainspace.Constant;
using rainspace.ViewModel;

namespace rainspace.Tests.ViewModel;

[TestClass]
public class IconInfoTests
{
    [TestMethod]
    public void SettingSameName_DoesNotRaisePropertyChanged()
    {
        var icon = new IconInfo { Name = "Calculator" };
        int changeCount = 0;
        icon.PropertyChanged += (_, _) => changeCount++;

        icon.Name = "Calculator";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameCount_DoesNotRaisePropertyChanged()
    {
        var icon = new IconInfo { Count = 2 };
        int changeCount = 0;
        icon.PropertyChanged += (_, _) => changeCount++;

        icon.Count = 2;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameAddShortcutState_DoesNotRaisePropertyChanged()
    {
        var icon = new IconInfo { IsAddShortcut_NoWrite = true };
        int changeCount = 0;
        icon.PropertyChanged += (_, _) => changeCount++;

        icon.IsAddShortcut_NoWrite = true;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentAddShortcutState_RaisesAddAndRegularShortcutChanges()
    {
        var icon = new IconInfo();
        var propertyNames = new List<string?>();
        icon.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        icon.IsAddShortcut_NoWrite = true;

        CollectionAssert.AreEqual(
            new[] { nameof(IconInfo.IsAddShortcut_NoWrite), nameof(IconInfo.IsRegularShortcut_NoWrite), nameof(IconInfo.IsLaunchableShortcut_NoWrite) },
            propertyNames);
    }

    [TestMethod]
    public void NewIcon_DefaultsToRegularNonFolderShortcut()
    {
        var icon = new IconInfo();

        Assert.IsFalse(string.IsNullOrWhiteSpace(icon.IconId));
        Assert.IsFalse(icon.IsFolder);
        Assert.AreEqual(ShortcutFolderSize.Small, icon.FolderSize);
        Assert.IsNotNull(icon.FolderItems);
        Assert.AreEqual(0, icon.FolderItems.Count);
        Assert.IsTrue(icon.IsRegularShortcut_NoWrite);
        Assert.IsTrue(icon.IsLaunchableShortcut_NoWrite);
        Assert.AreEqual(0, icon.FolderItemCount_NoWrite);
        Assert.AreEqual(0, icon.FolderPreviewItems_NoWrite.Count);
        Assert.AreEqual(0, icon.FolderPreviewOverflowCount_NoWrite);
        Assert.IsFalse(icon.HasFolderPreviewOverflow_NoWrite);
        Assert.AreEqual(1, icon.FolderColumnSpan_NoWrite);
        Assert.AreEqual(1, icon.FolderRowSpan_NoWrite);
    }

    [TestMethod]
    public void SettingFolderState_RaisesFolderAndLaunchabilityChanges()
    {
        var icon = new IconInfo();
        var propertyNames = new List<string?>();
        icon.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        icon.IsFolder = true;

        CollectionAssert.Contains(propertyNames, nameof(IconInfo.IsFolder));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.IsLaunchableShortcut_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderItemCount_NoWrite));
        Assert.IsFalse(icon.IsLaunchableShortcut_NoWrite);
    }

    [TestMethod]
    public void SettingFolderSize_RaisesSpanChanges()
    {
        var icon = new IconInfo { IsFolder = true };
        var propertyNames = new List<string?>();
        icon.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        icon.FolderSize = ShortcutFolderSize.Large;

        Assert.AreEqual(ShortcutFolderSize.Large, icon.FolderSize);
        Assert.AreEqual(2, icon.FolderColumnSpan_NoWrite);
        Assert.AreEqual(2, icon.FolderRowSpan_NoWrite);
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderSize));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderColumnSpan_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderRowSpan_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderPreviewItems_NoWrite));
    }

    [TestMethod]
    public void FolderPreviewItems_UsesPhoneStyleCoverLimits()
    {
        var folder = new IconInfo { IsFolder = true, FolderSize = ShortcutFolderSize.Small };
        for (int i = 0; i < 7; i++)
        {
            folder.FolderItems.Add(new IconInfo { Name = $"App {i}" });
        }

        Assert.AreEqual(4, folder.FolderPreviewItems_NoWrite.Count);
        Assert.AreEqual(3, folder.FolderPreviewOverflowCount_NoWrite);
        Assert.IsTrue(folder.HasFolderPreviewOverflow_NoWrite);

        folder.FolderSize = ShortcutFolderSize.Large;

        Assert.AreEqual(6, folder.FolderPreviewItems_NoWrite.Count);
        Assert.AreEqual(1, folder.FolderPreviewOverflowCount_NoWrite);
    }

    [TestMethod]
    public void LauncherGridPosition_NormalizesNegativeValues()
    {
        var icon = new IconInfo();

        icon.LauncherGridColumn = 3;
        icon.LauncherGridRow = 2;

        Assert.IsTrue(icon.HasManualLauncherGridPosition_NoWrite);
        Assert.AreEqual(3, icon.LauncherGridColumn);
        Assert.AreEqual(2, icon.LauncherGridRow);

        icon.LauncherGridColumn = -10;

        Assert.AreEqual(-1, icon.LauncherGridColumn);
        Assert.IsFalse(icon.HasManualLauncherGridPosition_NoWrite);
    }

    [TestMethod]
    public void FolderItemsChange_RaisesFolderCountAndRefreshesContent()
    {
        var folder = new IconInfo { Name = "Tools", IsFolder = true };
        var propertyNames = new List<string?>();
        folder.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        folder.FolderItems.Add(new IconInfo { Name = "App" });

        Assert.AreEqual(1, folder.FolderItemCount_NoWrite);
        Assert.IsTrue(folder.Content.Contains("1 个项目"));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderItemCount_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderPreviewItems_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.FolderPreviewOverflowCount_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.HasFolderPreviewOverflow_NoWrite));
        CollectionAssert.Contains(propertyNames, nameof(IconInfo.Content));
    }

    [TestMethod]
    public void RenamingFolder_KeepsFolderContentFormat()
    {
        var folder = new IconInfo { Name = "Old", IsFolder = true };
        folder.FolderItems.Add(new IconInfo { Name = "App" });

        folder.Name = "New";

        Assert.IsTrue(folder.Content.StartsWith("New"));
        Assert.IsTrue(folder.Content.Contains("1 个项目"));
        Assert.IsFalse(folder.Content.Contains("使用次数"));
    }
}
