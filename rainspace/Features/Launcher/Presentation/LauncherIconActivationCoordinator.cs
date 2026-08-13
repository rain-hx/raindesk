using rainspace.Features.Launcher.Contracts;

namespace rainspace.Features.Launcher.Presentation;

internal sealed class LauncherIconActivationCoordinator<TItem>
    where TItem : class
{
    private readonly ILauncherActivationService _activationService;
    private readonly Func<TItem, string> _getShortcutId;
    private readonly Func<TItem, bool> _isAddPlaceholder;
    private readonly Func<TItem, bool> _isFolder;
    private readonly Action _showAddPanel;
    private readonly Action<TItem> _openFolder;

    public LauncherIconActivationCoordinator(
        ILauncherActivationService activationService,
        Func<TItem, string> getShortcutId,
        Func<TItem, bool> isAddPlaceholder,
        Func<TItem, bool> isFolder,
        Action showAddPanel,
        Action<TItem> openFolder)
    {
        _activationService = activationService
            ?? throw new ArgumentNullException(nameof(activationService));
        _getShortcutId = getShortcutId ?? throw new ArgumentNullException(nameof(getShortcutId));
        _isAddPlaceholder = isAddPlaceholder
            ?? throw new ArgumentNullException(nameof(isAddPlaceholder));
        _isFolder = isFolder ?? throw new ArgumentNullException(nameof(isFolder));
        _showAddPanel = showAddPanel ?? throw new ArgumentNullException(nameof(showAddPanel));
        _openFolder = openFolder ?? throw new ArgumentNullException(nameof(openFolder));
    }

    public bool HandleTap(
        TItem item,
        bool interactionEnabled,
        bool modifierDown,
        bool batchMode,
        bool doubleOpen)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!interactionEnabled)
        {
            return false;
        }
        if (modifierDown || batchMode)
        {
            return true;
        }
        if (_isAddPlaceholder(item))
        {
            _showAddPanel();
        }
        else if (_isFolder(item))
        {
            _openFolder(item);
        }
        else if (!doubleOpen)
        {
            Activate(item, LauncherActivationMode.Default);
        }
        return true;
    }

    public bool HandleDoubleTap(
        TItem item,
        bool interactionEnabled,
        bool batchMode,
        bool doubleOpen)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!interactionEnabled
            || batchMode
            || !doubleOpen
            || _isAddPlaceholder(item))
        {
            return false;
        }

        if (_isFolder(item))
        {
            _openFolder(item);
        }
        else
        {
            Activate(item, LauncherActivationMode.Default);
        }
        return true;
    }

    public LauncherActivationResult Activate(TItem item, LauncherActivationMode mode)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _activationService.Execute(new LauncherActivationCommand(
            _getShortcutId(item),
            mode));
    }
}
