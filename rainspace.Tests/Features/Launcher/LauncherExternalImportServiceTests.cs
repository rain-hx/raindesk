using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Launcher.Infrastructure;
using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherExternalImportServiceTests
{
    [TestMethod]
    public async global::System.Threading.Tasks.Task PartialResolution_BatchesSuccessfulDraftsOnceAndReportsErrors()
    {
        var source = new StubDraftSource(path => path == "bad"
            ? LauncherShortcutDraftResolution.Failed(path, "unsupported")
            : new LauncherShortcutDraftResolution(path, new LauncherShortcutDraft(path)));
        var handler = new RecordingHandler();
        var service = new LauncherExternalImportService(source, handler);

        LauncherExternalImportResult result = await service.ImportAsync(
            "group",
            new[] { "one", "bad", "two" });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.AddedCount);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual("bad", result.Errors[0].SourcePath);
        Assert.AreEqual(1, handler.AddManyCount);
        Assert.AreEqual("group", handler.Batch!.TargetGroupId);
        Assert.IsTrue(handler.Batch.ApplyConfiguredSort);
        CollectionAssert.AreEqual(
            new[] { "one", "two" },
            handler.Batch.Shortcuts.Select(draft => draft.Name).ToArray());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task NoResolvedDrafts_DoesNotEnterWriteBoundary()
    {
        var source = new StubDraftSource(path =>
            LauncherShortcutDraftResolution.Failed(path, "bad"));
        var handler = new RecordingHandler();
        var service = new LauncherExternalImportService(source, handler);

        LauncherExternalImportResult result = await service.ImportAsync(
            "group",
            new[] { "one", "two" });

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.AddedCount);
        Assert.AreEqual(2, result.Errors.Count);
        Assert.AreEqual(0, handler.AddManyCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Coordinator_CancelsPreviousBatchAndSuppressesItsLateResult()
    {
        var service = new DelayedImportService();
        using var coordinator = new LauncherExternalImportCoordinator(service);

        global::System.Threading.Tasks.Task<LauncherExternalImportResult?> first = coordinator.ImportAsync("group", new[] { "slow" });
        global::System.Threading.Tasks.Task<LauncherExternalImportResult?> second = coordinator.ImportAsync("group", new[] { "fast" });
        LauncherExternalImportResult? secondResult = await second;
        LauncherExternalImportResult? firstResult = await first;

        Assert.IsNull(firstResult);
        Assert.IsNotNull(secondResult);
        Assert.AreEqual(1, secondResult.AddedCount);
        Assert.AreEqual(1, service.CancellationCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task WindowsSource_RejectsMissingFileWithoutCreatingFallbackShortcut()
    {
        var source = new WindowsLauncherShortcutDraftSource();

        LauncherShortcutDraftResolution result = await source.ResolveAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.exe"));

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Draft);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Error));
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task WindowsSource_MapsExistingFileToPureDraftWithPersistentImageBytes()
    {
        string path = Path.GetTempFileName();
        try
        {
            var source = new WindowsLauncherShortcutDraftSource();

            LauncherShortcutDraftResolution result = await source.ResolveAsync(path);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(path), result.Draft!.Name);
            Assert.AreEqual(path, result.Draft.Path);
            Assert.IsTrue(result.Draft.ImageBytes is { Length: > 0 });
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class StubDraftSource : ILauncherShortcutDraftSource
    {
        private readonly Func<string, LauncherShortcutDraftResolution> _resolve;

        public StubDraftSource(Func<string, LauncherShortcutDraftResolution> resolve)
        {
            _resolve = resolve;
        }

        public global::System.Threading.Tasks.Task<LauncherShortcutDraftResolution> ResolveAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return global::System.Threading.Tasks.Task.FromResult(_resolve(path));
        }
    }

    private sealed class RecordingHandler : ILauncherCommandHandler
    {
        public int AddManyCount { get; private set; }
        public AddLauncherShortcutsCommand? Batch { get; private set; }

        public LauncherCommandResult AddMany(AddLauncherShortcutsCommand command)
        {
            AddManyCount++;
            Batch = command;
            return new LauncherCommandResult(true, true);
        }

        public LauncherCommandResult Execute(LauncherMutationCommand command) => throw new NotSupportedException();
        public LauncherCommandResult Add(AddLauncherShortcutCommand command) => throw new NotSupportedException();
        public LauncherCommandResult Rename(RenameLauncherShortcutCommand command) => throw new NotSupportedException();
        public LauncherCommandResult Update(UpdateLauncherShortcutCommand command) => throw new NotSupportedException();
        public LauncherCommandResult UpdateGridLayout(UpdateLauncherGridLayoutCommand command) => throw new NotSupportedException();
        public LauncherCommandResult UpdateOrder(UpdateLauncherShortcutOrderCommand command) => throw new NotSupportedException();
        public LauncherCommandResult MutateAllGroupsFolder(LauncherAllGroupsFolderMutationCommand command) => throw new NotSupportedException();
    }

    private sealed class DelayedImportService : ILauncherExternalImportService
    {
        public int CancellationCount { get; private set; }

        public async global::System.Threading.Tasks.Task<LauncherExternalImportResult> ImportAsync(
            string targetGroupId,
            IReadOnlyList<string> paths,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await global::System.Threading.Tasks.Task.Delay(
                    paths[0] == "slow" ? 100 : 1,
                    cancellationToken);
                return new LauncherExternalImportResult(
                    true,
                    1,
                    Array.Empty<LauncherExternalImportError>());
            }
            catch (OperationCanceledException)
            {
                CancellationCount++;
                throw;
            }
        }
    }
}
