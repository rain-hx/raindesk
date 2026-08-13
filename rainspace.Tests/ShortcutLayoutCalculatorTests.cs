using Microsoft.VisualStudio.TestTools.UnitTesting;
using rainspace.Features.Launcher.Application;

namespace rainspace.Tests;

[TestClass]
public class ShortcutLayoutCalculatorTests
{
    [TestMethod]
    public void Calculate_UsesMoreColumnsWhenWidthAllows()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(343d);

        Assert.AreEqual(3, metrics.ColumnCount);
        Assert.AreEqual(114.33333333333333d, metrics.SlotWidth, 0.01d);
        Assert.AreEqual(88d, metrics.CardWidth, 0.01d);
        Assert.AreEqual(24d, metrics.GapWidth, 0.01d);
        Assert.AreEqual(0d, metrics.HorizontalOffset);
        Assert.AreEqual(104d, metrics.SlotHeight);
    }

    [TestMethod]
    public void Calculate_KeepsSingleLineSlotHeightForNarrowWidths()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(120d);

        Assert.AreEqual(1, metrics.ColumnCount);
        Assert.AreEqual(120d, metrics.SlotWidth);
        Assert.AreEqual(88d, metrics.CardWidth);
        Assert.AreEqual(24d, metrics.GapWidth);
        Assert.AreEqual(0d, metrics.HorizontalOffset);
        Assert.AreEqual(104d, metrics.SlotHeight);
    }

    [TestMethod]
    public void Calculate_KeepsOneColumnUntilLauncherSlotFits()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(176d);

        Assert.AreEqual(2, metrics.ColumnCount);
        Assert.AreEqual(88d, metrics.SlotWidth);
        Assert.AreEqual(88d, metrics.CardWidth);
        Assert.AreEqual(0d, metrics.GapWidth);
        Assert.AreEqual(0d, metrics.HorizontalOffset);
    }

    [TestMethod]
    public void Calculate_SwitchesToTwoLauncherColumns()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(264d);

        Assert.AreEqual(3, metrics.ColumnCount);
        Assert.AreEqual(88d, metrics.SlotWidth);
        Assert.AreEqual(88d, metrics.CardWidth);
        Assert.AreEqual(0d, metrics.GapWidth);
        Assert.AreEqual(0d, metrics.HorizontalOffset);
    }

    [TestMethod]
    public void Calculate_SwitchesToThreeLauncherColumns()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(352d);

        Assert.AreEqual(4, metrics.ColumnCount);
        Assert.AreEqual(88d, metrics.SlotWidth, 0.01d);
        Assert.AreEqual(88d, metrics.CardWidth, 0.01d);
        Assert.AreEqual(0d, metrics.GapWidth, 0.01d);
        Assert.AreEqual(0d, metrics.HorizontalOffset);
    }

    [TestMethod]
    public void Calculate_FillsWidePanelsWithMoreColumns()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(700d);

        Assert.AreEqual(7, metrics.ColumnCount);
        Assert.AreEqual(100d, metrics.SlotWidth, 0.01d);
        Assert.AreEqual(88d, metrics.CardWidth, 0.01d);
        Assert.AreEqual(12d, metrics.GapWidth, 0.01d);
        Assert.AreEqual(0d, metrics.HorizontalOffset);
    }

    [TestMethod]
    public void Calculate_UsesTwelveLauncherColumnsForWidePanels()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(1440d);

        Assert.AreEqual(12, metrics.ColumnCount);
        Assert.AreEqual(120d, metrics.SlotWidth, 0.01d);
        Assert.AreEqual(88d, metrics.CardWidth, 0.01d);
        Assert.AreEqual(24d, metrics.GapWidth, 0.01d);
        Assert.AreEqual(104d, metrics.SlotHeight);
    }

    [TestMethod]
    public void Calculate_KeepsSlotWidthEqualToAvailableWidthDividedByColumns()
    {
        var metrics = ShortcutLayoutCalculator.Calculate(560d);

        Assert.AreEqual(6, metrics.ColumnCount);
        Assert.AreEqual(560d / 6d, metrics.SlotWidth, 0.01d);
        Assert.AreEqual(0d, metrics.ColumnCount * metrics.SlotWidth - 560d, 0.01d);
    }

    [TestMethod]
    public void HasMeaningfulLayoutChange_ReturnsFalse_ForSubPixelNoise()
    {
        var metrics = new ShortcutLayoutMetrics(3, 94d, 94d, 88d, 6d);
        var previous = new ShortcutLayoutSnapshot(
            AvailableWidth: 342.4d,
            TileCount: 12,
            ColumnCount: 3,
            SlotWidth: 93.4d,
            CardWidth: 87.4d,
            GapWidth: 5.4d,
            HorizontalOffset: 0.25d,
            SlotHeight: 93.4d);

        bool changed = ShortcutLayoutCalculator.HasMeaningfulLayoutChange(
            metrics,
            previous,
            availableWidth: 343d,
            tileCount: 12);

        Assert.IsFalse(changed);
    }

    [TestMethod]
    public void HasMeaningfulLayoutChange_ReturnsTrue_WhenTileCountChanges()
    {
        var metrics = new ShortcutLayoutMetrics(3, 94d, 94d, 88d, 6d);
        var previous = new ShortcutLayoutSnapshot(
            AvailableWidth: 343d,
            TileCount: 12,
            ColumnCount: 3,
            SlotWidth: 94d,
            CardWidth: 88d,
            GapWidth: 6d,
            HorizontalOffset: 0d,
            SlotHeight: 94d);

        bool changed = ShortcutLayoutCalculator.HasMeaningfulLayoutChange(
            metrics,
            previous,
            availableWidth: 343d,
            tileCount: 13);

        Assert.IsTrue(changed);
    }

    [TestMethod]
    public void MovePlaceholderBlock_WhenPrimaryPlaceholderMoved_RejoinsWholeBlockAtDropTarget()
    {
        string[] currentItems = ["A", "B", "P1", "P2", "C", "D", "E", "F", "P0"];
        string[] placeholders = ["P0", "P1", "P2"];

        var result = ShortcutLayoutCalculator.MovePlaceholderBlock(currentItems, placeholders, "P0", 8);

        CollectionAssert.AreEqual(new[] { "A", "B", "C", "D", "E", "F", "P0", "P1", "P2" }, result.ToArray());
    }

    [TestMethod]
    public void MovePlaceholderBlock_WhenContiguousBlockAtStart_MovesWholeBlockToEnd()
    {
        string[] placeholders = Enumerable.Range(1, 35).Select(i => $"P{i}").ToArray();
        string[] icons = ["I1", "I2", "I3", "I4", "I5", "I6", "I7", "I8", "ADD"];
        string[] currentItems = placeholders.Concat(icons).ToArray();

        var result = ShortcutLayoutCalculator.MovePlaceholderBlock(currentItems, placeholders, "P1", 8);

        string[] expected = icons.Take(8).Concat(placeholders).Append("ADD").ToArray();
        CollectionAssert.AreEqual(expected, result.ToArray());
    }

    [TestMethod]
    public void ResolvePlaceholderBlockLogicalTargetIndex_WhenTargetAfterBlock_ReturnsIndexBeforeShiftedIcon()
    {
        string[] currentItems = ["A", "P0", "P1", "B", "C", "ADD"];
        string[] placeholders = ["P0", "P1"];

        int result = ShortcutLayoutCalculator.ResolvePlaceholderBlockLogicalTargetIndex(
            currentItems,
            placeholders,
            visualTargetIndex: 3);

        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void MovePlaceholderBlock_WithLogicalTarget_KeepsBlockBeforeShiftedIcon()
    {
        string[] currentItems = ["A", "P0", "P1", "B", "C", "ADD"];
        string[] placeholders = ["P0", "P1"];
        int logicalTarget = ShortcutLayoutCalculator.ResolvePlaceholderBlockLogicalTargetIndex(
            currentItems,
            placeholders,
            visualTargetIndex: 3);

        var result = ShortcutLayoutCalculator.MovePlaceholderBlock(
            currentItems,
            placeholders,
            "P0",
            logicalTarget);

        CollectionAssert.AreEqual(currentItems, result.ToArray());
    }

    [TestMethod]
    public void CanDropOnGroupTab_FromAllGroupsToSameGroup_ReturnsFalse()
    {
        string icon = "App";
        string sourceGroup = "Tools";

        bool result = ShortcutLayoutCalculator.CanDropOnGroupTab(
            [icon],
            sourceGroup,
            true,
            item => item == icon ? sourceGroup : null);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CanDropOnGroupTab_FromAllGroupsToDifferentGroup_ReturnsTrue()
    {
        string icon = "App";

        bool result = ShortcutLayoutCalculator.CanDropOnGroupTab(
            [icon],
            "Work",
            true,
            item => item == icon ? "Tools" : null);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void FilterValidDragItems_RemovesNullAndPlaceholders()
    {
        string[] items = ["App", "Add", "Tool"];

        var result = ShortcutLayoutCalculator.FilterValidDragItems(
            items,
            item => item == "Add");

        CollectionAssert.AreEqual(new[] { "App", "Tool" }, result.ToArray());
    }

    [TestMethod]
    public void FilterValidDragItems_ReturnsEmptyForNullSource()
    {
        var result = ShortcutLayoutCalculator.FilterValidDragItems<string>(
            null!,
            item => item == "Add");

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void BuildBatchDragRestoreItems_UsesDragStartOrderAndRemovesDraggedItems()
    {
        string[] dragStart = ["A", "B", "C", "D"];
        string[] beforePreview = ["Before"];
        string[] current = ["Current"];
        string[] dragged = ["B", "D"];
        string[] placeholders = [];

        var result = ShortcutLayoutCalculator.BuildBatchDragRestoreItems(
            dragStart,
            beforePreview,
            current,
            dragged,
            placeholders,
            sourceInsertIndex: -1,
            fallbackInsertIndex: -1,
            isPlaceholder: item => item.StartsWith("P"));

        CollectionAssert.AreEqual(new[] { "A", "C" }, result.Items.ToArray());
        Assert.AreEqual(-1, result.InsertIndex);
    }

    [TestMethod]
    public void BuildBatchDragRestoreItems_InsertsPlaceholderAtClampedSourceIndex()
    {
        string[] dragStart = ["A", "B", "C", "D"];
        string[] dragged = ["B"];
        string[] placeholders = ["P1", "P2"];

        var result = ShortcutLayoutCalculator.BuildBatchDragRestoreItems(
            dragStart,
            beforePreviewOrder: null,
            currentOrder: null,
            draggedItems: dragged,
            placeholders: placeholders,
            sourceInsertIndex: 10,
            fallbackInsertIndex: 1,
            isPlaceholder: item => item.StartsWith("P"));

        CollectionAssert.AreEqual(new[] { "A", "C", "D", "P1", "P2" }, result.Items.ToArray());
        Assert.AreEqual(3, result.InsertIndex);
    }

    [TestMethod]
    public void BuildBatchDragRestoreItems_InsertsWholePlaceholderBlock()
    {
        string[] dragStart = ["A", "B", "C", "D", "E"];
        string[] dragged = ["B", "D", "E"];
        string[] placeholders = ["P0", "P1", "P2"];

        var result = ShortcutLayoutCalculator.BuildBatchDragRestoreItems(
            dragStart,
            beforePreviewOrder: null,
            currentOrder: null,
            draggedItems: dragged,
            placeholders: placeholders,
            sourceInsertIndex: 1,
            fallbackInsertIndex: -1,
            isPlaceholder: item => item.StartsWith("P"));

        CollectionAssert.AreEqual(new[] { "A", "P0", "P1", "P2", "C" }, result.Items.ToArray());
        Assert.AreEqual(1, result.InsertIndex);
    }

    [TestMethod]
    public void CalculateTileDisplaySize_UsesFolderSpans()
    {
        var metrics = new ShortcutLayoutMetrics(
            ColumnCount: 4,
            SlotWidth: 88d,
            SlotHeight: 76d,
            CardWidth: 84d,
            GapWidth: 4d);

        var size = ShortcutLayoutCalculator.CalculateTileDisplaySize(metrics, columnSpan: 2, rowSpan: 2);

        Assert.AreEqual(176d, size.Width, 0.01d);
        Assert.AreEqual(152d, size.Height, 0.01d);
    }

    [TestMethod]
    public void CalculateTileDisplaySize_ClampsInvalidSpans()
    {
        var metrics = new ShortcutLayoutMetrics(
            ColumnCount: 4,
            SlotWidth: 88d,
            SlotHeight: 76d,
            CardWidth: 84d,
            GapWidth: 4d);

        var size = ShortcutLayoutCalculator.CalculateTileDisplaySize(metrics, columnSpan: 0, rowSpan: -1);

        Assert.AreEqual(88d, size.Width, 0.01d);
        Assert.AreEqual(76d, size.Height, 0.01d);
    }

    [TestMethod]
    public void ClampFolderColumnSpan_ClampsToAvailableColumns()
    {
        Assert.AreEqual(1, ShortcutLayoutCalculator.ClampFolderColumnSpan(columnSpan: 2, columnCount: 1));
        Assert.AreEqual(2, ShortcutLayoutCalculator.ClampFolderColumnSpan(columnSpan: 2, columnCount: 4));
    }

    [TestMethod]
    public void CalculateTileDisplaySize_ClampsFolderSpanToColumnCount()
    {
        var metrics = new ShortcutLayoutMetrics(
            ColumnCount: 1,
            SlotWidth: 120d,
            SlotHeight: 76d,
            CardWidth: 96d,
            GapWidth: 16d);

        var size = ShortcutLayoutCalculator.CalculateFolderTileDisplaySize(
            metrics,
            columnSpan: 2,
            rowSpan: 2);

        Assert.AreEqual(120d, size.Width, 0.01d);
        Assert.AreEqual(152d, size.Height, 0.01d);
    }

    [TestMethod]
    public void CalculatePanelTileDisplaySize_WithoutVariableSizedItems_KeepsSingleSlot()
    {
        var metrics = new ShortcutLayoutMetrics(
            ColumnCount: 4,
            SlotWidth: 88d,
            SlotHeight: 76d,
            CardWidth: 84d,
            GapWidth: 4d);

        var size = ShortcutLayoutCalculator.CalculatePanelTileDisplaySize(
            metrics,
            columnSpan: 2,
            rowSpan: 2,
            supportsVariableSizedItems: false);

        Assert.AreEqual(88d, size.Width, 0.01d);
        Assert.AreEqual(76d, size.Height, 0.01d);
    }

    [TestMethod]
    public void GetFolderStackPanelHeight_FitsSingleSlot()
    {
        double stackHeight = ShortcutLayoutCalculator.GetFolderStackPanelHeight(slotHeight: 94d, rowSpan: 1);

        Assert.AreEqual(86d, stackHeight, 0.01d);
        Assert.AreEqual(64d, ShortcutLayoutCalculator.GetFolderBodyHeight(94d, 1), 0.01d);
    }

    [TestMethod]
    public void ResolveWrapGridDropIndex_UsesSlotFootprintsInListOrder()
    {
        var targets = new[]
        {
            new ShortcutSlotHitTarget(0, 0d, 0d, 88d, 94d),
            new ShortcutSlotHitTarget(1, 88d, 0d, 88d, 94d),
            new ShortcutSlotHitTarget(2, 176d, 0d, 88d, 94d),
            new ShortcutSlotHitTarget(3, 0d, 94d, 88d, 94d),
        };

        Assert.AreEqual(1, ShortcutLayoutCalculator.ResolveWrapGridDropIndex(targets, 100d, 40d, 3));
        Assert.AreEqual(3, ShortcutLayoutCalculator.ResolveWrapGridDropIndex(targets, 10d, 100d, 3));
        Assert.AreEqual(3, ShortcutLayoutCalculator.ResolveWrapGridDropIndex(targets, 250d, 40d, 3));
    }
}
