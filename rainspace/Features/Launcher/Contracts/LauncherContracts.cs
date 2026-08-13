namespace rainspace.Features.Launcher.Contracts;

public enum LauncherMutationKind
{
    CreateFolder,
    MoveIntoFolder,
    MoveToGroup,
    Reorder,
    RemoveShortcut,
    ResizeFolder,
    MoveOutOfFolder
}

public enum LauncherFolderSize
{
    Small,
    Medium,
    Large
}

public sealed record LauncherMutationCommand(
    LauncherMutationKind Kind,
    string? DraggedShortcutId = null,
    string? TargetShortcutId = null,
    IReadOnlyList<string>? ShortcutIds = null,
    string? TargetGroupId = null,
    int InsertIndex = -1,
    string? SubjectShortcutId = null,
    LauncherFolderSize? FolderSize = null);

public enum LauncherContextMenuAction
{
    OpenFolder,
    ResizeFolder,
    RunAsAdministrator,
    ShowInFileExplorer,
    EditProperties,
    Rename,
    Remove
}

public sealed record LauncherContextMenuRequest(
    string? ShortcutId,
    bool IsFolder,
    bool IsPlaceholder,
    bool HasFileLocation);

public sealed class LauncherContextMenuPlan
{
    private readonly HashSet<LauncherContextMenuAction> _visibleActions;

    public LauncherContextMenuPlan(IEnumerable<LauncherContextMenuAction> visibleActions)
    {
        ArgumentNullException.ThrowIfNull(visibleActions);
        _visibleActions = new HashSet<LauncherContextMenuAction>(visibleActions);
    }

    public bool IsAvailable => _visibleActions.Count > 0;

    public bool IsVisible(LauncherContextMenuAction action) => _visibleActions.Contains(action);
}

public sealed record LauncherShortcutDraft(
    string Name,
    string? Path = null,
    string? StartArgument = null,
    string? LinkPath = null,
    byte[]? ImageBytes = null,
    LauncherShortcutKind Kind = LauncherShortcutKind.Application);

public enum LauncherShortcutKind
{
    Application,
    Url
}

public sealed record RenameLauncherShortcutCommand(string ShortcutId, string Name);

public sealed record LauncherShortcutPropertiesUpdate(
    string Name,
    string? Path,
    string? StartArgument,
    bool AdminStartUp,
    byte[]? ImageBytes = null,
    bool ReplaceImage = false);

public sealed record UpdateLauncherShortcutCommand(
    string ShortcutId,
    LauncherShortcutPropertiesUpdate Properties);

public sealed record AddLauncherShortcutCommand(
    string TargetGroupId,
    LauncherShortcutDraft Shortcut,
    int InsertIndex = -1);

public sealed record LauncherCommandResult(bool Success, bool Changed, string? Error = null)
{
    public static LauncherCommandResult Failed(string error) =>
        new(Success: false, Changed: false, Error: error);
}

public interface ILauncherCommandHandler
{
    LauncherCommandResult Execute(LauncherMutationCommand command);

    LauncherCommandResult Add(AddLauncherShortcutCommand command);

    LauncherCommandResult AddMany(AddLauncherShortcutsCommand command);

    LauncherCommandResult Rename(RenameLauncherShortcutCommand command);

    LauncherCommandResult Update(UpdateLauncherShortcutCommand command);

    LauncherCommandResult UpdateGridLayout(UpdateLauncherGridLayoutCommand command);

    LauncherCommandResult UpdateOrder(UpdateLauncherShortcutOrderCommand command);

    LauncherCommandResult MutateAllGroupsFolder(
        LauncherAllGroupsFolderMutationCommand command);
}

public sealed record LauncherPluginCommandDescriptor(
    string PluginId,
    string ContributionId,
    string DisplayName);

public sealed record LauncherPluginCommandContext(
    IReadOnlyDictionary<string, string?> Arguments);

public enum LauncherPluginCommandErrorCode
{
    None,
    InvalidRequest,
    NotFound,
    Disabled,
    Cancelled,
    Timeout,
    ExecutionFailed
}

public sealed record LauncherPluginCommandResult(
    bool Success,
    LauncherPluginCommandErrorCode ErrorCode = LauncherPluginCommandErrorCode.None,
    string? Error = null)
{
    public static LauncherPluginCommandResult Succeeded { get; } = new(true);

    public static LauncherPluginCommandResult Failed(
        LauncherPluginCommandErrorCode code,
        string error) => new(false, code, error);
}

/// <summary>由 composition root 显式注册的 Launcher 插件命令，不执行程序集扫描。</summary>
public interface ILauncherPluginCommandContribution
{
    LauncherPluginCommandDescriptor Descriptor { get; }

    Task<LauncherPluginCommandResult> ExecuteAsync(
        LauncherPluginCommandContext context,
        CancellationToken cancellationToken = default);
}

public interface ILauncherPluginCommandRouter
{
    IReadOnlyList<LauncherPluginCommandDescriptor> GetAvailableCommands();

    Task<LauncherPluginCommandResult> ExecuteAsync(
        string pluginId,
        string contributionId,
        LauncherPluginCommandContext context,
        CancellationToken cancellationToken = default);
}

public enum LauncherActivationMode
{
    Default,
    Administrator,
    ShowInFileExplorer
}

public sealed record LauncherActivationCommand(
    string ShortcutId,
    LauncherActivationMode Mode = LauncherActivationMode.Default);

public enum LauncherActivationErrorCode
{
    None,
    InvalidRequest,
    ShortcutNotFound,
    NotLaunchable,
    ActivationFailed
}

public sealed record LauncherActivationResult(
    bool Success,
    bool Dispatched,
    bool Suppressed = false,
    LauncherActivationErrorCode ErrorCode = LauncherActivationErrorCode.None,
    string? Error = null)
{
    public static LauncherActivationResult Activated { get; } =
        new(true, Dispatched: true);

    public static LauncherActivationResult Debounced { get; } =
        new(true, Dispatched: false, Suppressed: true);

    public static LauncherActivationResult Failed(
        LauncherActivationErrorCode code,
        string error) => new(false, false, false, code, error);
}

/// <summary>Launcher 激活用例；调用方只提交稳定快捷方式 ID 与激活意图。</summary>
public interface ILauncherActivationService
{
    LauncherActivationResult Execute(LauncherActivationCommand command);
}

/// <summary>由 Infrastructure 实现的操作系统激活 Port。</summary>
public interface ILauncherActivationPort
{
    LauncherActivationResult Activate(LauncherActivationCommand command);
}
