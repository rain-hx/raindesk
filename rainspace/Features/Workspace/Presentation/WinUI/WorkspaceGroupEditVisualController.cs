using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using rainspace.Util;

namespace rainspace.Features.Workspace.Presentation.WinUI;

internal sealed class WorkspaceGroupEditVisualController
{
    public bool IsInsideEditor<T>(DependencyObject? source, T target)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(target);
        while (source != null)
        {
            if (source is TextBox box && ReferenceEquals(box.Tag, target))
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    public bool TryGetEditor<T>(
        ListView? listView,
        T target,
        out ListViewItem container,
        out TextBox editor)
        where T : class
    {
        editor = null!;
        if (!WorkspaceGroupTabLayoutAdapter.TryGetContainer(listView, target, out container))
        {
            return false;
        }

        editor = FindFirstChild<TextBox>(container)!;
        return editor != null;
    }

    public void PrepareEditor(
        ListViewItem container,
        TextBox editor,
        string originalName,
        bool initializeText,
        WorkspaceGroupTabWidthMetrics widthMetrics,
        DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        if (initializeText)
        {
            editor.Text = originalName;
            int textLength = editor.Text?.Length ?? 0;
            editor.SelectionStart = textLength;
            editor.SelectionLength = 0;
        }

        WorkspaceGroupTabSizingAdapter.ApplyEditWidth(container, widthMetrics);
        if (BlinkingCaretOverlay.TryFindHostElements(editor, out TextBlock mirror, out Border blinker))
        {
            BlinkingCaretOverlay.Attach(editor, mirror, blinker, dispatcherQueue);
            BlinkingCaretOverlay.PrepareEditAppearance(editor);
        }

        editor.Focus(FocusState.Programmatic);
    }

    public void DetachEditor<T>(ListView? listView, T target)
        where T : class
    {
        if (TryGetEditor(listView, target, out _, out TextBox editor))
        {
            BlinkingCaretOverlay.Detach(editor);
        }
    }

    private static T? FindFirstChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            T? nested = FindFirstChild<T>(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
