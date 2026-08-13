using raindesk.Features.Launcher.Contracts;

namespace raindesk.Features.Launcher.Application;

public sealed record LauncherFolderDropCommandRequest(
    string? DraggedShortcutId,
    LauncherShortcutDropTraits DraggedTraits,
    string? CurrentFolderId,
    string? TargetShortcutId,
    LauncherShortcutDropTraits? TargetTraits,
    bool DraggedBelongsToCurrentFolder,
    int ReorderIndex);

public sealed record LauncherFolderDropDecision(
    LauncherMutationCommand? Command,
    string? RejectionReason = null)
{
    public bool CanExecute => Command != null;

    public static LauncherFolderDropDecision Rejected(string reason) => new(null, reason);
}

/// <summary>
/// 将文件夹浮层中的命中上下文转换为 Launcher 命令。
/// 不读取 UI、Store 或可变集合。
/// </summary>
public static class LauncherFolderDropCommandResolver
{
    public static LauncherFolderDropDecision Resolve(LauncherFolderDropCommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.DraggedShortcutId)
            || string.IsNullOrWhiteSpace(request.CurrentFolderId))
        {
            return LauncherFolderDropDecision.Rejected("拖动项和当前文件夹必须具有稳定 ID。");
        }

        bool hasTarget = !string.IsNullOrWhiteSpace(request.TargetShortcutId)
            && request.TargetTraits.HasValue;
        if (hasTarget)
        {
            LauncherShortcutDropTraits target = request.TargetTraits!.Value;
            bool sameTarget = string.Equals(
                request.DraggedShortcutId,
                request.TargetShortcutId,
                StringComparison.Ordinal);
            if (LauncherFolderDropPolicy.CanMoveIntoFolder(
                    request.DraggedTraits,
                    target,
                    sameTarget))
            {
                return new LauncherFolderDropDecision(new LauncherMutationCommand(
                    LauncherMutationKind.MoveIntoFolder,
                    DraggedShortcutId: request.DraggedShortcutId,
                    TargetShortcutId: request.TargetShortcutId));
            }

            if (request.DraggedBelongsToCurrentFolder
                && LauncherFolderDropPolicy.CanCreateFolder(
                    request.DraggedTraits,
                    target,
                    sameTarget))
            {
                return new LauncherFolderDropDecision(new LauncherMutationCommand(
                    LauncherMutationKind.CreateFolder,
                    DraggedShortcutId: request.DraggedShortcutId,
                    TargetShortcutId: request.TargetShortcutId));
            }
        }

        if (request.DraggedBelongsToCurrentFolder)
        {
            return new LauncherFolderDropDecision(new LauncherMutationCommand(
                LauncherMutationKind.Reorder,
                DraggedShortcutId: request.DraggedShortcutId,
                InsertIndex: Math.Max(0, request.ReorderIndex)));
        }

        var currentFolderTraits = new LauncherShortcutDropTraits(
            IsFolder: true,
            IsAddShortcut: false,
            IsDragPlaceholder: false);
        if (!LauncherFolderDropPolicy.CanMoveIntoFolder(
                request.DraggedTraits,
                currentFolderTraits,
                isSameItem: false))
        {
            return LauncherFolderDropDecision.Rejected("该项目不能移动到当前文件夹。");
        }

        return new LauncherFolderDropDecision(new LauncherMutationCommand(
            LauncherMutationKind.MoveIntoFolder,
            DraggedShortcutId: request.DraggedShortcutId,
            TargetShortcutId: request.CurrentFolderId));
    }
}
