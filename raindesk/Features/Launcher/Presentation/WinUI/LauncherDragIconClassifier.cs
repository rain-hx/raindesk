using System;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Static classification helpers for Launcher icons during drag operations.
/// Extracted from RightCardControl to reduce page code size.
/// </summary>
internal static class LauncherDragIconClassifier
{
    public static bool IsDragYieldPlaceholder(IconInfo? icon)
    {
        return icon?.IsDragYieldPlaceholder_NoWrite == true;
    }

    public static bool IsAddShortcutPlaceholder(IconInfo? icon)
    {
        if (icon == null) return false;
        if (icon.IsAddShortcut_NoWrite) return true;

        return string.Equals(icon.Name_NoWrite, "添加快捷方式", StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(icon.Path_NoWrite)
            && string.IsNullOrWhiteSpace(icon.LnkPath_NoWrite)
            && string.IsNullOrWhiteSpace(icon.StartArg_NoWrite);
    }

    public static bool IsDataShortcutIcon(IconInfo? icon)
    {
        return icon != null && !IsAddShortcutPlaceholder(icon) && !IsDragYieldPlaceholder(icon);
    }
}
