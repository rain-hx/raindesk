namespace raindesk.Tests;

[TestClass]
public class RightCardLauncherUiContractTests
{
    [TestMethod]
    public void ShortcutListViewItemStyle_DoesNotScaleWholeContainerOnPointerOver()
    {
        string xaml = ReadRightCardXaml();

        Assert.IsFalse(
            xaml.Contains("Storyboard.TargetName=\"Root\"", StringComparison.Ordinal)
            && xaml.Contains("ScaleTransform.ScaleX", StringComparison.Ordinal)
            && xaml.Contains("To=\"1.05\"", StringComparison.Ordinal),
            "Shortcut item hover must not scale the entire ListViewItem; only the icon image may animate.");
    }

    [TestMethod]
    public void LauncherGridPanels_AreTopAligned()
    {
        string xaml = ReadRightCardXaml();
        int launcherPanelCount = CountLauncherPanelTags(xaml, requireTopAlignment: false);
        int topAlignedLauncherPanelCount = CountLauncherPanelTags(xaml, requireTopAlignment: true);

        Assert.IsTrue(launcherPanelCount >= 2, "Main shortcuts and folder overlay should both use top-aligned launcher panels.");
        Assert.IsTrue(
            topAlignedLauncherPanelCount >= launcherPanelCount,
            "Launcher panels must be top-aligned so compact grids do not float in the available viewport.");
    }

    [TestMethod]
    public void MainShortcutList_UsesLauncherGridPanelForFolderSpans()
    {
        string xaml = ReadRightCardXaml();
        int listIndex = xaml.IndexOf("x:Name=\"IconListView\"", StringComparison.Ordinal);
        int itemPanelIndex = xaml.IndexOf("<ListView.ItemsPanel>", listIndex, StringComparison.Ordinal);
        int templateEndIndex = xaml.IndexOf("</ItemsPanelTemplate>", itemPanelIndex, StringComparison.Ordinal);

        Assert.IsTrue(listIndex >= 0 && itemPanelIndex > listIndex && templateEndIndex > itemPanelIndex);

        string mainItemsPanel = xaml[itemPanelIndex..templateEndIndex];
        Assert.IsTrue(
            mainItemsPanel.Contains("launcher:LauncherGridPanel", StringComparison.Ordinal),
            "Main shortcut list must use LauncherGridPanel so folder tiles with larger spans push surrounding icons aside.");
    }

    [TestMethod]
    public void MainShortcutList_CommitsFolderOperationsFromDropEvent()
    {
        string xaml = ReadRightCardXaml();
        string source = ReadRightCardSource();

        Assert.IsTrue(
            xaml.Contains("Drop=\"IconListView_Drop\"", StringComparison.Ordinal),
            "The launcher list needs a real Drop handler so folder commits do not depend only on DragItemsCompleted pointer state.");
        Assert.IsTrue(
            source.Contains("private void IconListView_Drop", StringComparison.Ordinal)
            && source.Contains("_dragBusiness.HandleIconListViewDrop", StringComparison.Ordinal),
            "The Drop handler must delegate folder resolution and commit to the business controller.");
    }

    [TestMethod]
    public void LauncherGridPanel_UsesManualCoordinatesAndHidesUnboundContainers()
    {
        string source = ReadLauncherGridPanelSource();

        Assert.IsTrue(
            source.Contains("icon?.HasManualLauncherGridPosition_NoWrite == true", StringComparison.Ordinal)
            && source.Contains("icon.LauncherGridColumn", StringComparison.Ordinal)
            && source.Contains("icon.LauncherGridRow", StringComparison.Ordinal),
            "The real panel must consume the same manual grid coordinates used by drag preview hit testing.");
        Assert.IsTrue(
            source.Contains("child.Arrange(new Rect(0d, 0d, 0d, 0d))", StringComparison.Ordinal),
            "Unbound or recycled containers must not be stacked in the visible top-left grid cell.");
    }

    [TestMethod]
    public void AllGroupsLayout_CanIgnoreCoordinatesOwnedBySourceGroups()
    {
        string panelSource = ReadLauncherGridPanelSource();
        string xaml = ReadRightCardXaml();
        string rightCardSource = ReadRightCardSource();

        Assert.IsTrue(
            panelSource.Contains("UseManualPositions && icon?.HasManualLauncherGridPosition_NoWrite", StringComparison.Ordinal),
            "The launcher panel must be able to lay out aggregate items strictly in list order.");
        Assert.IsTrue(
            xaml.Contains("UseManualPositions=\"{Binding UseManualShortcutGridPositions_NoWrite", StringComparison.Ordinal)
            && rightCardSource.Contains("UseManualShortcutGridPositions_NoWrite => !showingAllGroups", StringComparison.Ordinal),
            "All Groups must opt out of source-group manual coordinates while normal groups keep them.");
    }

    [TestMethod]
    public void MainShortcutItemContainers_StretchContentForSpannedFolders()
    {
        string xaml = ReadRightCardXaml();
        int styleIndex = xaml.IndexOf("<Style x:Key=\"MyListViewItemStyle\"", StringComparison.Ordinal);
        int styleEndIndex = xaml.IndexOf("</Style>", styleIndex, StringComparison.Ordinal);
        Assert.IsTrue(styleIndex >= 0 && styleEndIndex > styleIndex, "Could not locate main shortcut item style.");

        string styleBlock = xaml[styleIndex..styleEndIndex];
        Assert.IsTrue(
            styleBlock.Contains("<Setter Property=\"HorizontalContentAlignment\" Value=\"Stretch\"/>", StringComparison.Ordinal),
            "Main shortcut containers must stretch their content so two-column folders are not clipped when moved toward the right edge.");
        Assert.IsTrue(
            styleBlock.Contains("<Setter Property=\"VerticalContentAlignment\" Value=\"Stretch\"/>", StringComparison.Ordinal),
            "Main shortcut containers must stretch vertically so tall folders keep their full reserved slot while dragging.");
    }

    [TestMethod]
    public void FolderPreviewContent_ResizesWithFolderSpan()
    {
        string xaml = ReadRightCardXaml();
        string source = ReadFolderTileVisualAdapterSource();

        Assert.IsTrue(
            xaml.Contains("x:Name=\"FolderPreviewItems\"", StringComparison.Ordinal),
            "Folder preview grid must be named so span changes can resize its content, not just the outer folder shell.");
        Assert.IsTrue(
            xaml.Contains("FolderPreviewImage1_NoWrite", StringComparison.Ordinal)
            && xaml.Contains("FolderPreviewImage6_NoWrite", StringComparison.Ordinal)
            && xaml.Contains("FolderPreviewOverflowCount_NoWrite", StringComparison.Ordinal),
            "Folder cover previews should use fixed phone-style image slots with an overflow badge instead of nesting another items control.");
        Assert.IsTrue(
            source.Contains("FolderPreviewItems", StringComparison.Ordinal)
            && source.Contains("ApplyPreview(folderVisual, icon)", StringComparison.Ordinal),
            "Folder visual layout must resize the preview content when folder size changes.");
    }

