using raindesk.Features.Launcher.Contracts;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

internal static class LauncherSystemCandidatePresentationMapper
{
    public static IconInfo ToIconInfo(LauncherSystemCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var icon = new IconInfo
        {
            Name_NoWrite = candidate.Name,
            Path_NoWrite = candidate.Path ?? string.Empty,
            StartArg_NoWrite = candidate.StartArgument ?? string.Empty,
            LnkPath_NoWrite = candidate.LinkPath ?? string.Empty,
            Content_NoWrite = candidate.Content ?? candidate.Name
        };
        if (candidate.ImageBytes is { Length: > 0 } bytes)
        {
            icon.ImageByteArr_NoWrite = bytes;
            icon.DefaultImage_NoWrite = bytes;
        }
        return icon;
    }

    public static LauncherShortcutDraft ToDraft(IconInfo icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return new LauncherShortcutDraft(
            icon.Name_NoWrite,
            icon.Path_NoWrite,
            icon.StartArg_NoWrite,
            icon.LnkPath_NoWrite,
            icon.ImageByteArr_NoWrite);
    }
}
