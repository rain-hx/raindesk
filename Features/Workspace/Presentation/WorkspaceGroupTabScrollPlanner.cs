namespace raindesk.Features.Workspace.Presentation;

internal readonly record struct WorkspaceGroupTabScrollPlan(
    bool ShouldScroll,
    double TargetOffset);

internal static class WorkspaceGroupTabScrollPlanner
{
    public static WorkspaceGroupTabScrollPlan Plan(
        double tabLeft,
        double tabWidth,
        double viewportOffset,
        double viewportWidth,
        double scrollableWidth,
        double minimumChange = 0.5d)
    {
        if (tabWidth <= 0d || viewportWidth <= 0d)
        {
            return new WorkspaceGroupTabScrollPlan(false, viewportOffset);
        }

        double tabRight = tabLeft + tabWidth;
        double viewportRight = viewportOffset + viewportWidth;
        double targetOffset;
        if (tabLeft < viewportOffset)
        {
            targetOffset = tabLeft;
        }
        else if (tabRight > viewportRight)
        {
            targetOffset = tabRight - viewportWidth;
        }
        else
        {
            return new WorkspaceGroupTabScrollPlan(false, viewportOffset);
        }

        targetOffset = Math.Clamp(targetOffset, 0d, Math.Max(0d, scrollableWidth));
        return Math.Abs(targetOffset - viewportOffset) < Math.Max(0d, minimumChange)
            ? new WorkspaceGroupTabScrollPlan(false, viewportOffset)
            : new WorkspaceGroupTabScrollPlan(true, targetOffset);
    }
}
