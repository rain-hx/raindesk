namespace raindesk.Features.Search.Contracts;

/// <summary>只读搜索查询。轻量 CQRS 中的 Query，不产生状态变更。</summary>
public sealed record SearchQuery(
    string Keyword,
    bool IncludeExternalProviders,
    int MaxResults = 80);

public enum SearchResultKind
{
    Shortcut,
    Url,
    Folder
}

/// <summary>
/// 与 WinUI、Launcher 模型和具体搜索 SDK 无关的搜索结果契约。
/// </summary>
public sealed record SearchResult(
    string Id,
    string ProviderId,
    string Name,
    string Path,
    string? StartArgument,
    bool RunAsAdministrator,
    SearchResultKind Kind,
    ReadOnlyMemory<byte> IconBytes);

/// <summary>搜索提供者扩展点；Everything 和未来插件均实现此 Port。</summary>
public interface ISearchProvider
{
    string Id { get; }

    bool IsExternal { get; }

    ValueTask<IReadOnlyList<SearchResult>> SearchAsync(
        string keyword,
        int maxResults,
        CancellationToken cancellationToken);
}

public interface ISearchQueryHandler
{
    ValueTask<IReadOnlyList<SearchResult>> HandleAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);
}
