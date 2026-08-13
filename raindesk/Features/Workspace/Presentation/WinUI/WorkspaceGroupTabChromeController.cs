using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Resets ListView selection chrome so the dedicated workspace indicator remains authoritative.
/// </summary>
public sealed class WorkspaceGroupTabChromeController
{
    private static readonly SolidColorBrush TransparentBrush =
        new(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));

    public void Apply(ListViewItem? item)
    {
        if (item == null)
        {
            return;
        }

        item.Background = TransparentBrush;
        item.BorderBrush = TransparentBrush;
        item.BorderThickness = new Thickness(0);
    }

    public void ApplyAll(ListViewBase? listView, int itemCount)
    {
        if (listView == null || itemCount <= 0)
        {
            return;
        }

        for (int index = 0; index < itemCount; index++)
        {
            Apply(listView.ContainerFromIndex(index) as ListViewItem);
        }
    }
}
