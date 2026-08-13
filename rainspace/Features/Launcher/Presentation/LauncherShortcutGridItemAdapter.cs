using rainspace.Features.Launcher.Application;
using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Presentation;

internal static class LauncherShortcutGridItemAdapter
{
    public static LauncherGridItem Create(
        IconInfo icon,
        int columnCount,
        bool preserveManualPosition = true)
    {
        ArgumentNullException.ThrowIfNull(icon);
        int safeColumnCount = Math.Max(1, columnCount);
        int columnSpan = ShortcutLayoutCalculator.ClampFolderColumnSpan(
            GetColumnSpan(icon),
            safeColumnCount);
        int rowSpan = GetRowSpan(icon);

        return preserveManualPosition && icon.HasManualLauncherGridPosition_NoWrite
            ? new LauncherGridItem(
                icon,
                columnSpan,
                rowSpan,
                icon.LauncherGridColumn,
                icon.LauncherGridRow)
            : new LauncherGridItem(icon, columnSpan, rowSpan);
    }

    private static int GetColumnSpan(IconInfo icon)
    {
        if (icon.IsDragYieldPlaceholder_NoWrite)
        {
            return Math.Max(1, icon.DragPlaceholderColumnSpan_NoWrite);
        }

        return icon.IsFolder ? Math.Max(1, icon.FolderColumnSpan_NoWrite) : 1;
    }

    private static int GetRowSpan(IconInfo icon)
    {
        if (icon.IsDragYieldPlaceholder_NoWrite)
        {
            return Math.Max(1, icon.DragPlaceholderRowSpan_NoWrite);
        }

        return icon.IsFolder ? Math.Max(1, icon.FolderRowSpan_NoWrite) : 1;
    }
}
