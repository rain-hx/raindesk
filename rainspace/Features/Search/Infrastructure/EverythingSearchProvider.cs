using rainspace.Features.Search.Contracts;
using rainspace.Features.Plugins.Contracts;
using rainspace.Plugins.EveryThing;
using rainspace.ViewModel;

namespace rainspace.Features.Search.Infrastructure;

/// <summary>Everything SDK 的 Search Port 适配器。</summary>
public sealed class EverythingSearchProvider : ISearchProvider
{
    public const string ProviderId = "everything";
    public const string PluginId = "rainspace.everything";
    private readonly SemaphoreSlim _queryLock = new(1, 1);
    private readonly IPluginContributionGate _pluginGate;

    public EverythingSearchProvider(IPluginContributionGate pluginGate)
    {
        _pluginGate = pluginGate ?? throw new ArgumentNullException(nameof(pluginGate));
    }

    public string Id => ProviderId;

    public bool IsExternal => true;

    public async ValueTask<IReadOnlyList<SearchResult>> SearchAsync(
        string keyword,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyword)
            || maxResults <= 0
            || !_pluginGate.IsEnabled(
                PluginId,
                ProviderId,
                PluginContributionKind.SearchProvider))
        {
            return Array.Empty<SearchResult>();
        }

        await _queryLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await global::System.Threading.Tasks.Task.Run<IReadOnlyList<SearchResult>>(
                () => EveryThingUtil.SearchIcons(keyword, maxResults)
                    .Select((IconInfo icon) => IconSearchResultMapper.FromIcon(icon, ProviderId))
                    .ToList(),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _queryLock.Release();
        }
    }
}
