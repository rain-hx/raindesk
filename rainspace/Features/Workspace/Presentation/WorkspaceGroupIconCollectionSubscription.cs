using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using rainspace.ViewModel;

namespace rainspace.Features.Workspace.Presentation;

/// <summary>
/// Owns subscriptions to workspace groups and their replaceable icon collections.
/// </summary>
public sealed class WorkspaceGroupIconCollectionSubscription : IDisposable
{
    private readonly Dictionary<MenuInfo, ObservableCollection<IconInfo>> _tracked = new();

    public event Action<MenuInfo>? IconListReplaced;

    public event NotifyCollectionChangedEventHandler? IconsChanged;

    public int TrackedGroupCount => _tracked.Count;

    public void Synchronize(IEnumerable<MenuInfo> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var active = new HashSet<MenuInfo>(groups.Where(group => group != null));
        foreach (MenuInfo trackedGroup in _tracked.Keys.ToArray())
        {
            if (!active.Contains(trackedGroup))
            {
                Detach(trackedGroup);
            }
        }

        foreach (MenuInfo group in active)
        {
            Attach(group);
        }
    }

    public void DetachAll()
    {
        foreach (MenuInfo group in _tracked.Keys.ToArray())
        {
            Detach(group);
        }
    }

    public void Dispose() => DetachAll();

    private void Attach(MenuInfo group)
    {
        group.PropertyChanged -= OnGroupPropertyChanged;
        group.PropertyChanged += OnGroupPropertyChanged;

        ObservableCollection<IconInfo> icons = group.IconList;
        if (_tracked.TryGetValue(group, out ObservableCollection<IconInfo>? previous)
            && ReferenceEquals(previous, icons))
        {
            return;
        }

        if (previous != null)
        {
            previous.CollectionChanged -= OnIconsChanged;
        }

        _tracked[group] = icons;
        icons.CollectionChanged -= OnIconsChanged;
        icons.CollectionChanged += OnIconsChanged;
    }

    private void Detach(MenuInfo group)
    {
        group.PropertyChanged -= OnGroupPropertyChanged;
        if (_tracked.Remove(group, out ObservableCollection<IconInfo>? icons))
        {
            icons.CollectionChanged -= OnIconsChanged;
        }
    }

    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MenuInfo group || e.PropertyName != nameof(MenuInfo.IconList))
        {
            return;
        }

        Attach(group);
        IconListReplaced?.Invoke(group);
    }

    private void OnIconsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        IconsChanged?.Invoke(sender, e);
    }
}
