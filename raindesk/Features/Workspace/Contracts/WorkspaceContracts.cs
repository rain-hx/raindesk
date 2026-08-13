namespace raindesk.Features.Workspace.Contracts;

public sealed record WorkspaceGroupSummary(
    string Id,
    string Name,
    int Order,
    int ShortcutCount,
    bool IsEncrypted);

public sealed record WorkspaceCommandResult(
    bool Success,
    bool Changed,
    string? EntityId = null,
    string? Error = null)
{
    public static WorkspaceCommandResult Failed(string error) =>
        new(Success: false, Changed: false, Error: error);
}

public enum WorkspaceGroupKind
{
    Normal,
    LinkedFolder
}

public sealed record CreateWorkspaceGroupCommand(
    string Name,
    WorkspaceGroupKind Kind = WorkspaceGroupKind.Normal,
    string? LinkPath = null);

public sealed record RenameWorkspaceGroupCommand(string GroupId, string Name);

public sealed record DeleteWorkspaceGroupCommand(string GroupId);

public sealed record ReorderWorkspaceGroupsCommand(IReadOnlyList<string> OrderedGroupIds);

public sealed record SelectWorkspaceGroupCommand(int GroupIndex);

public sealed record SelectAllGroupsCommand;

public sealed record SetGroupEncryptionCommand(string GroupId, bool IsEncrypted);

public interface IWorkspaceQueryHandler
{
    IReadOnlyList<WorkspaceGroupSummary> GetGroups();
}

public interface IWorkspaceCommandHandler
{
    WorkspaceCommandResult Create(CreateWorkspaceGroupCommand command);

    WorkspaceCommandResult Rename(RenameWorkspaceGroupCommand command);

    WorkspaceCommandResult Delete(DeleteWorkspaceGroupCommand command);

    WorkspaceCommandResult Reorder(ReorderWorkspaceGroupsCommand command);

    WorkspaceCommandResult Select(SelectWorkspaceGroupCommand command);

    WorkspaceCommandResult SelectAll(SelectAllGroupsCommand command);

    WorkspaceCommandResult SetEncryption(SetGroupEncryptionCommand command);
}
