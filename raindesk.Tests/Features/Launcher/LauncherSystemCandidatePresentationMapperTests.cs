using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherSystemCandidatePresentationMapperTests
{
    [TestMethod]
    public void PresentationItem_MapsDirectlyBackToLauncherDraftWithoutPathReparse()
    {
        var icon = new IconInfo
        {
            Name_NoWrite = "Terminal",
            Path_NoWrite = "C:\\Tools\\terminal.exe",
            StartArg_NoWrite = "--safe",
            LnkPath_NoWrite = "C:\\Menu\\Terminal.lnk",
            ImageByteArr_NoWrite = new byte[] { 1, 2, 3 }
        };

        LauncherShortcutDraft draft = LauncherSystemCandidatePresentationMapper.ToDraft(icon);

        Assert.AreEqual("Terminal", draft.Name);
        Assert.AreEqual("C:\\Tools\\terminal.exe", draft.Path);
        Assert.AreEqual("--safe", draft.StartArgument);
        Assert.AreEqual("C:\\Menu\\Terminal.lnk", draft.LinkPath);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, draft.ImageBytes);
    }
}
