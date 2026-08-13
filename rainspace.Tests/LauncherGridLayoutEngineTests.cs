using rainspace.Features.Launcher.Application;

namespace rainspace.Tests;

[TestClass]
public class LauncherGridLayoutEngineTests
{
    [TestMethod]
    public void Arrange_PlacesSpannedFolderWithoutOverlappingLaterItems()
    {
        var items = new[]
        {
            new LauncherGridItem("A", 1, 1),
            new LauncherGridItem("Folder", 2, 2),
            new LauncherGridItem("B", 1, 1),
            new LauncherGridItem("C", 1, 1)
        };
        var metrics = new LauncherGridMetrics(ColumnCount: 3, SlotWidth: 100d, SlotHeight: 80d);

        var layout = LauncherGridLayoutEngine.Arrange(items, metrics);

        Assert.AreEqual(new LauncherGridCell(0, 0), layout["A"].Cell);
        Assert.AreEqual(new LauncherGridCell(1, 0), layout["Folder"].Cell);
        Assert.AreEqual(new LauncherGridCell(0, 1), layout["B"].Cell);
        Assert.AreEqual(new LauncherGridCell(0, 2), layout["C"].Cell);
        Assert.IsFalse(layout["Folder"].Bounds.IntersectsWith(layout["B"].Bounds));
        Assert.IsFalse(layout["Folder"].Bounds.IntersectsWith(layout["C"].Bounds));
    }

    [TestMethod]
    public void Arrange_UsesManualCellWhenAvailable()
    {
        var items = new[]
        {
            new LauncherGridItem("A", 1, 1),
            new LauncherGridItem("B", 1, 1, Column: 3, Row: 2),
            new LauncherGridItem("C", 1, 1),
        };

        var layout = LauncherGridLayoutEngine.Arrange(
            items,
            new LauncherGridMetrics(ColumnCount: 4, SlotWidth: 100d, SlotHeight: 80d));

        Assert.AreEqual(new LauncherGridCell(0, 0), layout["A"].Cell);
        Assert.AreEqual(new LauncherGridCell(3, 2), layout["B"].Cell);
        Assert.AreEqual(new LauncherGridCell(1, 0), layout["C"].Cell);
    }

    [TestMethod]
    public void Arrange_ManualCellReservesSpaceBeforeAutomaticItems()
    {
        var items = new[]
        {
            new LauncherGridItem("Automatic", 1, 1),
            new LauncherGridItem("Manual", 1, 1, Column: 0, Row: 0),
        };

        var layout = LauncherGridLayoutEngine.Arrange(
            items,
            new LauncherGridMetrics(ColumnCount: 3, SlotWidth: 100d, SlotHeight: 80d));

        Assert.AreEqual(new LauncherGridCell(0, 0), layout["Manual"].Cell);
        Assert.AreEqual(new LauncherGridCell(1, 0), layout["Automatic"].Cell);
        Assert.IsFalse(layout["Manual"].Bounds.IntersectsWith(layout["Automatic"].Bounds));
    }

    [TestMethod]
    public void Arrange_ConflictingManualCells_FallsBackWithoutOverlap()
    {
        var items = new[]
        {
            new LauncherGridItem("First", 1, 1, Column: 1, Row: 1),
            new LauncherGridItem("Second", 1, 1, Column: 1, Row: 1),
        };

        var layout = LauncherGridLayoutEngine.Arrange(
            items,
            new LauncherGridMetrics(ColumnCount: 3, SlotWidth: 100d, SlotHeight: 80d));

        Assert.AreEqual(new LauncherGridCell(1, 1), layout["First"].Cell);
        Assert.IsFalse(layout["First"].Bounds.IntersectsWith(layout["Second"].Bounds));
    }

    [TestMethod]
    public void Arrange_ClampsManualCellForSpannedItems()
    {
        var items = new[]
        {
            new LauncherGridItem("Folder", 2, 2, Column: 4, Row: 1),
        };

        var layout = LauncherGridLayoutEngine.Arrange(
            items,
            new LauncherGridMetrics(ColumnCount: 4, SlotWidth: 100d, SlotHeight: 80d));

        Assert.AreEqual(new LauncherGridCell(2, 1), layout["Folder"].Cell);
        Assert.AreEqual(200d, layout["Folder"].Bounds.Width, 0.01d);
        Assert.AreEqual(160d, layout["Folder"].Bounds.Height, 0.01d);
    }

    [TestMethod]
    public void Arrange_ClampsWideItemsToAvailableColumns()
    {
        var items = new[] { new LauncherGridItem("Folder", 2, 2) };
        var metrics = new LauncherGridMetrics(ColumnCount: 1, SlotWidth: 120d, SlotHeight: 80d);

        var layout = LauncherGridLayoutEngine.Arrange(items, metrics);

        Assert.AreEqual(1, layout["Folder"].ColumnSpan);
        Assert.AreEqual(2, layout["Folder"].RowSpan);
        Assert.AreEqual(120d, layout["Folder"].Bounds.Width, 0.01d);
        Assert.AreEqual(160d, layout["Folder"].Bounds.Height, 0.01d);
    }

    [TestMethod]
    public void CalculateExtent_UsesBottomOfSpannedRows()
    {
        var items = new[]
        {
            new LauncherGridItem("A", 1, 1),
            new LauncherGridItem("Folder", 2, 2),
            new LauncherGridItem("B", 1, 1),
            new LauncherGridItem("C", 1, 1)
        };
        var metrics = new LauncherGridMetrics(ColumnCount: 3, SlotWidth: 100d, SlotHeight: 80d);

        var layout = LauncherGridLayoutEngine.Arrange(items, metrics);

        LauncherGridExtent extent = LauncherGridLayoutEngine.CalculateExtent(layout, metrics);

        Assert.AreEqual(300d, extent.Width, 0.01d);
        Assert.AreEqual(240d, extent.Height, 0.01d);
    }

    [TestMethod]
    public void CalculateExtent_ReturnsPanelWidthAndZeroHeightWhenEmpty()
    {
        var metrics = new LauncherGridMetrics(ColumnCount: 2, SlotWidth: 90d, SlotHeight: 70d);

        LauncherGridExtent extent = LauncherGridLayoutEngine.CalculateExtent(
            new Dictionary<object, LauncherGridRect>(),
            metrics);

        Assert.AreEqual(180d, extent.Width, 0.01d);
        Assert.AreEqual(0d, extent.Height, 0.01d);
    }
}
