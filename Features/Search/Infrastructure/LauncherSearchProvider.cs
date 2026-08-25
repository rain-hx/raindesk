using raindesk.Features.Search.Contracts;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;

namespace raindesk.Features.Search.Infrastructure;

/// <summary>将 Launcher 当前快捷方式投影为 Search 模块的本地提供者。</summary>
public sealed class LauncherSearchProvider : ISearchProvider
{
    public const string ProviderId = "launcher";
    private readonly IAppDataProvider _dataProvider;

    public LauncherSearchProvider(IAppDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public string Id => ProviderId;

    public bool IsExternal => false;

    public ValueTask<IReadOnlyList<SearchResult>> SearchAsync(
        string keyword,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyword) || maxResults <= 0)
        {
            return ValueTask.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
        }

        string term = keyword.Trim();
        IReadOnlyList<SearchResult> results = _dataProvider.Read(data => data.MenuList
            .SelectMany(menu => EnumerateSearchableIcons(menu.IconList))
            .Concat(EnumerateSearchableIcons(data.AppConfig.AllGroupsIconList))
            .Distinct()
            .Where(icon => !icon.IsAddShortcut_NoWrite && Matches(icon, term))
            .Take(maxResults)
            .Select(icon => IconSearchResultMapper.FromIcon(icon, ProviderId))
            .ToList());

        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(results);
    }

    private static bool Matches(IconInfo icon, string term)
    {
        return (!string.IsNullOrEmpty(icon.Name)
                && icon.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(icon.Path)
                && icon.Path.Contains(term, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(icon.StartArg)
                && icon.StartArg.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<IconInfo> EnumerateSearchableIcons(IEnumerable<IconInfo> icons)
    {
        foreach (IconInfo icon in icons)
        {
            yield return icon;
            if (!icon.IsFolder)
            {
                continue;
            }

            foreach (IconInfo child in EnumerateSearchableIcons(icon.FolderItems))
            {
                yield return child;
            }
        }
    }
}
