namespace raindesk.Features.Launcher.Presentation;

internal enum LauncherInlineSystemItemsKind
{
    BuiltIn,
    StartMenu
}

internal readonly record struct LauncherInlineSystemItemsUpdate<TItem>(
    LauncherInlineSystemItemsKind Kind,
    IReadOnlyList<TItem> Items,
    bool IsLoading,
    string? Error = null);

/// <summary>
/// Owns candidate caches, load generations/cancellation and debounced filtering without UI types.
/// </summary>
internal sealed class LauncherInlineSystemItemsCoordinator<TItem> : IDisposable
{
    private readonly Func<CancellationToken, global::System.Threading.Tasks.Task<IReadOnlyList<TItem>>> _loadBuiltIn;
    private readonly Func<CancellationToken, global::System.Threading.Tasks.Task<IReadOnlyList<TItem>>> _loadStartMenu;
    private readonly Func<TItem, string?> _getName;
    private readonly TimeSpan _filterDelay;
    private IReadOnlyList<TItem>? _builtInItems;
    private IReadOnlyList<TItem>? _startMenuItems;
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _filterCancellation;
    private int _generation;
    private LauncherInlineSystemItemsKind _kind;
    private bool _disposed;

    public LauncherInlineSystemItemsCoordinator(
        Func<CancellationToken, global::System.Threading.Tasks.Task<IReadOnlyList<TItem>>> loadBuiltIn,
        Func<CancellationToken, global::System.Threading.Tasks.Task<IReadOnlyList<TItem>>> loadStartMenu,
        Func<TItem, string?> getName,
        TimeSpan? filterDelay = null)
    {
        _loadBuiltIn = loadBuiltIn ?? throw new ArgumentNullException(nameof(loadBuiltIn));
        _loadStartMenu = loadStartMenu ?? throw new ArgumentNullException(nameof(loadStartMenu));
        _getName = getName ?? throw new ArgumentNullException(nameof(getName));
        _filterDelay = filterDelay ?? TimeSpan.FromMilliseconds(200);
        if (_filterDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(filterDelay));
        }
    }

    public event Action<LauncherInlineSystemItemsUpdate<TItem>>? Updated;

    public async global::System.Threading.Tasks.Task SelectAsync(
        LauncherInlineSystemItemsKind kind,
        string? keyword = null)
    {
        ThrowIfDisposed();
        _kind = kind;
        int generation = ++_generation;
        CancelAndDispose(ref _loadCancellation);
        CancelAndDispose(ref _filterCancellation);
        _loadCancellation = new CancellationTokenSource();
        CancellationToken token = _loadCancellation.Token;

        IReadOnlyList<TItem>? cached = GetCache(kind);
        if (cached != null)
        {
            Publish(kind, Filter(cached, keyword), isLoading: false);
            return;
        }

        Publish(kind, Array.Empty<TItem>(), isLoading: true);
        try
        {
            IReadOnlyList<TItem> loaded = await Loader(kind)(token).ConfigureAwait(false)
                ?? Array.Empty<TItem>();
            if (token.IsCancellationRequested || generation != _generation || kind != _kind)
            {
                return;
            }

            SetCache(kind, loaded.ToList());
            Publish(kind, Filter(loaded, keyword), isLoading: false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (generation == _generation && kind == _kind)
            {
                Publish(kind, Array.Empty<TItem>(), isLoading: false, ex.Message);
            }
        }
    }

    public async global::System.Threading.Tasks.Task FilterAsync(string? keyword)
    {
        ThrowIfDisposed();
        CancelAndDispose(ref _filterCancellation);
        _filterCancellation = new CancellationTokenSource();
        CancellationToken token = _filterCancellation.Token;
        LauncherInlineSystemItemsKind kind = _kind;
        try
        {
            await global::System.Threading.Tasks.Task.Delay(_filterDelay, token).ConfigureAwait(false);
            IReadOnlyList<TItem>? items = GetCache(kind);
            if (!token.IsCancellationRequested && items != null && kind == _kind)
            {
                Publish(kind, Filter(items, keyword), isLoading: false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    public void CancelPending()
    {
        ThrowIfDisposed();
        _generation++;
        CancelAndDispose(ref _loadCancellation);
        CancelAndDispose(ref _filterCancellation);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _generation++;
        CancelAndDispose(ref _loadCancellation);
        CancelAndDispose(ref _filterCancellation);
        Updated = null;
    }

    private Func<CancellationToken, global::System.Threading.Tasks.Task<IReadOnlyList<TItem>>> Loader(
        LauncherInlineSystemItemsKind kind) =>
        kind == LauncherInlineSystemItemsKind.StartMenu ? _loadStartMenu : _loadBuiltIn;

    private IReadOnlyList<TItem>? GetCache(LauncherInlineSystemItemsKind kind) =>
        kind == LauncherInlineSystemItemsKind.StartMenu ? _startMenuItems : _builtInItems;

    private void SetCache(LauncherInlineSystemItemsKind kind, IReadOnlyList<TItem> items)
    {
        if (kind == LauncherInlineSystemItemsKind.StartMenu)
        {
            _startMenuItems = items;
        }
        else
        {
            _builtInItems = items;
        }
    }

    private IReadOnlyList<TItem> Filter(IReadOnlyList<TItem> items, string? keyword)
    {
        string normalized = keyword?.Trim() ?? string.Empty;
        return string.IsNullOrEmpty(normalized)
            ? items.ToList()
            : items.Where(item => (_getName(item) ?? string.Empty).Contains(
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private void Publish(
        LauncherInlineSystemItemsKind kind,
        IReadOnlyList<TItem> items,
        bool isLoading,
        string? error = null) =>
        Updated?.Invoke(new LauncherInlineSystemItemsUpdate<TItem>(kind, items, isLoading, error));

    private static void CancelAndDispose(ref CancellationTokenSource? source)
    {
        source?.Cancel();
        source?.Dispose();
        source = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
