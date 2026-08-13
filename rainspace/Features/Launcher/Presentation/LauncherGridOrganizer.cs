using rainspace.ViewModel;
using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;

namespace rainspace.Features.Launcher.Presentation;

internal readonly record struct LauncherGridOrganizeResult(int OrganizedCount);

internal static class LauncherGridOrganizer
{
    public static IReadOnlyList<LauncherGridPlacement> Plan(
        IEnumerable<IconInfo> icons,
        int columnCount,
        bool preserveExistingVisualOrder = true)
    {
        ArgumentNullException.ThrowIfNull(icons);

        int safeColumnCount = Math.Max(1, columnCount);
        IEnumerable<IconInfo> sourceIcons = icons
            .Where(icon => icon != null
                && !icon.IsAddShortcut_NoWrite
                && !icon.IsDragYieldPlaceholder_NoWrite);
        List<IconInfo> orderedIcons = preserveExistingVisualOrder
            ? sourceIcons
                .Select((icon, index) => new { Icon = icon, Index = index })
                .OrderBy(item => item.Icon.HasManualLauncherGridPosition_NoWrite ? 0 : 1)
                .ThenBy(item => item.Icon.HasManualLauncherGridPosition_NoWrite
                    ? item.Icon.LauncherGridRow
                    : int.MaxValue)
                .ThenBy(item => item.Icon.HasManualLauncherGridPosition_NoWrite
                    ? item.Icon.LauncherGridColumn
                    : int.MaxValue)
                .ThenBy(item => item.Index)
                .Select(item => item.Icon)
                .ToList()
            : sourceIcons.ToList();
        var occupied = new HashSet<LauncherGridCell>();
        var placements = new List<LauncherGridPlacement>();

        foreach (IconInfo icon in orderedIcons)
        {
            int columnSpan = GetColumnSpan(icon, safeColumnCount);
            int rowSpan = GetRowSpan(icon);
            LauncherGridCell cell = FindFirstAvailableCell(
                occupied,
                safeColumnCount,
                columnSpan,
                rowSpan);

            if ((icon.LauncherGridColumn != cell.Column || icon.LauncherGridRow != cell.Row)
                && !string.IsNullOrWhiteSpace(icon.IconId))
            {
                placements.Add(new LauncherGridPlacement(icon.IconId, cell.Column, cell.Row));
            }
            Occupy(occupied, cell, columnSpan, rowSpan);
        }

        return placements;
    }

    public static LauncherGridOrganizeResult Organize(
        IEnumerable<IconInfo> icons,
        int columnCount,
        bool preserveExistingVisualOrder = true)
    {
        ArgumentNullException.ThrowIfNull(icons);

        List<IconInfo> iconList = icons.Where(icon => icon != null).ToList();
        IReadOnlyList<LauncherGridPlacement> placements = Plan(
            iconList,
            columnCount,
            preserveExistingVisualOrder);
        var iconsById = iconList
            .Where(icon => !string.IsNullOrWhiteSpace(icon.IconId))
            .GroupBy(icon => icon.IconId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        int organizedCount = 0;
        foreach (LauncherGridPlacement placement in placements)
        {
            if (iconsById.TryGetValue(placement.ShortcutId, out IconInfo? icon))
            {
                organizedCount += ApplyPosition(
                    icon,
                    new LauncherGridCell(placement.Column, placement.Row));
            }
        }

        return new LauncherGridOrganizeResult(organizedCount);
    }

    private static int ApplyPosition(IconInfo icon, LauncherGridCell cell)
    {
        return icon.SetLauncherGridPositionSilently(cell.Column, cell.Row) ? 1 : 0;
    }

    private static int GetColumnSpan(IconInfo icon, int columnCount)
        => icon.IsFolder
            ? Math.Clamp(icon.FolderColumnSpan_NoWrite, 1, columnCount)
            : 1;

    private static int GetRowSpan(IconInfo icon)
        => icon.IsFolder ? Math.Max(1, icon.FolderRowSpan_NoWrite) : 1;

    private static LauncherGridCell FindFirstAvailableCell(
        HashSet<LauncherGridCell> occupied,
        int columnCount,
        int columnSpan,
        int rowSpan)
    {
        for (int row = 0; ; row++)
        {
            for (int column = 0; column <= columnCount - columnSpan; column++)
            {
                if (CanPlace(occupied, column, row, columnSpan, rowSpan))
                {
                    return new LauncherGridCell(column, row);
                }
            }
        }
    }

    private static bool CanPlace(
        HashSet<LauncherGridCell> occupied,
        int startColumn,
        int startRow,
        int columnSpan,
        int rowSpan)
    {
        for (int row = startRow; row < startRow + rowSpan; row++)
        {
            for (int column = startColumn; column < startColumn + columnSpan; column++)
            {
                if (occupied.Contains(new LauncherGridCell(column, row)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void Occupy(
        HashSet<LauncherGridCell> occupied,
        LauncherGridCell cell,
        int columnSpan,
        int rowSpan)
    {
        for (int row = cell.Row; row < cell.Row + rowSpan; row++)
        {
            for (int column = cell.Column; column < cell.Column + columnSpan; column++)
            {
                occupied.Add(new LauncherGridCell(column, row));
            }
        }
    }
}
