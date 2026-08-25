namespace raindesk.Features.Workspace.Presentation;

internal readonly record struct WorkspaceGroupTabWidthMetrics(
    double MinimumWidth,
    double HorizontalChrome,
    double IconSlotWidth,
    double ColumnSpacing,
    double EditContentWidth,
    double RightMargin)
{
    public static WorkspaceGroupTabWidthMetrics Default { get; } = new(
        MinimumWidth: 48d,
        HorizontalChrome: 28d,
        IconSlotWidth: 20d,
        ColumnSpacing: 6d,
        EditContentWidth: 88d,
        RightMargin: 8d);
}

internal static class WorkspaceGroupTabWidthCalculator
{
    public static double CalculateNatural(
        double labelDesiredWidth,
        WorkspaceGroupTabWidthMetrics metrics) =>
        Math.Max(
            metrics.MinimumWidth,
            Math.Ceiling(
                Math.Max(0d, labelDesiredWidth)
                + metrics.HorizontalChrome
                + metrics.IconSlotWidth
                + metrics.ColumnSpacing));

    public static double CalculateEdit(WorkspaceGroupTabWidthMetrics metrics) =>
        Math.Max(metrics.MinimumWidth, metrics.EditContentWidth);
}
