namespace rainspace.Features.Launcher.Contracts;

public sealed record LauncherGridPlacement(
    string ShortcutId,
    int Column,
    int Row);

public sealed record UpdateLauncherGridLayoutCommand(
    IReadOnlyList<LauncherGridPlacement> Placements);

public enum LauncherShortcutOrderScope
{
    Group,
    AllGroups
}

public sealed record UpdateLauncherShortcutOrderCommand(
    LauncherShortcutOrderScope Scope,
    IReadOnlyList<string> OrderedShortcutIds,
    string? TargetGroupId = null);
