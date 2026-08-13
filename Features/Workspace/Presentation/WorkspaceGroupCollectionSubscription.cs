using System.Collections.Specialized;

namespace raindesk.Features.Workspace.Presentation;

/// <summary>
/// Owns the lifetime of the workspace group collection subscription.
/// Replacing application state therefore cannot leave the old collection subscribed.
/// </summary>
public sealed class WorkspaceGroupCollectionSubscription : IDisposable
{
    private INotifyCollectionChanged? _source;
    private NotifyCollectionChangedEventHandler? _handler;

    public bool IsAttached => _source != null;

    public void Attach(
        INotifyCollectionChanged source,
        NotifyCollectionChangedEventHandler handler)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(handler);

        if (ReferenceEquals(_source, source) && _handler == handler)
        {
            return;
        }

        Detach();
        _source = source;
        _handler = handler;
        _source.CollectionChanged += _handler;
    }

    public void Detach()
    {
        if (_source != null && _handler != null)
        {
            _source.CollectionChanged -= _handler;
        }

        _source = null;
        _handler = null;
    }

    public void Dispose() => Detach();
}
