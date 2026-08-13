using raindesk.Features.Search.Contracts;

namespace raindesk.Features.Search.Application;

/// <summary>聚合所有已注册搜索提供者，负责去重、排序和结果上限。</summary>
public sealed class SearchQueryHandler : ISearchQueryHandler
{
    private readonly IReadOnlyList<ISearchProvider> _providers;

    public SearchQueryHandler(IEnumerable<ISearchProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToList();
    }

    public async ValueTask<IReadOnlyList<SearchResult>> HandleAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.Keyword) || query.MaxResults <= 0)
        {
            return Array.Empty<SearchResult>();
        }

        string keyword = query.Keyword.Trim();
        var results = new List<SearchResult>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ISearchProvider provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (provider.IsExternal && !query.IncludeExternalProviders)
            {
                continue;
            }

            IReadOnlyList<SearchResult> providerResults;
            try
            {
                providerResults = await provider
                    .SearchAsync(keyword, query.MaxResults, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // 一个可选提供者失败不应让 Launcher 本地搜索不可用。
                continue;
            }

            foreach (SearchResult result in providerResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(result.Path) && !seenPaths.Add(result.Path))
                {
                    continue;
                }

                results.Add(result);
            }
        }

        return results
            .OrderBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
            .Take(query.MaxResults)
            .ToList();
    }
}
