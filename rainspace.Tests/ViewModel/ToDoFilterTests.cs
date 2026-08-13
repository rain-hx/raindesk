using rainspace.ViewModel;

namespace rainspace.Tests.ViewModel;

[TestClass]
public class ToDoFilterTests
{
    [TestMethod]
    public void SettingSameStatusFilter_DoesNotRaisePropertyChanged()
    {
        var filter = new ToDoFilter();
        int changeCount = 0;
        filter.PropertyChanged += (_, _) => changeCount++;

        filter.StatusFilter = filter.StatusFilter;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentSearchQuery_RaisesPropertyChangedOnce()
    {
        var filter = new ToDoFilter();
        int changeCount = 0;
        string? propertyName = null;
        filter.PropertyChanged += (_, e) =>
        {
            changeCount++;
            propertyName = e.PropertyName;
        };

        filter.SearchQuery = "release";

        Assert.AreEqual(1, changeCount);
        Assert.AreEqual(nameof(ToDoFilter.SearchQuery), propertyName);
    }

    [TestMethod]
    public void SettingWhitespaceSearchQuery_WhenEmpty_DoesNotRaisePropertyChanged()
    {
        var filter = new ToDoFilter();
        int changeCount = 0;
        filter.PropertyChanged += (_, _) => changeCount++;

        filter.SearchQuery = "   ";

        Assert.AreEqual(0, changeCount);
        Assert.IsNull(filter.SearchQuery);
    }

    [TestMethod]
    public void SettingSearchQueryWithDifferentWhitespace_DoesNotRaisePropertyChanged()
    {
        var filter = new ToDoFilter { SearchQuery = "release" };
        int changeCount = 0;
        filter.PropertyChanged += (_, _) => changeCount++;

        filter.SearchQuery = "  release  ";

        Assert.AreEqual(0, changeCount);
        Assert.AreEqual("release", filter.SearchQuery);
    }

    [TestMethod]
    public void SettingSameProjectId_DoesNotRaisePropertyChanged()
    {
        var filter = new ToDoFilter
        {
            ProjectId = "work"
        };
        int changeCount = 0;
        filter.PropertyChanged += (_, _) => changeCount++;

        filter.ProjectId = "work";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingWhitespaceProjectId_WhenEmpty_DoesNotRaisePropertyChanged()
    {
        var filter = new ToDoFilter();
        int changeCount = 0;
        filter.PropertyChanged += (_, _) => changeCount++;

        filter.ProjectId = "   ";

        Assert.AreEqual(0, changeCount);
        Assert.IsNull(filter.ProjectId);
    }

    [TestMethod]
    public void SettingProjectIdWithDifferentWhitespace_DoesNotRaisePropertyChanged()
    {
        var filter = new ToDoFilter { ProjectId = "work" };
        int changeCount = 0;
        filter.PropertyChanged += (_, _) => changeCount++;

        filter.ProjectId = "  work  ";

        Assert.AreEqual(0, changeCount);
        Assert.AreEqual("work", filter.ProjectId);
    }

    [TestMethod]
    public void Apply_TitleSortAscending_OrdersCaseInsensitive()
    {
        var filter = new ToDoFilter
        {
            StatusFilter = ToDoStatusFilter.All,
            SortBy = ToDoSortBy.Title,
            SortAscending = true
        };
        var tasks = new List<ToDoTask>
        {
            new() { Id = "lower", Title = "alpha" },
            new() { Id = "upper", Title = "Alpha" },
            new() { Id = "beta", Title = "beta" }
        };

        var result = filter.Apply(tasks);

        CollectionAssert.AreEqual(new[] { "lower", "upper", "beta" }, result.Select(t => t.Id).ToArray());
    }

    [TestMethod]
    public void Apply_TitleSortDescending_OrdersCaseInsensitive()
    {
        var filter = new ToDoFilter
        {
            StatusFilter = ToDoStatusFilter.All,
            SortBy = ToDoSortBy.Title,
            SortAscending = false
        };
        var tasks = new List<ToDoTask>
        {
            new() { Id = "lower", Title = "alpha" },
            new() { Id = "upper", Title = "Alpha" },
            new() { Id = "beta", Title = "beta" }
        };

        var result = filter.Apply(tasks);

        CollectionAssert.AreEqual(new[] { "beta", "lower", "upper" }, result.Select(t => t.Id).ToArray());
    }
}
