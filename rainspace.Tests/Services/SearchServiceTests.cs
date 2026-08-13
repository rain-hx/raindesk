using rainspace.Features.Search.Application;
using rainspace.Features.Search.Contracts;
using rainspace.Features.Search.Infrastructure;
using rainspace.Services;
using rainspace.ViewModel;
using System.Collections.ObjectModel;

namespace rainspace.Tests.Services;

[TestClass]
public class SearchQueryHandlerTests
{
    [TestMethod]
    public async global::System.Threading.Tasks.Task BlankKeyword_ReturnsEmptyWithoutCallingProviders()
    {
        var provider = new StubProvider("external", isExternal: true, _ => Array.Empty<SearchResult>());
        var handler = new SearchQueryHandler(new[] { provider });

        IReadOnlyList<SearchResult> results = await handler.HandleAsync(
            new SearchQuery("   ", IncludeExternalProviders: true));

        Assert.AreEqual(0, results.Count);
        Assert.AreEqual(0, provider.CallCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task LauncherProvider_MatchesNamePathAndStartArgumentCaseInsensitive()
    {
        var nameMatch = new IconInfo { IconId = "name", Name = "Calculator", Path = @"C:\Windows\calc.exe" };
        var pathMatch = new IconInfo { IconId = "path", Name = "Tool", Path = @"D:\Apps\CalculatorPlus.exe" };
        var argMatch = new IconInfo { IconId = "arg", Name = "Launcher", StartArg = "--open-calculator" };
        var nonMatch = new IconInfo { IconId = "other", Name = "Notes", Path = @"D:\Apps\notes.exe" };
        var addShortcut = new IconInfo { Name = "Calculator Add", IsAddShortcut_NoWrite = true };
        var provider = CreateLauncherProvider(nameMatch, pathMatch, argMatch, nonMatch, addShortcut);
        var handler = new SearchQueryHandler(new ISearchProvider[] { provider });

        IReadOnlyList<SearchResult> results = await handler.HandleAsync(
            new SearchQuery("  CALCULATOR  ", IncludeExternalProviders: false));

        CollectionAssert.AreEqual(
            new[] { "Calculator", "Launcher", "Tool" },
            results.Select(result => result.Name).ToArray());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task ExternalProvider_SkipsDuplicateLocalPathsAndSortsTogether()
    {
        var local = new StubProvider(
            "launcher",
            isExternal: false,
            _ => new[] { Result("local", "Zoo", @"D:\Apps\zoo.exe", "launcher") });
        var external = new StubProvider(
            "everything",
            isExternal: true,
            _ => new[]
            {
                Result("duplicate", "Alpha duplicate", @"D:\Apps\zoo.exe", "everything"),
                Result("external", "Alpha", @"D:\Apps\alpha.exe", "everything")
            });
        var handler = new SearchQueryHandler(new ISearchProvider[] { local, external });

        IReadOnlyList<SearchResult> results = await handler.HandleAsync(
            new SearchQuery("app", IncludeExternalProviders: true));

        CollectionAssert.AreEqual(
            new[] { "Alpha", "Zoo" },
            results.Select(result => result.Name).ToArray());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task ExternalProviders_AreNotCalledWhenQueryDisablesThem()
    {
        var external = new StubProvider(
            "everything",
            isExternal: true,
            _ => new[] { Result("external", "External", "external.exe", "everything") });
        var handler = new SearchQueryHandler(new[] { external });

        IReadOnlyList<SearchResult> results = await handler.HandleAsync(
            new SearchQuery("app", IncludeExternalProviders: false));

        Assert.AreEqual(0, results.Count);
        Assert.AreEqual(0, external.CallCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task ProviderFailure_DoesNotHideResultsFromOtherProviders()
    {
        var failing = new StubProvider("broken", isExternal: true, _ => throw new InvalidOperationException("boom"));
        var healthy = new StubProvider(
            "launcher",
            isExternal: false,
            _ => new[] { Result("healthy", "Healthy", "healthy.exe", "launcher") });
        var handler = new SearchQueryHandler(new ISearchProvider[] { failing, healthy });

        IReadOnlyList<SearchResult> results = await handler.HandleAsync(
            new SearchQuery("health", IncludeExternalProviders: true));

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Healthy", results[0].Name);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task LauncherProvider_MatchesFolderNameAndNestedItems()
    {
        var child = new IconInfo { IconId = "child", Name = "Nested App", Path = @"D:\Apps\nested.exe" };
        var nested = new IconInfo { IconId = "nested", Name = "Nested Tools", IsFolder = true };
        var folder = new IconInfo { IconId = "root", Name = "Office Folder", IsFolder = true };
        nested.FolderItems.Add(child);
        folder.FolderItems.Add(nested);
        var handler = new SearchQueryHandler(new ISearchProvider[] { CreateLauncherProvider(folder) });

        IReadOnlyList<SearchResult> folderResults = await handler.HandleAsync(
            new SearchQuery("Office", IncludeExternalProviders: false));
        IReadOnlyList<SearchResult> nestedResults = await handler.HandleAsync(
            new SearchQuery("Nested", IncludeExternalProviders: false));

        Assert.AreEqual(SearchResultKind.Folder, folderResults.Single().Kind);
        CollectionAssert.AreEqual(
            new[] { "Nested App", "Nested Tools" },
            nestedResults.Select(result => result.Name).ToArray());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task ResultLimit_IsAppliedAfterAggregation()
    {
        var provider = new StubProvider(
            "launcher",
            isExternal: false,
            _ => new[]
            {
                Result("c", "Charlie", "c.exe", "launcher"),
                Result("a", "Alpha", "a.exe", "launcher"),
                Result("b", "Bravo", "b.exe", "launcher")
            });
        var handler = new SearchQueryHandler(new[] { provider });

        IReadOnlyList<SearchResult> results = await handler.HandleAsync(
            new SearchQuery("app", IncludeExternalProviders: false, MaxResults: 2));

        CollectionAssert.AreEqual(
            new[] { "Alpha", "Bravo" },
            results.Select(result => result.Name).ToArray());
    }

    private static LauncherSearchProvider CreateLauncherProvider(params IconInfo[] icons)
    {
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                new() { IconList = new ObservableCollection<IconInfo>(icons) }
            }
        });
        return new LauncherSearchProvider(store);
    }

    private static SearchResult Result(
        string id,
        string name,
        string path,
        string providerId)
    {
        return new SearchResult(
            id,
            providerId,
            name,
            path,
            StartArgument: null,
            RunAsAdministrator: false,
            SearchResultKind.Shortcut,
            ReadOnlyMemory<byte>.Empty);
    }

    private sealed class StubProvider : ISearchProvider
    {
        private readonly Func<string, IReadOnlyList<SearchResult>> _search;

        public StubProvider(
            string id,
            bool isExternal,
            Func<string, IReadOnlyList<SearchResult>> search)
        {
            Id = id;
            IsExternal = isExternal;
            _search = search;
        }

        public string Id { get; }

        public bool IsExternal { get; }

        public int CallCount { get; private set; }

        public ValueTask<IReadOnlyList<SearchResult>> SearchAsync(
            string keyword,
            int maxResults,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_search(keyword));
        }
    }
}
