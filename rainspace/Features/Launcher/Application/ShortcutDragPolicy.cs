using System;
using System.Collections.Generic;
using System.Linq;

namespace rainspace.Features.Launcher.Application;

internal readonly record struct ShortcutDragStartItems<T>(
    IReadOnlyList<T> Items,
    bool Cancel);

internal readonly record struct ShortcutDragStartPolicy(
    bool UseBatchPlaceholderDrag,
    bool KeepSourceItemInCollection,
    bool RestrictDragItemsToPlaceholder);

internal static class ShortcutDragPolicy
{
    public static ShortcutDragStartItems<T> ResolveDragStartItems<T>(
        IReadOnlyList<T>? requestedItems,
        Func<T, bool> isCancelItem,
        Func<T, bool> isDragItem)
    {
        ArgumentNullException.ThrowIfNull(isCancelItem);
        ArgumentNullException.ThrowIfNull(isDragItem);

        if (requestedItems == null || requestedItems.Count == 0)
        {
            return new ShortcutDragStartItems<T>(Array.Empty<T>(), Cancel: true);
        }

        foreach (T item in requestedItems)
        {
            if (item is not null && isCancelItem(item))
            {
                return new ShortcutDragStartItems<T>(Array.Empty<T>(), Cancel: true);
            }
        }

        List<T> dragItems = requestedItems
            .Where(item => item is not null && isDragItem(item))
            .ToList();

        return dragItems.Count == 0
            ? new ShortcutDragStartItems<T>(Array.Empty<T>(), Cancel: true)
            : new ShortcutDragStartItems<T>(dragItems, Cancel: false);
    }

    public static ShortcutDragStartPolicy ForDragCount(int draggedCount)
    {
        bool batch = draggedCount > 1;
        return new ShortcutDragStartPolicy(
            UseBatchPlaceholderDrag: batch,
            KeepSourceItemInCollection: !batch,
            RestrictDragItemsToPlaceholder: false);
    }

    public static bool ShouldSuppressReorderForFolderTarget(
        int draggedCount,
        bool hasValidFolderDropTarget)
    {
        return draggedCount == 1 && hasValidFolderDropTarget;
    }

    public static bool ShouldApplyLiveReorderPreview(
        int draggedCount,
        bool showingAllGroups,
        bool hasFolderDropIntent,
        bool isCrossGroupPreviewActive)
    {
        return draggedCount == 1
            && !hasFolderDropIntent
            && !isCrossGroupPreviewActive;
    }
}
