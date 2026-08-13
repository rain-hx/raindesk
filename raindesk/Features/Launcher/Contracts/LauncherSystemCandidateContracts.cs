namespace raindesk.Features.Launcher.Contracts;

public sealed record LauncherSystemCandidate(
    string Name,
    string? Path = null,
    string? StartArgument = null,
    string? LinkPath = null,
    string? Content = null,
    byte[]? ImageBytes = null);

/// <summary>发现可添加到 Launcher 的 Windows 系统项与开始菜单项。</summary>
public interface ILauncherSystemCandidateSource
{
    global::System.Threading.Tasks.Task<IReadOnlyList<LauncherSystemCandidate>> GetBuiltInSystemItemsAsync(
        CancellationToken cancellationToken = default);

    global::System.Threading.Tasks.Task<IReadOnlyList<LauncherSystemCandidate>> GetStartMenuItemsAsync(
        CancellationToken cancellationToken = default);
}
