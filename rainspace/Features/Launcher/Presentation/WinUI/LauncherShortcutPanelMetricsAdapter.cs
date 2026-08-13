using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using rainspace.Features.Launcher.Application;

namespace rainspace.Features.Launcher.Presentation.WinUI;

internal static class LauncherShortcutPanelMetricsAdapter
{
    public static bool Apply(Panel? panel, ShortcutLayoutMetrics metrics)
    {
        double panelWidth = metrics.ColumnCount * metrics.SlotWidth;
        switch (panel)
        {
            case ItemsWrapGrid wrapGrid:
                wrapGrid.ItemWidth = metrics.SlotWidth;
                wrapGrid.ItemHeight = metrics.SlotHeight;
                wrapGrid.MaximumRowsOrColumns = Math.Max(1, metrics.ColumnCount);
                wrapGrid.MinWidth = panelWidth;
                wrapGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                return true;

            case LauncherGridPanel launcherPanel:
                launcherPanel.ColumnCount = Math.Max(1, metrics.ColumnCount);
                launcherPanel.ItemWidth = metrics.SlotWidth;
                launcherPanel.ItemHeight = metrics.SlotHeight;
                launcherPanel.MinWidth = panelWidth;
                launcherPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                return true;

            case VariableSizedWrapGrid variableGrid:
                variableGrid.ItemWidth = metrics.SlotWidth;
                variableGrid.ItemHeight = metrics.SlotHeight;
                variableGrid.MaximumRowsOrColumns = Math.Max(1, metrics.ColumnCount);
                variableGrid.MinWidth = panelWidth;
                variableGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                return true;

            default:
                return false;
        }
    }

    public static bool Invalidate(Panel? panel)
    {
        if (panel is not (ItemsWrapGrid or LauncherGridPanel or VariableSizedWrapGrid))
        {
            return false;
        }

        panel.InvalidateMeasure();
        panel.InvalidateArrange();
        return true;
    }

    public static bool ApplyAndRefresh(ListView? listView, ShortcutLayoutMetrics metrics)
    {
        if (listView == null || !Apply(listView.ItemsPanelRoot, metrics))
        {
            return false;
        }

        Invalidate(listView.ItemsPanelRoot);
        listView.UpdateLayout();
        return true;
    }
}
