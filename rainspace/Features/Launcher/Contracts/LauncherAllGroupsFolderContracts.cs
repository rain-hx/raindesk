namespace rainspace.Features.Launcher.Contracts;

public enum LauncherAllGroupsFolderMutationKind
{
    Create,
    MoveInto,
    MoveOut
}

/// <summary>
/// Mutates an All Groups-owned folder using stable IDs and the caller's complete
/// visible flat-order snapshot. Infrastructure validates the whole snapshot before
/// changing either the owned-folder container or its persisted projection.
/// </summary>
public sealed record LauncherAllGroupsFolderMutationCommand(
    LauncherAllGroupsFolderMutationKind Kind,
    string SubjectShortcutId,
    string TargetShortcutId,
    IReadOnlyList<string> FlatOrderShortcutIds,
    int InsertIndex = -1);
