using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherShortcutEditDraftTests
{
    [TestMethod]
    public void ConstructorAndAccessors_DefensivelyCloneImageBuffers()
    {
        byte[] image = { 1, 2 };
        byte[] fallback = { 3, 4 };
        var draft = CreateDraft(image, fallback);

        image[0] = 9;
        fallback[0] = 9;
        byte[] exposedImage = draft.ImageBytes!;
        byte[] exposedFallback = draft.DefaultImageBytes!;
        exposedImage[1] = 9;
        exposedFallback[1] = 9;

        CollectionAssert.AreEqual(new byte[] { 1, 2 }, draft.ImageBytes);
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, draft.DefaultImageBytes);
    }

    [TestMethod]
    public void ReplaceImage_BuildsExplicitAtomicUpdateWithoutMutatingInputBuffer()
    {
        var draft = CreateDraft(new byte[] { 1 }, new byte[] { 3 });
        byte[] replacement = { 7, 8 };

        draft.ReplaceImage(replacement);
        UpdateLauncherShortcutCommand command = draft.CreateUpdateCommand(
            "Renamed",
            "new-path",
            "--safe",
            adminStartUp: true);
        replacement[0] = 0;

        Assert.AreEqual("shortcut", command.ShortcutId);
        Assert.AreEqual("Renamed", command.Properties.Name);
        Assert.AreEqual("new-path", command.Properties.Path);
        Assert.AreEqual("--safe", command.Properties.StartArgument);
        Assert.IsTrue(command.Properties.AdminStartUp);
        Assert.IsTrue(command.Properties.ReplaceImage);
        CollectionAssert.AreEqual(new byte[] { 7, 8 }, command.Properties.ImageBytes);
    }

    [TestMethod]
    public void RestoreDefaultImage_IsStagedUntilCommandCreation()
    {
        var draft = CreateDraft(new byte[] { 1 }, new byte[] { 4, 5 });

        bool restored = draft.RestoreDefaultImage();
        UpdateLauncherShortcutCommand command = draft.CreateUpdateCommand(
            draft.Name,
            draft.Path,
            draft.StartArgument,
            draft.AdminStartUp);

        Assert.IsTrue(restored);
        Assert.IsTrue(command.Properties.ReplaceImage);
        CollectionAssert.AreEqual(new byte[] { 4, 5 }, command.Properties.ImageBytes);
    }

    private static LauncherShortcutEditDraft CreateDraft(byte[] image, byte[] fallback) => new(
        "shortcut",
        "App",
        "path",
        "relative",
        "--old",
        adminStartUp: false,
        image,
        fallback);
}
