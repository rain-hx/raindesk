namespace rainspace.Features.Launcher.Presentation;

internal enum LauncherCrossGroupDropTargetSource
{
    None,
    Pointer,
    Hover,
    Preview
}

internal readonly record struct LauncherCrossGroupDropTarget<T>(
    T? Target,
    LauncherCrossGroupDropTargetSource Source)
    where T : class;

internal readonly record struct LauncherCrossGroupDropPreparation(
    bool CanCommit,
    bool ShouldFinalizeBatchPlaceholder);

/// <summary>
/// Pure target-priority and batch-preview insertion rules shared by every cross-group drop entry point.
/// </summary>
internal static class LauncherCrossGroupDropPlanner
{
    public static LauncherCrossGroupDropTarget<T> ResolveAtDragEnd<T>(
        T? pointerTarget,
        bool previewActive,
        T? previewTarget,
        T? hoverTarget,
        bool pointerOverGroupBar)
        where T : class
    {
        if (pointerTarget != null)
        {
            return new LauncherCrossGroupDropTarget<T>(
                pointerTarget,
                LauncherCrossGroupDropTargetSource.Pointer);
        }

        if (!previewActive || !pointerOverGroupBar)
        {
            return None<T>();
        }

        if (previewTarget != null)
        {
            return new LauncherCrossGroupDropTarget<T>(
                previewTarget,
                LauncherCrossGroupDropTargetSource.Preview);
        }

        return hoverTarget != null
            ? new LauncherCrossGroupDropTarget<T>(
                hoverTarget,
                LauncherCrossGroupDropTargetSource.Hover)
            : None<T>();
    }

    public static LauncherCrossGroupDropTarget<T> ResolveDropEvent<T>(
        T? directTarget,
        T? hoverTarget,
        T? previewTarget)
        where T : class
    {
        if (directTarget != null)
        {
            return new LauncherCrossGroupDropTarget<T>(
                directTarget,
                LauncherCrossGroupDropTargetSource.Pointer);
        }

        if (hoverTarget != null)
        {
            return new LauncherCrossGroupDropTarget<T>(
                hoverTarget,
                LauncherCrossGroupDropTargetSource.Hover);
        }

        return previewTarget != null
            ? new LauncherCrossGroupDropTarget<T>(
                previewTarget,
                LauncherCrossGroupDropTargetSource.Preview)
            : None<T>();
    }

    public static LauncherCrossGroupDropTarget<T> ResolveOnComplete<T>(
        bool previewActive,
        T? previewTarget)
        where T : class =>
        previewActive && previewTarget != null
            ? new LauncherCrossGroupDropTarget<T>(
                previewTarget,
                LauncherCrossGroupDropTargetSource.Preview)
            : None<T>();

    public static LauncherCrossGroupDropPreparation PrepareCommit(
        bool targetAccepted,
        bool hasBatchPlaceholders) =>
        new(
            CanCommit: targetAccepted,
            ShouldFinalizeBatchPlaceholder: targetAccepted && hasBatchPlaceholders);

    public static int? ResolvePreviewInsertIndex(
        bool hasBatchPlaceholders,
        int batchInsertIndex,
        bool previewTargetCompatible) =>
        hasBatchPlaceholders
            && batchInsertIndex >= 0
            && previewTargetCompatible
                ? batchInsertIndex
                : null;

    private static LauncherCrossGroupDropTarget<T> None<T>()
        where T : class =>
        new(null, LauncherCrossGroupDropTargetSource.None);
}