    [TestMethod]
    public void FolderVisualContent_GrowsWithSpannedGridSlot()
    {
        string source = ReadFolderTileVisualAdapterSource();
        int methodIndex = source.IndexOf("public static void Apply(", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("public static double GetPreviewIconSize", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate folder tile adapter Apply body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("folderVisual.Width = GetBodyWidth(tileSize.Width, columnSpan)", StringComparison.Ordinal)
            && body.Contains("folderVisual.Height = GetBodyHeight(metrics.SlotHeight, rowSpan, topInset)", StringComparison.Ordinal),
            "Folder content must grow with the selected folder size; otherwise medium/large folders reserve a larger slot but still render as a small icon.");
        Assert.IsTrue(
            body.Contains("ApplySurface(folderVisual, icon.FolderSize)", StringComparison.Ordinal),
            "Expanded phone-style folder covers need their surface radius updated along with the larger body size.");
        Assert.IsFalse(
            body.Contains("folderVisual.Width = LauncherShortcutDragPreviewSize", StringComparison.Ordinal)
            || body.Contains("folderVisual.Height = LauncherShortcutDragPreviewSize", StringComparison.Ordinal),
            "The static folder visual must not be tied to the fixed drag-preview capture size.");
    }

    [TestMethod]
    public void FolderVisualContent_AlignsTopWithRegularShortcutIcons()
    {
        string source = ReadFolderTileVisualAdapterSource();
        int methodIndex = source.IndexOf("public static void Apply(", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("public static double GetPreviewIconSize", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate folder tile adapter Apply body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            source.Contains("GetShortcutIconStackTopInset", StringComparison.Ordinal)
            && body.Contains("double topInset = GetShortcutIconStackTopInset(metrics.SlotHeight)", StringComparison.Ordinal)
            && body.Contains("folderVisual.Margin = new Thickness(0d, topInset, 0d, 0d)", StringComparison.Ordinal),
            "Folder visuals must use the same top inset as regular shortcut icons so large folders and neighboring icons align.");
        Assert.IsTrue(
            body.Contains("GetBodyHeight(metrics.SlotHeight, rowSpan, topInset)", StringComparison.Ordinal),
            "Large folder body height must reserve the shared top inset instead of overflowing the spanned tile.");
    }

    [TestMethod]
    public void FolderPreviewContent_KeepsSafePaddingForAnimatedIcons()
    {
        string xaml = ReadRightCardXaml();
        string source = ReadFolderTileVisualAdapterSource();
        string calculator = ReadShortcutLayoutCalculatorSource();

        Assert.IsTrue(
            calculator.Contains("ShortcutAnimationSafePadding", StringComparison.Ordinal)
            && calculator.Contains("private const double SlotHeight = BaseSlotHeight + ShortcutAnimationSafePadding", StringComparison.Ordinal),
            "Launcher slots must reserve vertical safe space so scaled drag/yield icons, shadows, and titles are not clipped.");
        Assert.IsTrue(
            xaml.Contains("Margin=\"6\"", StringComparison.Ordinal)
            && xaml.Contains("ColumnSpacing=\"4\"", StringComparison.Ordinal)
            && xaml.Contains("RowSpacing=\"4\"", StringComparison.Ordinal)
            && xaml.Contains("HorizontalAlignment=\"Center\"", StringComparison.Ordinal)
            && xaml.Contains("VerticalAlignment=\"Center\"", StringComparison.Ordinal),
            "Folder preview icons need inner padding and centered image layout so small icons are not cut off inside the folder card.");
        Assert.IsTrue(
            source.Contains("PreviewSafePadding", StringComparison.Ordinal)
            && source.Contains("PreviewIconGap", StringComparison.Ordinal)
            && source.Contains("int columnCount = itemCount <= 2 ? itemCount : 2", StringComparison.Ordinal)
            && source.Contains("double itemSize = iconSize + PreviewIconGap", StringComparison.Ordinal)
            && source.Contains("previewItems.Margin = new Thickness(PreviewSafePadding)", StringComparison.Ordinal)
            && source.Contains("previewItems.ColumnSpacing = columnCount > 1 ? PreviewIconGap : 0d", StringComparison.Ordinal)
            && source.Contains("previewItems.RowSpacing = rowCount > 1 ? PreviewIconGap : 0d", StringComparison.Ordinal)
            && source.Contains("SetGridTracks(previewItems, columnCount, rowCount)", StringComparison.Ordinal),
            "Folder preview resizing must keep explicit safe padding/gaps and activate only the visible grid rows/columns when folders change size.");
    }

    [TestMethod]
    public void FolderPreviewContent_UsesLargePhoneCoverIcons()
    {
        string source = ReadFolderTileVisualAdapterSource();

        Assert.IsTrue(
            source.Contains("int columnCount = itemCount <= 2 ? itemCount : 2", StringComparison.Ordinal),
            "Phone-style folder covers should keep a two-column preview so child app icons stay large and recognizable.");
        Assert.IsTrue(
            source.Contains("ShortcutFolderSize.Large => 44d", StringComparison.Ordinal)
            && source.Contains("ShortcutFolderSize.Medium => 22d", StringComparison.Ordinal),
            "Large folder covers must promote child icons well beyond the old tiny grid preview size.");
    }

    [TestMethod]
    public void FolderPreviewContent_ShrinksToPreviewGridBeforeCentering()
    {
        string source = ReadFolderTileVisualAdapterSource();
        int methodIndex = source.IndexOf("private static void ApplyPreview", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("private static void SetGridTracks", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate folder tile adapter ApplyPreview body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("previewItems.Width = itemSize * columnCount", StringComparison.Ordinal)
            && body.Contains("previewItems.Height = itemSize * rowCount", StringComparison.Ordinal),
            "Folder preview ItemsControl must shrink to the actual icon grid before it is centered; otherwise single preview icons sit at the folder's top-left.");
        Assert.IsFalse(
            body.Contains("Math.Clamp(", StringComparison.Ordinal)
            && body.Contains("48d", StringComparison.Ordinal),
            "A fixed 48px minimum preview panel makes one-icon folders look left-aligned inside larger folder bodies.");
    }

    [TestMethod]
    public void FolderLabel_UsesExpandedTileWidthAndReservedTemplateSpace()
    {
        string xaml = ReadRightCardXaml();
        string source = ReadFolderTileVisualAdapterSource();

        int folderIndex = xaml.IndexOf("Visibility=\"{x:Bind IsFolder", StringComparison.Ordinal);
        int templateEndIndex = xaml.IndexOf("</DataTemplate>", folderIndex, StringComparison.Ordinal);
        Assert.IsTrue(folderIndex >= 0 && templateEndIndex > folderIndex, "Could not locate folder template block.");

        string folderBlock = xaml[folderIndex..templateEndIndex];
        Assert.IsTrue(
            xaml.Contains("x:Name=\"ShortcutTemplateRoot\"", StringComparison.Ordinal)
            && folderBlock.Contains("Width=\"{Binding ActualWidth, ElementName=ShortcutTemplateRoot, Mode=OneWay}\"", StringComparison.Ordinal),
            "Folder title must use the expanded folder tile width so larger folders do not clip or visually split the name.");
        Assert.IsTrue(
            source.Contains("LabelBlockHeight = 22d", StringComparison.Ordinal)
            && source.Contains("StackSpacing = 8d", StringComparison.Ordinal),
            "Folder layout reservation must match the template title height and StackPanel spacing.");
    }

    [TestMethod]
    public void ShortcutLayoutUpdate_ReappliesFolderVisualsWhenOnlyFolderSizeChanges()
    {
        string source = ReadLayoutApplyControllerSource();
        int applyCoreIndex = source.IndexOf("private void ApplyCore", StringComparison.Ordinal);
        int shouldApplyIndex = source.IndexOf("!_state.ShouldApply", applyCoreIndex, StringComparison.Ordinal);
        int noMeaningfulReturnIndex = source.IndexOf("return;", shouldApplyIndex, StringComparison.Ordinal);
        Assert.IsTrue(
            applyCoreIndex >= 0 && shouldApplyIndex > applyCoreIndex && noMeaningfulReturnIndex > shouldApplyIndex,
            "Could not locate the no-meaningful-layout-change branch in the layout apply controller.");

        string noMeaningfulChangeBranch = source[shouldApplyIndex..noMeaningfulReturnIndex];
        Assert.IsTrue(
            noMeaningfulChangeBranch.Contains("_refreshSpans()", StringComparison.Ordinal),
            "Folder size changes do not alter layout metrics, so the early-return path must still reapply folder spans and visuals.");
    }

    [TestMethod]
    public void DragContainerRestore_ReappliesFolderVisuals()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadShortcutContainerStateControllerSource();
        int methodIndex = source.IndexOf("private void RestoreVisibleShortcutContainerState()", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("public void RefreshGlassBackdrop", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate RestoreVisibleShortcutContainerState body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("_shortcutContainers.Restore(shortcutTiles)", StringComparison.Ordinal)
            && controllerSource.Contains("_refreshItemSpans();", StringComparison.Ordinal),
            "Drag cleanup must reapply folder spans and visuals; otherwise large folders reset to the 64px template default after dragging.");
    }

    [TestMethod]
    public void BatchDragPlaceholder_PreservesDraggedFolderSpan()
    {
        string source = ReadRightCardSource();
        string panelSource = ReadLauncherGridPanelSource();
        string iconSource = ReadIconInfoSource();
        string coordinatorSource = ReadBatchPlaceholderCoordinatorSource();

        Assert.IsTrue(
            iconSource.Contains("DragPlaceholderColumnSpan_NoWrite", StringComparison.Ordinal)
            && iconSource.Contains("DragPlaceholderRowSpan_NoWrite", StringComparison.Ordinal),
            "Drag yield placeholders need their own span fields so moving a folder does not collapse its reserved slot to a normal icon size.");
        Assert.IsTrue(
            coordinatorSource.Contains("CreatePlaceholder(IconInfo sourceIcon, int groupCount)", StringComparison.Ordinal)
            && coordinatorSource.Contains("DragPlaceholderColumnSpan_NoWrite = columnSpan", StringComparison.Ordinal)
            && coordinatorSource.Contains("DragPlaceholderRowSpan_NoWrite = rowSpan", StringComparison.Ordinal)
            && coordinatorSource.Contains("sourceIcon.FolderColumnSpan_NoWrite", StringComparison.Ordinal)
            && coordinatorSource.Contains("sourceIcon.FolderRowSpan_NoWrite", StringComparison.Ordinal),
            "Batch drag placeholders must copy the dragged folder span at creation time.");
        // _batchDragPlaceholders.TryBegin is now called inside the business controller during drag start
        string controllerSource = ReadDragBusinessControllerSource();
        Assert.IsTrue(
            controllerSource.Contains("_batchDragPlaceholders.TryBegin", StringComparison.Ordinal),
            "Business controller must initiate batch placeholder creation from the drag start path.");
        Assert.IsTrue(
            panelSource.Contains("GetShortcutColumnSpan(icon)", StringComparison.Ordinal)
            && panelSource.Contains("GetShortcutRowSpan(icon)", StringComparison.Ordinal),
            "LauncherGridPanel must arrange drag placeholders with their copied folder span instead of treating every placeholder as 1x1.");
    }

    [TestMethod]
    public void ShortcutTemplate_UsesStableContextFlyoutAndHasNoAddTile()
    {
        string xaml = ReadRightCardXaml();

        int templateRootIndex = xaml.IndexOf("x:Name=\"ShortcutTemplateRoot\"", StringComparison.Ordinal);
        int launchableIndex = xaml.IndexOf("IsLaunchableShortcut_NoWrite", templateRootIndex, StringComparison.Ordinal);
        Assert.IsTrue(templateRootIndex >= 0 && launchableIndex > templateRootIndex, "Could not locate shortcut item template root.");

        string templateRootTag = xaml[templateRootIndex..launchableIndex];
        Assert.IsTrue(
            templateRootTag.Contains("ContextFlyout=\"{StaticResource IconDialog}\"", StringComparison.Ordinal),
            "Normal shortcut and folder template root must own IconDialog.");

        Assert.IsFalse(xaml.Contains("AddShortcutContextFlyout", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("Text=\"添加快捷方式\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ShortcutLayout_AutoCompactsWithoutManualCommand()
    {
        string xaml = ReadRightCardXaml();
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();

        Assert.IsFalse(xaml.Contains("OrganizeShortcutsButton", StringComparison.Ordinal));
        Assert.IsFalse(xaml.Contains("Text=\"一键整理\"", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("OrganizeShortcuts_Click", StringComparison.Ordinal));

        // RebuildShortcutTiles 已迁移到 _dragBusiness，验证直接调用存在
        Assert.IsTrue(
            source.Contains("_dragBusiness.RebuildShortcutTiles", StringComparison.Ordinal),
            "RightCard 应通过 _dragBusiness.RebuildShortcutTiles 重建快捷方式列表。");

        // 拖拽完成后的布局压缩已迁移到 _dragBusiness 内部处理
        Assert.IsTrue(
            source.Contains("_dragBusiness.CompleteIconDragCleanup", StringComparison.Ordinal),
            "RightCard 应通过 _dragBusiness 处理拖拽完成后的清理和布局压缩。");

        Assert.IsTrue(
            (controllerSource.Contains("_layoutApply.Compact(shortcutTiles, preserveExistingVisualOrder: false)", StringComparison.Ordinal)
             || controllerSource.Contains("_ctx.LayoutApply.Compact(ShortcutTiles, preserveExistingVisualOrder: false)", StringComparison.Ordinal))
            && controllerSource.Contains("_allGroupsOrder.SetCurrent", StringComparison.Ordinal),
            "All-groups ordering must fill layout gaps automatically via the business controller.");
    }

    [TestMethod]
    public void RecycledShortcutContainers_ClearContextFlyout()
    {
        string source = ReadRightCardSource();
        int methodIndex = source.IndexOf(
            "private void IconListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)",
            StringComparison.Ordinal);
        int recycleIndex = source.IndexOf("if (args.InRecycleQueue)", methodIndex, StringComparison.Ordinal);
        int recycleReturnIndex = source.IndexOf("return;", recycleIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && recycleIndex > methodIndex && recycleReturnIndex > recycleIndex, "Could not locate shortcut container recycle branch.");

        string recycleBody = source[recycleIndex..recycleReturnIndex];
        Assert.IsTrue(
            recycleBody.Contains("recycledItem.ContextFlyout = null", StringComparison.Ordinal),
            "Recycled ListViewItem containers must clear ContextFlyout before they are reused for folders or apps.");
    }

    [TestMethod]
    public void LauncherGridPanel_InvalidatesLayoutWhenFolderSizeChanges()
    {
        string source = ReadLauncherGridPanelSource();

        Assert.IsTrue(
            source.Contains("INotifyPropertyChanged", StringComparison.Ordinal)
            && source.Contains("PropertyChanged", StringComparison.Ordinal)
            && source.Contains("FolderSize", StringComparison.Ordinal)
            && source.Contains("FolderColumnSpan_NoWrite", StringComparison.Ordinal)
            && source.Contains("FolderRowSpan_NoWrite", StringComparison.Ordinal)
            && source.Contains("InvalidateMeasure()", StringComparison.Ordinal)
            && source.Contains("InvalidateArrange()", StringComparison.Ordinal),
            "LauncherGridPanel must listen for folder size/span changes so folder tiles resize immediately.");
    }

    [TestMethod]
    public void AllGroupsCreateFolder_PreservesVisualDropOrderBeforeRebuild()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();
        // Verify the all-groups create-folder path exists and uses the typed command
        Assert.IsTrue(
            source.Contains("TryExecuteAllGroupsFolderMutation", StringComparison.Ordinal)
            && source.Contains("RebuildShortcutTiles();", StringComparison.Ordinal),
            "Creating a folder in the all-groups view must place the new folder into the persisted visual order before rebuilding tiles.");
        // The Create kind is now resolved inside the business controller
        Assert.IsTrue(
            controllerSource.Contains("LauncherAllGroupsFolderMutationKind.Create", StringComparison.Ordinal),
            "Business controller must route all-groups folder creation through the typed mutation command.");
    }

    [TestMethod]
    public void AllGroupsCreateFolder_UsesAllGroupsOwnedFolderContainer()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();

        // Verify TryExecuteAllGroupsFolderMutation is delegated to the business controller
        Assert.IsTrue(
            source.Contains("_dragBusiness.TryExecuteAllGroupsFolderMutation", StringComparison.Ordinal),
            "RightCard must delegate all-groups folder mutations through _dragBusiness.");
        // Verify the business controller has the TryCommitFolderDrop method
        Assert.IsTrue(
            controllerSource.Contains("public bool TryCommitFolderDrop(", StringComparison.Ordinal),
            "Business controller must expose TryCommitFolderDrop.");
        // The Create kind and command type are now used inside the business controller
        Assert.IsTrue(
            controllerSource.Contains("LauncherAllGroupsFolderMutationKind.Create", StringComparison.Ordinal)
            && controllerSource.Contains("LauncherAllGroupsFolderMutationCommand", StringComparison.Ordinal),
            "Folders created from the all-groups page must use the typed owned-folder command inside the business controller.");
    }

    [TestMethod]
    public void AllGroupsCreateFolder_DoesNotRequireNormalGroupSourceBeforeOwnedCommit()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();
        // Verify the all-groups path exists and the delegate is in place
        Assert.IsTrue(
            source.Contains("showingAllGroups", StringComparison.Ordinal)
            && source.Contains("TryExecuteAllGroupsFolderMutation", StringComparison.Ordinal),
            "All-groups owned folder creation must run before requiring the dragged icon to belong to a normal shortcut group.");
        // TryResolveItemCollection and the Create kind are now routed through the business controller
        Assert.IsTrue(
            controllerSource.Contains("TryResolveItemCollection", StringComparison.Ordinal)
            && controllerSource.Contains("LauncherAllGroupsFolderMutationKind.Create", StringComparison.Ordinal),
            "Business controller must handle the all-groups Create mutation and resolve item collections.");
    }

    [TestMethod]
    public void AllGroupsFolderMoveOut_UsesOwnedContainerBeforeLauncherCommand()
    {
        string source = ReadRightCardSource();
        Assert.IsTrue(
            source.Contains("TryMoveOutOfAllGroupsFolder", StringComparison.Ordinal)
            && source.Contains("_launcherContextMenu.CreateMoveOutOfFolderCommand", StringComparison.Ordinal),
            "Dragging out of an all-groups-owned folder must use its owned container before falling back to the stable-ID Launcher command.");

        int ownedMoveOutMethodIndex = source.IndexOf("private bool TryMoveOutOfAllGroupsFolder", StringComparison.Ordinal);
        int ownedMoveOutMethodEnd = source.IndexOf("private void MoveDraggedIconToResolvedFlatTarget", ownedMoveOutMethodIndex, StringComparison.Ordinal);
        string ownedMoveOutBody = source[ownedMoveOutMethodIndex..ownedMoveOutMethodEnd];
        Assert.IsTrue(
            ownedMoveOutBody.Contains("LauncherAllGroupsFolderMutationKind.MoveOut", StringComparison.Ordinal)
            && !ownedMoveOutBody.Contains("AllGroupsShortcutViewService.TryMoveOutOfOwnedFolder", StringComparison.Ordinal),
            "All-groups move-out must submit a stable-ID command instead of mutating the owned container in the page.");

        int syncIndex = source.IndexOf("private void SyncFolderOverlayAfterFolderMutation()", StringComparison.Ordinal);
        int removeIndex = source.IndexOf("RemoveIcon(object sender, RoutedEventArgs e)", syncIndex, StringComparison.Ordinal);
        Assert.IsTrue(syncIndex >= 0 && removeIndex > syncIndex, "Could not locate SyncFolderOverlayAfterFolderMutation body.");
        string syncBody = source[syncIndex..removeIndex];
        Assert.IsTrue(
            syncBody.Contains("AllGroupsShortcutViewService.IsAllGroupsRootItem", StringComparison.Ordinal),
            "Folder overlay refresh must keep all-groups-owned folders open instead of closing them as missing from the normal menu tree.");
    }

    [TestMethod]
    public void FolderOverlayDragStarting_CreatesOverlayPreviewForMoveOutHitTesting()
    {
        string source = ReadRightCardSource();
        int methodIndex = source.IndexOf("private void FolderItemsListView_DragItemsStarting", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("private void FolderItemsListView_DragItemsCompleted", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate FolderItemsListView_DragItemsStarting body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("BeginDragPointerTracking(registerSystemDragSuppressHandlers: true)", StringComparison.Ordinal)
            && body.Contains("StartIconDragPreviewAsync(previewRequest)", StringComparison.Ordinal),
            "Dragging out of a folder relies on overlay pointer coordinates, so folder-item drags must create the same overlay preview as launcher drags.");

        int wrapperIndex = source.IndexOf("private System.Threading.Tasks.Task StartIconDragPreviewAsync", StringComparison.Ordinal);
        int wrapperEndIndex = source.IndexOf("private void BeginDragPointerTracking", wrapperIndex, StringComparison.Ordinal);
        Assert.IsTrue(wrapperIndex >= 0 && wrapperEndIndex > wrapperIndex, "Could not locate StartIconDragPreviewAsync body.");
        string wrapperBody = source[wrapperIndex..wrapperEndIndex];
        Assert.IsTrue(
            wrapperBody.Contains("_dragPreviewController.BeginAsync", StringComparison.Ordinal),
            "The shared preview wrapper must begin the overlay preview through the drag preview controller.");
    }

    [TestMethod]
    public void FolderOverlayDragCompleted_EndsOverlayPreviewAndTracking()
    {
        string source = ReadRightCardSource();
        int methodIndex = source.IndexOf("private void FolderItemsListView_DragItemsCompleted", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("private void FolderOverlay_DragOver", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate FolderItemsListView_DragItemsCompleted body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("CompleteFolderOverlayItemDrag()", StringComparison.Ordinal),
            "Folder item drag completion must end the overlay preview and pointer tracking; otherwise the preview can remain stuck to the mouse.");

        int cleanupIndex = source.IndexOf("private void CompleteFolderOverlayItemDrag()", StringComparison.Ordinal);
        int cleanupEndIndex = source.IndexOf("private void FolderOverlay_DragOver", cleanupIndex, StringComparison.Ordinal);
        Assert.IsTrue(cleanupIndex >= 0 && cleanupEndIndex > cleanupIndex, "Could not locate CompleteFolderOverlayItemDrag body.");

        string cleanupBody = source[cleanupIndex..cleanupEndIndex];
        Assert.IsTrue(
            cleanupBody.Contains("EndDragPointerTracking()", StringComparison.Ordinal)
            && cleanupBody.Contains("_dragPreviewController.End(animate: false)", StringComparison.Ordinal)
            && cleanupBody.Contains("_isCustomDragActive = false", StringComparison.Ordinal),
            "Folder overlay drag cleanup must fully stop the custom drag preview lifecycle.");
    }

    [TestMethod]
    public void GroupTabDrop_DoesNotBypassDragCompletedCleanup()
    {
        string source = ReadRightCardSource();
        int methodIndex = source.IndexOf("private void GroupListView_IconDrop(object sender, DragEventArgs e)", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("private MenuInfo? FindGroupContainingIcon", methodIndex, StringComparison.Ordinal);

        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate GroupListView_IconDrop body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsFalse(
            body.Contains("_dragPreviewController.End", StringComparison.Ordinal)
            || body.Contains("_isCustomDragActive = false", StringComparison.Ordinal),
            "Group tab drop must not end the custom drag directly; DragItemsCompleted owns pointer, preview, and container cleanup.");
        Assert.IsTrue(
            body.Contains("TryCommitCrossGroupDrop(", StringComparison.Ordinal),
            "Group tab drop should delegate to the single cross-group commit path via the page wrapper.");

        // The page-level wrapper must forward to the business controller's shared cross-group commit path.
        int pageWrapperIndex = source.IndexOf("private bool TryCommitCrossGroupDrop(", StringComparison.Ordinal);
        int pageWrapperEnd = source.IndexOf("private void TryUpdateBatchPlaceholderBlockFromPointer", pageWrapperIndex, StringComparison.Ordinal);
        Assert.IsTrue(pageWrapperIndex >= 0 && pageWrapperEnd > pageWrapperIndex, "Could not locate the page-level TryCommitCrossGroupDrop wrapper.");
        string pageWrapperBody = source[pageWrapperIndex..pageWrapperEnd];
        Assert.IsTrue(
            pageWrapperBody.Contains("_dragBusiness.TryCommitCrossGroupDrop(icons, targetMenu, batchFinalizeReason, out wasAllGroups)", StringComparison.Ordinal),
            "Group tab drop must be forwarded through _dragBusiness so pointer, preview, and container cleanup stay unified in DragItemsCompleted.");

        // The business controller owns the cross-group commit logic now.
        // Verify it does NOT end drag lifecycle directly in its TryCommitCrossGroupDrop method.
        string controllerSource = ReadDragBusinessControllerSource();
        int controllerCommitIndex = controllerSource.IndexOf("public bool TryCommitCrossGroupDrop(", StringComparison.Ordinal);
        if (controllerCommitIndex >= 0)
        {
            int controllerCommitEnd = controllerSource.IndexOf("public bool TryCommitCrossGroupDropAtDragEnd", controllerCommitIndex, StringComparison.Ordinal);
            if (controllerCommitEnd > controllerCommitIndex)
            {
                string controllerCommitBody = controllerSource[controllerCommitIndex..controllerCommitEnd];
                Assert.IsFalse(
                    controllerCommitBody.Contains("_dragPreviewController.End", StringComparison.Ordinal)
                    || controllerCommitBody.Contains("_isCustomDragActive = false", StringComparison.Ordinal),
                    "The shared cross-group commit path may mark the commit but must leave drag lifecycle cleanup to DragItemsCompleted.");
            }
        }
        Assert.IsTrue(
            controllerSource.Contains("_dragState.IsCrossGroupDrag = true", StringComparison.Ordinal)
            || controllerSource.Contains("_isCrossGroupDrag = true", StringComparison.Ordinal)
            || controllerSource.Contains("IsCrossGroupDrag = true", StringComparison.Ordinal),
            "The business controller must mark cross-group drag state internally.");
    }

    [TestMethod]
    public void SingleIconDrag_MovesTilesDuringLiveReorderAndUsesOverlayPreview()
    {
        string source = ReadRightCardSource();
        string previewStateSource = ReadSingleIconDragPreviewStateSource();
        string controllerSource = ReadDragBusinessControllerSource();

        // Verify TryUpdateSingleIconDropIndexFromPointer exists and uses stabilization
        Assert.IsTrue(
            source.Contains("private void TryUpdateSingleIconDropIndexFromPointer()", StringComparison.Ordinal),
            "Could not locate single-icon drag update method.");
        Assert.IsTrue(
            source.Contains("TryStabilizeSingleIconReorderTarget(targetIndex)", StringComparison.Ordinal),
            "Pointer-move drag callbacks must delegate placeholder/yield decisions to the container preview controller.");
        Assert.IsFalse(
            source.Contains("QueueSingleIconLiveReorder(", StringComparison.Ordinal),
            "Pointer hit-testing should only resolve a target index; debounce, clearing, and yield animation belong to the centralized preview controller.");
        // Verify shortcutTiles.Move is not called inside TryUpdateSingleIconDropIndexFromPointer body
        // (it's legitimate in drag completion paths, so we check the method body specifically)
        int methodStart = source.IndexOf("private void TryUpdateSingleIconDropIndexFromPointer()", StringComparison.Ordinal);
        int methodEnd = source.IndexOf("private bool TryResolveLauncherDragPreviewFromPointer", StringComparison.Ordinal);
        if (methodEnd < 0) methodEnd = source.IndexOf("private IconInfo? ResolveFolderDropTargetFromPointer", StringComparison.Ordinal);
        if (methodStart >= 0 && methodEnd > methodStart)
        {
            string methodBody = source[methodStart..methodEnd];
            Assert.IsFalse(
                methodBody.Contains("shortcutTiles.Move(", StringComparison.Ordinal),
                "Pointer-move drag callbacks must not synchronously mutate shortcutTiles; WinUI can crash in native collection-change dispatch.");
        }

        // Verify the inlined reorder preview logic exists
        Assert.IsTrue(
            source.Contains("_singleIconDragPreview.CommitTarget(targetIndex)", StringComparison.Ordinal)
            && source.Contains("BeginShortcutItemPreviewOffset", StringComparison.Ordinal),
            "The container preview controller must own the single global insert target, debounce gate, and yield animation lifecycle.");
        Assert.IsTrue(
            source.Contains("_dragPreviewController.BeginAsync", StringComparison.Ordinal),
            "Single icon drag must create an overlay preview so the original container can be hidden.");
        Assert.IsTrue(
            source.Contains("_dragBusiness.ApplyDraggedItemContainerState", StringComparison.Ordinal),
            "Single icon drag should hide the original container while preserving its layout slot.");
        Assert.IsTrue(
            source.Contains("SingleIconReorderActivationDelayMs = 150", StringComparison.Ordinal)
            && previewStateSource.Contains("PendingSinceTick", StringComparison.Ordinal),
            "Single icon reorder preview must require a stable insert target before moving neighbors.");
        // TryActivateTarget is now called inside the business controller
        Assert.IsTrue(
            controllerSource.Contains("_singleIconDragPreview.TryActivateTarget", StringComparison.Ordinal),
            "Business controller must own the single icon reorder activation gate.");
    }

    [TestMethod]
    public void SingleIconLiveYieldPreview_UsesVirtualLayoutWithoutMutatingTiles()
    {
        string source = ReadRightCardSource();
        // Verify the inline yield preview logic exists
        Assert.IsTrue(
            source.Contains("LauncherGridLayoutEngine.Arrange", StringComparison.Ordinal)
            && source.Contains("BeginShortcutItemPreviewOffset", StringComparison.Ordinal),
            "Single icon yield preview must compute virtual grid positions and animate affected neighbors.");
        Assert.IsTrue(
            CountOccurrences(source, "LauncherGridLayoutEngine.Arrange") >= 2,
            "Single icon yield preview must calculate both current and virtual layout from data, not from transformed visuals.");
        // BuildCurrentLauncherLayoutMap was migrated to the business controller
        string controllerSource = ReadDragBusinessControllerSource();
        Assert.IsTrue(
            controllerSource.Contains("BuildCurrentLauncherLayoutMap", StringComparison.Ordinal),
            "Business controller must own the layout map building for hit-testing.");
        Assert.IsFalse(
            source.Contains("CountDataShortcutItemsBeforeIndex", StringComparison.Ordinal),
            "ResolveGridReorderIndex already returns the insertion index after excluding the dragged icon; converting it again shifts the preview one slot off.");
        Assert.IsTrue(
            source.Contains("vt.Insert(Math.Clamp(sti, 0, vt.Count), _draggedIcon)", StringComparison.Ordinal),
            "Single icon yield preview must insert the dragged icon at the resolved excluded-list target index.");
        // The yield preview code uses vt (virtual tiles), not shortcutTiles directly.
        // shortcutTiles.Move exists in drag-completion paths, which is correct.
        Assert.IsTrue(
            source.Contains("vt.RemoveAt(vci)", StringComparison.Ordinal)
            && source.Contains("vt.Insert(Math.Clamp(sti", StringComparison.Ordinal),
            "Single icon yield preview must operate on virtual tiles, not shortcutTiles.");
    }

    [TestMethod]
    public void SingleIconDropPosition_AppliesBeforeSyncInSingleGroups()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();
        Assert.IsTrue(
            source.Contains("_dragBusiness.CompleteDragReorder", StringComparison.Ordinal),
            "Drop completion must delegate to the business controller for tile move and sync.");
        Assert.IsTrue(
            controllerSource.Contains("ShortcutTiles.Move(curIdx, tgtIdx)", StringComparison.Ordinal)
            && controllerSource.Contains("_singleIconDragPreview.TargetIndex", StringComparison.Ordinal),
            "Business controller must move the visual tile order before syncing it back to the selected group source collection.");
    }

    [TestMethod]
    public void DragHitTesting_UsesStableLogicalGridInsteadOfAnimatedVisualPositions()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();
        string adapterSource = ReadDropHitTestAdapterSource();

        // BuildCurrentLauncherLayoutMap was migrated to the business controller
        Assert.IsTrue(
            controllerSource.Contains("BuildCurrentLauncherLayoutMap", StringComparison.Ordinal)
            && controllerSource.Contains("LauncherDropHitTestAdapter.BuildLogicalLayout", StringComparison.Ordinal),
            "Business controller must delegate WinUI coordinate adaptation to the Launcher hit-test adapter.");

        int adapterMethodIndex = adapterSource.IndexOf(
            "public static Dictionary<IconInfo, LauncherGridRect> BuildLogicalLayout",
            StringComparison.Ordinal);
        int adapterMethodEnd = adapterSource.IndexOf("public static bool TryTranslatePoint", adapterMethodIndex, StringComparison.Ordinal);
        Assert.IsTrue(adapterMethodIndex >= 0 && adapterMethodEnd > adapterMethodIndex, "Could not locate BuildLogicalLayout adapter body.");
        string adapterBody = adapterSource[adapterMethodIndex..adapterMethodEnd];
        Assert.IsTrue(
            adapterBody.Contains("LauncherGridLayoutEngine.Arrange", StringComparison.Ordinal),
            "Drag hit-testing must use the stable logical launcher grid; animated render transforms move visual bounds and make border positions flicker.");
        Assert.IsTrue(
            adapterBody.Contains("GetPanelOrigin(listView)", StringComparison.Ordinal)
            && adapterSource.Contains("panel.TransformToVisual(listView)", StringComparison.Ordinal),
            "Logical panel bounds must be translated into ListView coordinates so scrolling does not offset hit-testing.");
        Assert.IsFalse(
            adapterBody.Contains("item.TransformToVisual(listView)", StringComparison.Ordinal),
            "Drag hit-testing must not read animated ListViewItem visual positions while live-yield transforms are active.");
    }

    [TestMethod]
    public void DragHitTesting_UsesTheSameManualPositionModeAsTheVisiblePanel()
    {
        string adapterSource = ReadDropHitTestAdapterSource();
        string controllerSource = ReadDragBusinessControllerSource();

        Assert.IsTrue(
            adapterSource.Contains("launcherPanel.UseManualPositions", StringComparison.Ordinal)
            && adapterSource.Contains("preserveManualPositions", StringComparison.Ordinal),
            "Pointer hit-testing must follow the visible panel's position mode so All Groups drops do not use source-group coordinates.");
        Assert.IsTrue(
            controllerSource.Contains("_cachedLayoutUsesManualPositions == usesManualPositions", StringComparison.Ordinal),
            "Switching between All Groups and a normal group must invalidate incompatible cached hit-test geometry.");
    }

    [TestMethod]
    public void DragMotion_DoesNotRecomputeStationaryLayoutOrAnimateTheWholeGrid()
    {
        string source = ReadRightCardSource();
        string controllerSource = ReadDragBusinessControllerSource();
        string orchestratorSource = ReadLauncherDragOrchestratorSource();

        Assert.IsTrue(
            orchestratorSource.Contains("if (_pointerUpdatePending)", StringComparison.Ordinal)
            && orchestratorSource.Contains("_pointerUpdatePending = false", StringComparison.Ordinal),
            "The 60 fps preview timer must not repeat hit-testing and layout work while the pointer is stationary.");
        Assert.IsTrue(
            source.Contains("MaxLiveReorderAnimatedItems = 12", StringComparison.Ordinal)
            && controllerSource.Contains("MaxLiveReorderAnimatedItems = 12", StringComparison.Ordinal),
            "A long drag must not create storyboards for every launcher item in the grid.");
    }

    [TestMethod]
    public void FolderDropIntent_ActivatesOnlyAfterHoverDelay()
    {
        string source = ReadRightCardSource();
        string intentStateSource = ReadFolderDropIntentStateSource();
        int methodIndex = source.IndexOf("private bool TryUpdateFolderDropIntentFromPointer()", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("private IconInfo? ResolveFolderDropTargetFromPointer", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate folder drop intent update path.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("_dragBusiness.UpdateFolderDropIntentActivation(", StringComparison.Ordinal)
            && body.Contains("_dragBusiness.ResolveFolderDropTargetFromPointer(allowCachedPointer: false)", StringComparison.Ordinal),
            "Folder/create-folder drop intent must pass through the hover delay gate instead of activating immediately.");
        Assert.IsTrue(
            body.Contains("_dragBusiness.ResolveFolderDropTargetFromPointer(allowCachedPointer: false)", StringComparison.Ordinal),
            "Folder/create-folder hover activation must use the current pointer position, not cached coordinates from a previous target.");
        Assert.IsTrue(
            source.Contains("FolderDropHoverActivationDelayMs = 300", StringComparison.Ordinal)
            && intentStateSource.Contains("PendingSinceTick", StringComparison.Ordinal),
            "Folder/create-folder animation should require a stable 300ms hover before activation.");
        // _folderDropIntent.EvaluateCandidate is now called inside the business controller
        string controllerSource = ReadDragBusinessControllerSource();
        Assert.IsTrue(
            controllerSource.Contains("_folderDropIntent.EvaluateCandidate", StringComparison.Ordinal),
            "Business controller must own the folder drop intent evaluation gate.");
        Assert.IsTrue(
            source.Contains("_dragBusiness.CompleteDragReorder", StringComparison.Ordinal),
            "Drag cleanup must delegate to the business controller for reorder completion.");
        Assert.IsTrue(
            source.Contains("ResolveActivatedFolderDropTargetAtDragEnd()", StringComparison.Ordinal),
            "Dropping before the 300ms hover activation must not create or join a folder.");
    }

    [TestMethod]
    public void FolderDropIntent_DoesNotFallbackToAnimatedVisualHitTestAfterStablePreviewResolves()
    {
        string source = ReadRightCardSource();
        int methodIndex = source.IndexOf(
            "private IconInfo? ResolveFolderDropTargetFromPointer",
            StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("private void TryUpdateSingleIconDropIndexFromPointer", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate ResolveFolderDropTargetFromPointer body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("bool previewResolved = TryResolveLauncherDragPreviewFromPointer", StringComparison.Ordinal)
            && body.Contains("if (previewResolved)", StringComparison.Ordinal)
            && body.Contains("return null;", StringComparison.Ordinal),
            "Once stable logical-grid hit-testing resolves the pointer, folder hover must not fall back to animated visual bounds that can resurrect stale targets.");
    }

    [TestMethod]
    public void FolderDropIntent_DoesNotMoveRealTileOrderDuringHoverPreview()
    {
        string source = ReadRightCardSource();
        // SetFolderDropIntent 已迁移到 _dragBusiness，验证直接调用存在
        Assert.IsTrue(
            source.Contains("_dragBusiness.SetFolderDropIntent", StringComparison.Ordinal),
            "RightCard 应通过 _dragBusiness.SetFolderDropIntent 处理文件夹拖放意图。");
        // Verify the hover-path methods do not move tiles directly
        int hoverIndex = source.IndexOf("private bool TryUpdateFolderDropIntentFromPointer()", StringComparison.Ordinal);
        int hoverEnd = source.IndexOf("private IconInfo? ResolveFolderDropTargetFromPointer", hoverIndex, StringComparison.Ordinal);
        Assert.IsTrue(hoverIndex >= 0 && hoverEnd > hoverIndex, "Could not locate hover intent path.");
        string hoverBody = source[hoverIndex..hoverEnd];
        Assert.IsFalse(
            hoverBody.Contains("shortcutTiles.Move(", StringComparison.Ordinal),
            "Folder hover preview must not move the real tile collection directly; doing so makes the hidden source container flash beside the overlay.");
        // Business controller must handle intent evaluation internally
        string controllerSource = ReadDragBusinessControllerSource();
        Assert.IsTrue(
            controllerSource.Contains("_folderDropIntent.EvaluateCandidate", StringComparison.Ordinal),
            "Business controller must own the folder drop intent evaluation gate.");
    }

    [TestMethod]
    public void LaunchableShortcutTemplate_KeepsFortyEightPixelIconFrameForDragCapture()
    {
        string xaml = ReadRightCardXaml();

        Assert.IsTrue(
            xaml.Contains("x:Name=\"ShortcutIconFrame\"", StringComparison.Ordinal)
            && xaml.Contains("Width=\"48\"", StringComparison.Ordinal)
            && xaml.Contains("Height=\"48\"", StringComparison.Ordinal),
            "Drag preview capture depends on a stable 48x48 icon frame instead of the full launcher slot.");
    }

    [TestMethod]
    public void FolderDragPreview_CapturesFolderIconBodyInsteadOfWholeTemplateSlot()
    {
        string helperSource = ReadVisualTreeHelperSource();
        int methodIndex = helperSource.IndexOf("FindShortcutDragCaptureTarget", StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0, "Could not locate FindShortcutDragCaptureTarget in VisualTreeHelper.");

        string body = helperSource[methodIndex..];
        Assert.IsTrue(
            body.Contains("FolderVisualRoot", StringComparison.Ordinal),
            "Folder drag previews must capture the 64px FolderVisualRoot, not the whole template slot.");
        Assert.IsTrue(
            body.IndexOf("FolderVisualRoot", StringComparison.Ordinal)
            < body.LastIndexOf("return container.ContentTemplateRoot", StringComparison.Ordinal),
            "FolderVisualRoot must be tried before falling back to the full template root.");

        string source = ReadRightCardSource();
        // StartIconDragPreviewAsync was removed; the preview is now initiated via _dragPreviewController.BeginAsync
        Assert.IsTrue(
            source.Contains("_dragPreviewController.BeginAsync", StringComparison.Ordinal),
            "Folder drag preview must be initiated through the drag preview controller's BeginAsync method.");
        Assert.IsFalse(
            source.Contains("Math.Max(request.CaptureTarget.ActualWidth", StringComparison.Ordinal)
            || source.Contains("Math.Max(request.CaptureTarget.ActualHeight", StringComparison.Ordinal),
            "Drag overlay size must use the prepared 64px capture request; resizing from ActualWidth can reintroduce white slot captures.");
    }

    [TestMethod]
    public void LauncherShell_UsesTranslucentLauncherResources()
    {
        string xaml = ReadRightCardXaml();

        Assert.IsTrue(xaml.Contains("x:Name=\"WrapCard\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Background=\"{ThemeResource LauncherShellBackgroundBrush}\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("BorderBrush=\"{ThemeResource LauncherShellBorderBrush}\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("CornerRadius=\"28\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("<Grid Padding=\"30,12,36,26\">", StringComparison.Ordinal));
    }

    [TestMethod]
    public void FolderOverlay_IsEmbeddedLauncherPanelWithoutDarkBackdrop()
    {
        string xaml = ReadRightCardXaml();

        Assert.IsFalse(
            xaml.Contains("Background=\"#66000000\"", StringComparison.Ordinal),
            "Folder open state should no longer dim the launcher with a modal black backdrop.");
        Assert.IsTrue(xaml.Contains("Background=\"Transparent\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Background=\"{ThemeResource LauncherFolderPanelBackgroundBrush}\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("BorderBrush=\"{ThemeResource LauncherMutedBorderBrush}\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("CornerRadius=\"28\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ShortcutIcons_UseSixtyFourPixelLauncherSurfaceAroundStableIconFrame()
    {
        string xaml = ReadRightCardXaml();

        Assert.IsTrue(xaml.Contains("x:Name=\"ShortcutIconSurface\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Width=\"64\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Height=\"64\"", StringComparison.Ordinal));
        Assert.IsTrue(xaml.Contains("Background=\"{ThemeResource LauncherIconSurfaceBrush}\"", StringComparison.Ordinal));
    }

    private static string ReadRightCardXaml()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Control",
                "UserControls",
                "PannelCard",
                "RightCardControl.xaml");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate RightCardControl.xaml from the test output directory.");
        return string.Empty;
    }

    private static string ReadLauncherGridPanelSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherGridPanel.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherGridPanel.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadShortcutLayoutCalculatorSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Application",
                "ShortcutLayoutCalculator.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate ShortcutLayoutCalculator.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadBatchPlaceholderCoordinatorSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "LauncherBatchPlaceholderCoordinator.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherBatchPlaceholderCoordinator.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadDropHitTestAdapterSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherDropHitTestAdapter.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherDropHitTestAdapter.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadSingleIconDragPreviewStateSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "LauncherSingleIconDragPreviewState.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherSingleIconDragPreviewState.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadFolderDropIntentStateSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "LauncherFolderDropIntentState.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherFolderDropIntentState.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadRightCardSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Control",
                "UserControls",
                "PannelCard",
                "RightCardControl.xaml.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate RightCardControl.xaml.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadFolderTileVisualAdapterSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherFolderTileVisualAdapter.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherFolderTileVisualAdapter.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadShortcutContainerStateControllerSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherShortcutContainerStateController.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherShortcutContainerStateController.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadIconInfoSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "ViewModel",
                "IconInfo.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate IconInfo.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadLayoutApplyControllerSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherShortcutLayoutApplyController.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherShortcutLayoutApplyController.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadVisualTreeHelperSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherVisualTreeHelper.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherVisualTreeHelper.cs from the test output directory.");
        return string.Empty;
    }

    private static int CountLauncherPanelTags(string value, bool requireTopAlignment)
    {
        int count = 0;
        foreach (string tagStart in new[] { "<ItemsWrapGrid", "<launcher:LauncherGridPanel" })
        {
            int index = 0;
            while ((index = value.IndexOf(tagStart, index, StringComparison.Ordinal)) >= 0)
            {
                int tagEnd = value.IndexOf('>', index);
                if (tagEnd < 0)
                {
                    break;
                }

                string tag = value[index..tagEnd];
                bool isLauncherPanel = tagStart.Contains("LauncherGridPanel", StringComparison.Ordinal)
                    || tag.Contains("HorizontalAlignment=\"Stretch\"", StringComparison.Ordinal);
                if (isLauncherPanel
                    && (!requireTopAlignment || tag.Contains("VerticalAlignment=\"Top\"", StringComparison.Ordinal)))
                {
                    count++;
                }

                index = tagEnd + 1;
            }
        }

        return count;
    }

    private static int CountOccurrences(string value, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static string ReadDragBusinessControllerSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherDragBusinessController.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherDragBusinessController.cs from the test output directory.");
        return string.Empty;
    }

    private static string ReadLauncherDragOrchestratorSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(
                directory,
                "raindesk",
                "Features",
                "Launcher",
                "Presentation",
                "WinUI",
                "LauncherDragOrchestrator.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate LauncherDragOrchestrator.cs from the test output directory.");
        return string.Empty;
    }
}
