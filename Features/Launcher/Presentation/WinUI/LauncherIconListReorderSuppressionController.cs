using Microsoft.UI.Xaml.Controls;

namespace raindesk.Features.Launcher.Presentation.WinUI;

internal enum LauncherIconListReorderSuppressionReason
{
    CrossGroupPreview,
    BatchDrag
}

/// <summary>
/// Applies ListView reorder suppression while one or more independent drag reasons are active.
/// </summary>
internal sealed class LauncherIconListReorderSuppressionController
{
    private readonly LauncherReorderSuppressionState<LauncherIconListReorderSuppressionReason>
        _state = new();

    public bool IsSuppressed => _state.IsSuppressed;

    public bool Suppress(
        ListView? listView,
        LauncherIconListReorderSuppressionReason reason)
    {
        if (listView == null)
        {
            return false;
        }

        bool changed = _state.Suppress(reason);
        listView.CanReorderItems = false;
        listView.ReorderMode = ListViewReorderMode.Disabled;
        return changed;
    }

    public bool Restore(
        ListView? listView,
        LauncherIconListReorderSuppressionReason reason)
    {
        if (listView == null)
        {
            return false;
        }

        bool changed = _state.Restore(reason);
        if (!_state.IsSuppressed)
        {
            listView.CanReorderItems = false;
            listView.ReorderMode = ListViewReorderMode.Disabled;
        }

        return changed;
    }

    public void Clear(ListView? listView)
    {
        _state.Clear();
        if (listView != null)
        {
            listView.CanReorderItems = false;
            listView.ReorderMode = ListViewReorderMode.Disabled;
        }
    }
}
