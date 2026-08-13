using raindesk.Features.Workspace.Presentation;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabWidthCalculatorTests
{
    [TestMethod]
    public void NaturalWidth_AddsChromeIconAndSpacingThenRoundsUp()
    {
        double width = WorkspaceGroupTabWidthCalculator.CalculateNatural(
            31.2d,
            WorkspaceGroupTabWidthMetrics.Default);

        Assert.AreEqual(86d, width);
    }

    [TestMethod]
    public void NaturalWidth_RespectsMinimum()
    {
        var metrics = WorkspaceGroupTabWidthMetrics.Default with
        {
            HorizontalChrome = 0d,
            IconSlotWidth = 0d,
            ColumnSpacing = 0d
        };

        Assert.AreEqual(48d, WorkspaceGroupTabWidthCalculator.CalculateNatural(5d, metrics));
    }

    [TestMethod]
    public void EditWidth_UsesConfiguredContentAndChrome()
    {
        Assert.AreEqual(
            124d,
            WorkspaceGroupTabWidthCalculator.CalculateEdit(WorkspaceGroupTabWidthMetrics.Default));
    }
}
