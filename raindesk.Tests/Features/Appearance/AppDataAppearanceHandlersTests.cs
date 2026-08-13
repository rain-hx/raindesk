using raindesk.Features.Appearance.Contracts;
using raindesk.Features.Appearance.Infrastructure;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Appearance;

[TestClass]
public class AppDataAppearanceHandlersTests
{
    [TestMethod]
    public void SetTheme_UpdatesTypedSnapshotAndRequestsSave()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var save = new RecordingSaveService();
        var commands = new AppDataAppearanceCommandHandler(store, save);

        AppearanceCommandResult result = commands.SetTheme(
            new SetAppearanceThemeCommand(AppearanceThemeMode.Dark));
        AppearanceSnapshot snapshot = new AppDataAppearanceQueryHandler(store).GetCurrent();

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual(AppearanceThemeMode.Dark, snapshot.ThemeMode);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void SetBackgroundImage_ClonesCallerBuffer()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var save = new RecordingSaveService();
        var commands = new AppDataAppearanceCommandHandler(store, save);
        byte[] bytes = { 1, 2, 3 };

        AppearanceCommandResult result = commands.SetBackgroundImage(
            new SetBackgroundImageCommand(bytes, "custom.png"));
        bytes[0] = 99;

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, store.Current.AppConfig.ImageByteArr);
        Assert.AreEqual("custom.png", store.Current.AppConfig.BacImgName);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void UpdateEffects_InvalidRange_IsRejectedWithoutMutation()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        int originalOpacity = store.Current.AppConfig.BgOpacity;
        var save = new RecordingSaveService();
        var commands = new AppDataAppearanceCommandHandler(store, save);

        AppearanceCommandResult result = commands.UpdateEffects(
            new UpdateAppearanceEffectsCommand(101, true, 10, false));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(originalOpacity, store.Current.AppConfig.BgOpacity);
        Assert.AreEqual(0, save.DebouncedCount);
    }

    [TestMethod]
    public void SetIconTitleVisibility_UsesTypedSnapshotAndSavesOnlyOnChange()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var save = new RecordingSaveService();
        var commands = new AppDataAppearanceCommandHandler(store, save);

        AppearanceCommandResult hidden = commands.SetIconTitleVisibility(
            new SetIconTitleVisibilityCommand(false));
        AppearanceCommandResult duplicate = commands.SetIconTitleVisibility(
            new SetIconTitleVisibilityCommand(false));
        AppearanceSnapshot snapshot = new AppDataAppearanceQueryHandler(store).GetCurrent();

        Assert.IsTrue(hidden.Success && hidden.Changed);
        Assert.IsTrue(duplicate.Success && !duplicate.Changed);
        Assert.IsFalse(snapshot.ShowIconTitles);
        Assert.AreEqual(false, store.Current.AppConfig.ShowIconTitle);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void GradientCommands_UseTypedBoundaryAndPersistChanges()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var save = new RecordingSaveService();
        var commands = new AppDataAppearanceCommandHandler(store, save);

        AppearanceCommandResult select = commands.SelectGradient(
            new SelectAppearanceGradientCommand("preset", "Ocean", "#001122", "#334455"));
        AppearanceCommandResult color = commands.SetColor(
            new SetAppearanceColorCommand(AppearanceColorTarget.GradientEnd, "#667788"));
        AppearanceGradientSnapshot snapshot = new AppDataAppearanceQueryHandler(store).GetGradient();

        Assert.IsTrue(select.Success && color.Success);
        Assert.AreEqual("Ocean", snapshot.Name);
        Assert.AreEqual("#001122", snapshot.StartColor);
        Assert.AreEqual("#667788", snapshot.EndColor);
        Assert.AreEqual(2, save.DebouncedCount);
    }

    [TestMethod]
    public void CustomGradient_CanBeAddedAndDeletedByStableId()
    {
        var store = new AppDataStore();
        store.Replace(new AppData());
        var save = new RecordingSaveService();
        var commands = new AppDataAppearanceCommandHandler(store, save);

        AppearanceCommandResult add = commands.AddCustomGradient(
            new AddCustomAppearanceGradientCommand("Sunset", "#112233", "#445566"));
        GradientBGParam added = store.Current.AppConfig.CustomBGParams.Single();
        AppearanceCommandResult delete = commands.DeleteCustomGradient(
            new DeleteCustomAppearanceGradientCommand(added.Id, added.Color1, added.Color2));

        Assert.IsTrue(add.Success && delete.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(added.Id));
        Assert.AreEqual(0, store.Current.AppConfig.CustomBGParams.Count);
        Assert.AreEqual(2, save.DebouncedCount);
    }

    private sealed class RecordingSaveService : ISaveService
    {
        public int DebouncedCount { get; private set; }
        public void SaveNow() { }
        public void SaveDebounced() => DebouncedCount++;
        public void CancelPendingSave() { }
        public void Flush() { }
    }
}
