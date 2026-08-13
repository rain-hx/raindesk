using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Presentation;

internal static class LauncherShortcutEditPresentationMapper
{
    public static LauncherShortcutEditDraft ToDraft(IconInfo icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return new LauncherShortcutEditDraft(
            icon.IconId,
            icon.Name,
            icon.Path,
            icon.RelativePath,
            icon.StartArg,
            icon.AdminStartUp,
            icon.ImageByteArr,
            icon.DefaultImage);
    }
}
