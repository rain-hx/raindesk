namespace rainspace.Features.Launcher.Presentation;

internal readonly record struct LauncherAnimationOffset(double X, double Y);

/// <summary>
/// 批量落位动画的纯时间线规划，不依赖 Storyboard 或 WinUI 容器。
/// </summary>
internal static class LauncherBatchDropAnimationPlanner
{
    public static LauncherAnimationOffset GetReleaseOffset(int itemIndex, int columnCount)
    {
        int safeIndex = Math.Max(0, itemIndex);
        int safeColumns = Math.Max(1, columnCount);
        return new LauncherAnimationOffset(
            ((safeIndex % 3) - 1) * 10d,
            -8d - (safeIndex / safeColumns) * 4d);
    }

    public static TimeSpan GetStepDelay(int step, TimeSpan stagger)
    {
        double milliseconds = Math.Max(0, step) * Math.Max(0d, stagger.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    public static TimeSpan GetCleanupDelay(
        int startedAnimations,
        int trailingCount,
        int droppedCount,
        TimeSpan animationDuration,
        TimeSpan stagger,
        TimeSpan padding)
    {
        int totalSteps = Math.Max(
            Math.Max(0, startedAnimations),
            Math.Max(0, trailingCount) + Math.Max(0, droppedCount));
        return NonNegative(animationDuration)
            + GetStepDelay(Math.Max(0, totalSteps - 1), stagger)
            + NonNegative(padding);
    }

    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;
}
