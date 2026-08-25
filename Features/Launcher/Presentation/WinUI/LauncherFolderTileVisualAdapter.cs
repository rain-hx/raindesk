using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using raindesk.Constant;
using raindesk.Features.Launcher.Application;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

public static class LauncherFolderTileVisualAdapter
{
    private const double ShortcutIconSize = 64d;
    private const double LabelBlockHeight = 22d;
    private const double StackSpacing = 8d;
    private const double PreviewSafePadding = 6d;
    private const double PreviewIconGap = 4d;
    private const double PreviewCellSize = 16d;

    public static void Apply(
        ListView listView,
        ListViewItem item,
        IconInfo icon,
        int columnCount,
        double availableWidth)
    {
        if (!icon.IsFolder || item.ContentTemplateRoot is not FrameworkElement templateRoot)
        {
            return;
        }

        bool supportsVariableSize = LauncherShortcutItemSpanAdapter.SupportsVariableSize(listView.ItemsPanelRoot);
        ShortcutLayoutMetrics metrics = ShortcutLayoutCalculator.Calculate(availableWidth);
        // 文件夹已统一为 1x1；视觉模板也必须使用单格尺寸，否则标题会落到
        // LauncherGridPanel 分配区域之外并被裁剪。
        const int columnSpan = 1;
        const int rowSpan = 1;
        ShortcutTileDisplaySize tileSize = ShortcutLayoutCalculator.CalculatePanelTileDisplaySize(
            metrics,
            columnSpan,
            rowSpan,
            supportsVariableSizedItems: supportsVariableSize);

        // 统一按实际瓦片尺寸设置模板根，保证文件夹标题区域始终有布局空间；
        // 不依赖 ResetTemplate 的“清除高度”路径，避免标题被挤出可视区域。
        templateRoot.Width = tileSize.Width;
        templateRoot.Height = tileSize.Height;
        if (MenuGeometryHelper.FindDescendantByName<Grid>(
                templateRoot,
                "FolderVisualRoot") is not Grid folderVisual)
        {
            return;
        }

        folderVisual.Width = 48d;
        folderVisual.Height = 48d;
        folderVisual.Margin = new Thickness(0d, 13d, 0d, 0d);
        folderVisual.HorizontalAlignment = HorizontalAlignment.Center;
        folderVisual.VerticalAlignment = VerticalAlignment.Top;
        ApplySurface(folderVisual, icon.FolderSize);
        ApplyPreview(folderVisual, icon);
    }

    public static double GetPreviewIconSize(ShortcutFolderSize size) => 14d;

    public static double GetBodyHeight(double slotHeight, int rowSpan) =>
        GetBodyHeight(slotHeight, rowSpan, topInset: 0d);

    private static double GetBodyHeight(double slotHeight, int rowSpan, double topInset)
    {
        double bodyHeight = ShortcutLayoutCalculator.GetFolderBodyHeight(
            slotHeight,
            rowSpan,
            LabelBlockHeight,
            StackSpacing);
        return rowSpan > 1 ? Math.Max(64d, bodyHeight - topInset) : bodyHeight;
    }

    private static double GetShortcutIconStackTopInset(double slotHeight)
    {
        double regularStackHeight = ShortcutIconSize + StackSpacing + LabelBlockHeight;
        return Math.Max(0d, (slotHeight - regularStackHeight) / 2d);
    }

    private static double GetBodyWidth(double tileWidth, int columnSpan) =>
        columnSpan >= 2 ? Math.Max(64d, tileWidth - 24d) : 64d;

    private static void ApplySurface(DependencyObject folderVisual, ShortcutFolderSize size)
    {
        if (MenuGeometryHelper.FindDescendantByName<Border>(
                folderVisual,
                "FolderVisualSurface") is Border surface)
        {
            surface.CornerRadius = new CornerRadius(12d);
        }
    }

    private static void ApplyPreview(DependencyObject folderVisual, IconInfo icon)
    {
        if (MenuGeometryHelper.FindDescendantByName<Grid>(
                folderVisual,
                "FolderPreviewItems") is not Grid previewItems)
        {
            return;
        }

        double iconSize = GetPreviewIconSize(icon.FolderSize);
        int itemCount = Math.Clamp(icon.FolderPreviewItems_NoWrite.Count, 1, 2);
        int columnCount = itemCount <= 2 ? itemCount : 2;
        int rowCount = (int)Math.Ceiling(itemCount / (double)columnCount);
        double previewWidth = PreviewCellSize * columnCount
            + PreviewIconGap * Math.Max(0, columnCount - 1);
        double previewHeight = PreviewCellSize * rowCount
            + PreviewIconGap * Math.Max(0, rowCount - 1);

        previewItems.Margin = new Thickness(0d);
        previewItems.Width = previewWidth;
        previewItems.Height = previewHeight;
        previewItems.HorizontalAlignment = HorizontalAlignment.Center;
        previewItems.VerticalAlignment = VerticalAlignment.Center;
        previewItems.ColumnSpacing = columnCount > 1 ? PreviewIconGap : 0d;
        previewItems.RowSpacing = rowCount > 1 ? PreviewIconGap : 0d;
        SetGridTracks(previewItems, columnCount, rowCount);

        for (int i = 0; i < 6; i++)
        {
            if (MenuGeometryHelper.FindDescendantByName<Border>(
                    previewItems,
                    $"FolderPreviewCell{i + 1}") is Border cell)
            {
                cell.Visibility = i < itemCount ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        ForEachDescendant<Image>(previewItems, image =>
        {
            image.Width = iconSize;
            image.Height = iconSize;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Center;
        });
    }

    private static void SetGridTracks(Grid grid, int columnCount, int rowCount)
    {
        for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
        {
            grid.ColumnDefinitions[i].Width = i < columnCount
                ? new GridLength(1d, GridUnitType.Star)
                : new GridLength(0d);
        }
        for (int i = 0; i < grid.RowDefinitions.Count; i++)
        {
            grid.RowDefinitions[i].Height = i < rowCount
                ? new GridLength(1d, GridUnitType.Star)
                : new GridLength(0d);
        }
    }

    private static void ForEachDescendant<T>(DependencyObject root, Action<T> action)
        where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                action(typed);
            }
            ForEachDescendant(child, action);
        }
    }
}
