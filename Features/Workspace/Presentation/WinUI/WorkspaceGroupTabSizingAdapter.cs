using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Util;
using Windows.Foundation;

namespace raindesk.Features.Workspace.Presentation.WinUI;

internal static class WorkspaceGroupTabSizingAdapter
{
    public static void ApplyEditWidth(
        ListViewItem item,
        WorkspaceGroupTabWidthMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Height = WorkspaceGroupTabWidthCalculator.CalculateEdit(metrics);
        item.Margin = new Thickness(0d, 0d, 0d, metrics.RightMargin);
    }

    public static bool ApplyNaturalWidth(
        ListViewItem item,
        WorkspaceGroupTabWidthMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.ContentTemplateRoot is not FrameworkElement root)
        {
            return false;
        }

        TextBlock? label = MenuGeometryHelper.FindDescendantByName<TextBlock>(root, "GroupTabLabel")
            ?? FindFirstTextBlock(root);
        if (label == null)
        {
            return false;
        }

        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        item.Height = WorkspaceGroupTabWidthCalculator.CalculateNatural(
            label.DesiredSize.Width,
            metrics);
        item.Margin = new Thickness(0d, 0d, 0d, metrics.RightMargin);
        return true;
    }

    private static TextBlock? FindFirstTextBlock(DependencyObject root)
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock textBlock)
            {
                return textBlock;
            }

            TextBlock? nested = FindFirstTextBlock(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
