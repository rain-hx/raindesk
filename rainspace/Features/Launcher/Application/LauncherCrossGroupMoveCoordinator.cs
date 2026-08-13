using rainspace.Features.Launcher.Contracts;

namespace rainspace.Features.Launcher.Application;

public enum LauncherCrossGroupMoveViewAction
{
    None,
    RestoreCapturedAllGroupsOrder
}

public sealed record LauncherCrossGroupMoveRequest(
    IReadOnlyList<string> ShortcutIds,
    string TargetGroupId,
    int TargetItemCount,
    int? PreviewInsertIndex,
    bool PreserveAllGroupsOrder);

public sealed record LauncherCrossGroupMoveResult(
    bool Committed,
    int InsertIndex,
    LauncherCrossGroupMoveViewAction ViewAction,
    string? Error = null);

/// <summary>
/// 跨分组移动的应用层协调器：校验稳定 ID、归一化插入位置、执行命令，
/// 并明确告知 Presentation 是否需要恢复进入预览前的“全部”视图顺序。
/// </summary>
public sealed class LauncherCrossGroupMoveCoordinator
{
    private readonly ILauncherCommandHandler _commandHandler;

    public LauncherCrossGroupMoveCoordinator(ILauncherCommandHandler commandHandler)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    }

    public LauncherCrossGroupMoveResult Execute(LauncherCrossGroupMoveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ShortcutIds);

        string[] shortcutIds = request.ShortcutIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (shortcutIds.Length == 0 || string.IsNullOrWhiteSpace(request.TargetGroupId))
        {
            return new LauncherCrossGroupMoveResult(
                false,
                0,
                LauncherCrossGroupMoveViewAction.None,
                "跨分组移动需要有效的快捷方式 ID 和目标分组 ID。");
        }

        int targetCount = Math.Max(0, request.TargetItemCount);
        int insertIndex = request.PreviewInsertIndex.HasValue
            ? Math.Clamp(request.PreviewInsertIndex.Value, 0, targetCount)
            : targetCount;
        LauncherCommandResult commandResult = _commandHandler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.MoveToGroup,
            ShortcutIds: shortcutIds,
            TargetGroupId: request.TargetGroupId,
            InsertIndex: insertIndex));

        if (!commandResult.Success || !commandResult.Changed)
        {
            return new LauncherCrossGroupMoveResult(
                false,
                insertIndex,
                LauncherCrossGroupMoveViewAction.None,
                commandResult.Error);
        }

        LauncherCrossGroupMoveViewAction viewAction = request.PreserveAllGroupsOrder
            ? LauncherCrossGroupMoveViewAction.RestoreCapturedAllGroupsOrder
            : LauncherCrossGroupMoveViewAction.None;
        return new LauncherCrossGroupMoveResult(true, insertIndex, viewAction);
    }
}
