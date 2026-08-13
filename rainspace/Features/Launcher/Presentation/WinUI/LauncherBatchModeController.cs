using Microsoft.UI.Xaml.Controls;

namespace rainspace.Features.Launcher.Presentation.WinUI;

public sealed class LauncherBatchModeController<T> : IDisposable
{
    private readonly LauncherBatchModeState _state;
    private readonly ListView _listView;
    private readonly Func<IEnumerable<T>> _items;
    private readonly Predicate<T> _excludeFromClear;
    private readonly Action<T> _clearChecked;
    private readonly Action _modeApplied;
    private bool _attached;

    public LauncherBatchModeController(
        LauncherBatchModeState state,
        ListView listView,
        Func<IEnumerable<T>> items,
        Predicate<T> excludeFromClear,
        Action<T> clearChecked,
        Action modeApplied)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _excludeFromClear = excludeFromClear ?? throw new ArgumentNullException(nameof(excludeFromClear));
        _clearChecked = clearChecked ?? throw new ArgumentNullException(nameof(clearChecked));
        _modeApplied = modeApplied ?? throw new ArgumentNullException(nameof(modeApplied));
    }

    public void Attach()
    {
        if (!_attached)
        {
            _state.Changed += State_Changed;
            _attached = true;
        }
        Apply();
    }

    public void Detach()
    {
        if (!_attached)
        {
            return;
        }
        _state.Changed -= State_Changed;
        _attached = false;
    }

    public void Toggle()
    {
        if (!_state.IsEnabled)
        {
            foreach (T item in _items())
            {
                if (!_excludeFromClear(item))
                {
                    _clearChecked(item);
                }
            }
        }
        _state.Toggle();
    }

    private void State_Changed(object? sender, EventArgs e)
    {
        if (!_listView.DispatcherQueue.HasThreadAccess)
        {
            _listView.DispatcherQueue.TryEnqueue(Apply);
            return;
        }
        Apply();
    }

    private void Apply()
    {
        _listView.SelectionMode = _state.IsEnabled
            ? ListViewSelectionMode.Multiple
            : ListViewSelectionMode.Extended;
        _modeApplied();
    }

    public void Dispose() => Detach();
}
