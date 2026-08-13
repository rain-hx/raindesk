using rainspace.Features.Launcher.Application;
using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Presentation;

internal static class LauncherShortcutDropTraitsAdapter
{
    public static LauncherShortcutDropTraits From(IconInfo icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return new LauncherShortcutDropTraits(
            icon.IsFolder,
            icon.IsAddShortcut_NoWrite,
            icon.IsDragYieldPlaceholder_NoWrite);
    }

    public static bool CanCreateFolderFrom(IconInfo dragged, IconInfo target) =>
        LauncherFolderDropPolicy.CanCreateFolder(
            From(dragged),
            From(target),
            ReferenceEquals(dragged, target));

    public static bool CanMoveIntoFolder(IconInfo dragged, IconInfo target) =>
        LauncherFolderDropPolicy.CanMoveIntoFolder(
            From(dragged),
            From(target),
            ReferenceEquals(dragged, target));
}
