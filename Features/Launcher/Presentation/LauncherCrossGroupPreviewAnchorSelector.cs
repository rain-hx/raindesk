namespace raindesk.Features.Launcher.Presentation;

internal enum LauncherCrossGroupPreviewAnchorKind
{
    None,
    Placeholder,
    DraggedItem
}

internal readonly record struct LauncherCrossGroupPreviewAnchor<T>(
    T? Item,
    LauncherCrossGroupPreviewAnchorKind Kind)
    where T : class;

internal static class LauncherCrossGroupPreviewAnchorSelector
{
    public static LauncherCrossGroupPreviewAnchor<T> Select<T>(
        IReadOnlyCollection<T> hostItems,
        IReadOnlyList<T> placeholders,
        IReadOnlyList<T> draggedItems)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(hostItems);
        ArgumentNullException.ThrowIfNull(placeholders);
        ArgumentNullException.ThrowIfNull(draggedItems);

        T? placeholder = placeholders.FirstOrDefault(hostItems.Contains);
        if (placeholder != null)
        {
            return new LauncherCrossGroupPreviewAnchor<T>(
                placeholder,
                LauncherCrossGroupPreviewAnchorKind.Placeholder);
        }

        T? draggedItem = draggedItems.FirstOrDefault(hostItems.Contains);
        return draggedItem == null
            ? new LauncherCrossGroupPreviewAnchor<T>(
                null,
                LauncherCrossGroupPreviewAnchorKind.None)
            : new LauncherCrossGroupPreviewAnchor<T>(
                draggedItem,
                LauncherCrossGroupPreviewAnchorKind.DraggedItem);
    }
}
