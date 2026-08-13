namespace rainspace.Features.Launcher.Presentation;

/// <summary>
/// “全部分组”视图的排序及拖拽快照状态，不依赖具体快捷方式模型或 UI。
/// 所有写入都会复制集合，避免页面持有的可变列表绕过状态边界。
/// </summary>
internal sealed class LauncherAllGroupsOrderState<T>
{
    private List<T>? _currentOrder;
    private List<T>? _beforePreviewOrder;
    private List<T>? _dragStartOrder;

    public IReadOnlyList<T>? CurrentOrder => _currentOrder;

    public IReadOnlyList<T>? BeforePreviewOrder => _beforePreviewOrder;

    public IReadOnlyList<T>? DragStartOrder => _dragStartOrder;

    public IReadOnlyList<T>? CancellationOrder =>
        HasItems(_dragStartOrder) ? _dragStartOrder : _beforePreviewOrder;

    public bool HasCurrentOrder => HasItems(_currentOrder);

    public bool HasBeforePreviewOrder => HasItems(_beforePreviewOrder);

    public bool HasDragStartOrder => HasItems(_dragStartOrder);

    public void SetCurrent(IEnumerable<T>? order)
    {
        _currentOrder = order?.ToList();
    }

    public List<T> CopyCurrentOrder() =>
        _currentOrder?.ToList() ?? new List<T>();

    public void CaptureDragStart(IReadOnlyList<T> fallbackOrder)
    {
        ArgumentNullException.ThrowIfNull(fallbackOrder);
        _dragStartOrder = HasCurrentOrder
            ? _currentOrder!.ToList()
            : fallbackOrder.ToList();
        if (HasDragStartOrder)
        {
            _currentOrder = _dragStartOrder!.ToList();
        }
    }

    public IReadOnlyList<T> CaptureBeforePreview(IReadOnlyList<T> fallbackOrder)
    {
        ArgumentNullException.ThrowIfNull(fallbackOrder);
        _beforePreviewOrder = HasDragStartOrder
            ? _dragStartOrder!.ToList()
            : fallbackOrder.ToList();
        _currentOrder = _beforePreviewOrder.ToList();
        return _beforePreviewOrder;
    }

    public bool RestoreCurrentFromDragStart()
    {
        if (!HasDragStartOrder)
        {
            return false;
        }

        _currentOrder = _dragStartOrder!.ToList();
        return true;
    }

    public bool RestoreCurrentFromBestSnapshot()
    {
        IReadOnlyList<T>? snapshot = CancellationOrder;
        if (snapshot == null || snapshot.Count == 0)
        {
            return false;
        }

        _currentOrder = snapshot.ToList();
        return true;
    }

    public void ClearDragStart() => _dragStartOrder = null;

    public void ClearBeforePreview() => _beforePreviewOrder = null;

    public void ClearDragSnapshots()
    {
        _dragStartOrder = null;
        _beforePreviewOrder = null;
    }

    private static bool HasItems(IReadOnlyCollection<T>? items) =>
        items is { Count: > 0 };
}
