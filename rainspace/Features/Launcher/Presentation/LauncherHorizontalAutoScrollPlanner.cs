namespace rainspace.Features.Launcher.Presentation;

internal readonly record struct LauncherHorizontalAutoScrollPlan(
    bool ShouldScroll,
    double TargetOffset);

/// <summary>
/// 水平拖拽边缘自动滚动的纯规划器。
/// </summary>
internal static class LauncherHorizontalAutoScrollPlanner
{
    public static LauncherHorizontalAutoScrollPlan Plan(
        double pointerX,
        double viewportWidth,
        double currentOffset,
        double scrollableWidth,
        double edgeThreshold,
        double scrollStep,
        double minimumChange = 0.5d)
    {
        if (viewportWidth <= 0d
            || scrollableWidth <= 0d
            || edgeThreshold <= 0d
            || scrollStep <= 0d)
        {
            return new LauncherHorizontalAutoScrollPlan(false, currentOffset);
        }

        double targetOffset;
        if (pointerX < edgeThreshold)
        {
            targetOffset = currentOffset - scrollStep;
        }
        else if (pointerX > viewportWidth - edgeThreshold)
        {
            targetOffset = currentOffset + scrollStep;
        }
        else
        {
            return new LauncherHorizontalAutoScrollPlan(false, currentOffset);
        }

        targetOffset = Math.Clamp(targetOffset, 0d, scrollableWidth);
        return Math.Abs(targetOffset - currentOffset) < Math.Max(0d, minimumChange)
            ? new LauncherHorizontalAutoScrollPlan(false, currentOffset)
            : new LauncherHorizontalAutoScrollPlan(true, targetOffset);
    }
}
