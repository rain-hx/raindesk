using raindesk.Features.Launcher.Application;

namespace raindesk.Tests;

[TestClass]
public class ShortcutDragPolicyTests
{
    [TestMethod]
    public void ForDragCount_SingleIcon_KeepsSourceItemStable()
    {
        var policy = ShortcutDragPolicy.ForDragCount(1);

        Assert.IsFalse(policy.UseBatchPlaceholderDrag);
        Assert.IsTrue(policy.KeepSourceItemInCollection);
        Assert.IsFalse(policy.RestrictDragItemsToPlaceholder);
    }

    [TestMethod]
    public void ForDragCount_MultipleIcons_UsesBatchPlaceholderWithoutReplacingDragItems()
    {
        var policy = ShortcutDragPolicy.ForDragCount(2);

        Assert.IsTrue(policy.UseBatchPlaceholderDrag);
        Assert.IsFalse(policy.KeepSourceItemInCollection);
        Assert.IsFalse(policy.RestrictDragItemsToPlaceholder);
    }

    [TestMethod]
    public void ForDragCount_ZeroIcons_KeepsSourceItemStable()
    {
        var policy = ShortcutDragPolicy.ForDragCount(0);

        Assert.IsFalse(policy.UseBatchPlaceholderDrag);
        Assert.IsTrue(policy.KeepSourceItemInCollection);
        Assert.IsFalse(policy.RestrictDragItemsToPlaceholder);
    }

    [TestMethod]
    public void ForDragCount_LargeBatch_UsesBatchPlaceholder()
    {
        var policy = ShortcutDragPolicy.ForDragCount(10);

        Assert.IsTrue(policy.UseBatchPlaceholderDrag);
        Assert.IsFalse(policy.KeepSourceItemInCollection);
        Assert.IsFalse(policy.RestrictDragItemsToPlaceholder);
    }

    [TestMethod]
    public void ResolveDragStartItems_MultipleIcons_KeepsBatchItems()
    {
        var selectedItems = new[] { "first", "second", "third" };

        var resolved = ShortcutDragPolicy.ResolveDragStartItems(
            selectedItems,
            item => item == "add",
            item => item != "add");

        Assert.IsFalse(resolved.Cancel);
        CollectionAssert.AreEqual(selectedItems, resolved.Items.ToArray());
    }

    [TestMethod]
    public void ResolveDragStartItems_CancelItem_CancelsDrag()
    {
        var selectedItems = new[] { "first", "add" };

        var resolved = ShortcutDragPolicy.ResolveDragStartItems(
            selectedItems,
            item => item == "add",
            item => item != "add");

        Assert.IsTrue(resolved.Cancel);
        Assert.AreEqual(0, resolved.Items.Count);
    }

    [TestMethod]
    public void ResolveDragStartItems_NoValidIcons_CancelsDrag()
    {
        var resolved = ShortcutDragPolicy.ResolveDragStartItems(
            new[] { "caption" },
            item => item == "add",
            item => false);

        Assert.IsTrue(resolved.Cancel);
        Assert.AreEqual(0, resolved.Items.Count);
    }

    [TestMethod]
    public void ShouldSuppressReorderForFolderTarget_SingleIconWithValidTarget_ReturnsTrue()
    {
        Assert.IsTrue(ShortcutDragPolicy.ShouldSuppressReorderForFolderTarget(
            draggedCount: 1,
            hasValidFolderDropTarget: true));
    }

    [TestMethod]
    public void ShouldSuppressReorderForFolderTarget_SingleIconWithoutTarget_ReturnsFalse()
    {
        Assert.IsFalse(ShortcutDragPolicy.ShouldSuppressReorderForFolderTarget(
            draggedCount: 1,
            hasValidFolderDropTarget: false));
    }

    [TestMethod]
    public void ShouldSuppressReorderForFolderTarget_BatchDrag_ReturnsFalse()
    {
        Assert.IsFalse(ShortcutDragPolicy.ShouldSuppressReorderForFolderTarget(
            draggedCount: 2,
            hasValidFolderDropTarget: true));
    }

    [TestMethod]
    public void ShouldApplyLiveReorderPreview_FolderDropIntent_ReturnsFalse()
    {
        Assert.IsFalse(ShortcutDragPolicy.ShouldApplyLiveReorderPreview(
            draggedCount: 1,
            showingAllGroups: true,
            hasFolderDropIntent: true,
            isCrossGroupPreviewActive: false));
    }

    [TestMethod]
    public void ShouldApplyLiveReorderPreview_AllGroupsSingleIconWithoutFolderIntent_ReturnsTrue()
    {
        Assert.IsTrue(ShortcutDragPolicy.ShouldApplyLiveReorderPreview(
            draggedCount: 1,
            showingAllGroups: true,
            hasFolderDropIntent: false,
            isCrossGroupPreviewActive: false));
    }

    [TestMethod]
    public void ShouldApplyLiveReorderPreview_SelectedGroupSingleIconWithoutFolderIntent_ReturnsTrue()
    {
        Assert.IsTrue(ShortcutDragPolicy.ShouldApplyLiveReorderPreview(
            draggedCount: 1,
            showingAllGroups: false,
            hasFolderDropIntent: false,
            isCrossGroupPreviewActive: false));
    }

    [TestMethod]
    public void ShouldApplyLiveReorderPreview_CrossGroupPreview_ReturnsFalse()
    {
        Assert.IsFalse(ShortcutDragPolicy.ShouldApplyLiveReorderPreview(
            draggedCount: 1,
            showingAllGroups: true,
            hasFolderDropIntent: false,
            isCrossGroupPreviewActive: true));
    }
}
