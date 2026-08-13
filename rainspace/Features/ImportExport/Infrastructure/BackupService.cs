using rainspace.Features.ImportExport.Contracts;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Services.Migrations;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Features.ImportExport.Infrastructure;

/// <summary>
/// JSON/旧备份适配器。一次导入在此完成解析、迁移、状态替换、原子写盘和回滚。
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IAppDataProvider _dataProvider;
    private readonly ISaveService _saveService;
    private readonly BackupDocumentReader _documentReader;
    private readonly string _primaryDataPath;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public BackupService(
        IAppDataProvider dataProvider,
        ISaveService saveService,
        BackupDocumentReader documentReader,
        string primaryDataPath)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        _documentReader = documentReader ?? throw new ArgumentNullException(nameof(documentReader));
        _primaryDataPath = string.IsNullOrWhiteSpace(primaryDataPath)
            ? throw new ArgumentException("主数据路径不能为空。", nameof(primaryDataPath))
            : primaryDataPath;
    }

    public async global::System.Threading.Tasks.Task<BackupOperationResult> ExportAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return BackupOperationResult.Failed(BackupErrorCode.Cancelled, "备份导出已取消。");
        }

        try
        {
            return await global::System.Threading.Tasks.Task.Run(
                () => Export(destinationPath, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async global::System.Threading.Tasks.Task<BackupOperationResult> ImportAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return BackupOperationResult.Failed(BackupErrorCode.Cancelled, "备份导入已取消。");
        }

        try
        {
            return await global::System.Threading.Tasks.Task.Run(
                () => Import(sourcePath, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private BackupOperationResult Export(string destinationPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return BackupOperationResult.Failed(BackupErrorCode.InvalidPath, "导出路径不能为空。");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppDataSnapshot snapshot = _dataProvider.GetSnapshot();
            cancellationToken.ThrowIfCancellationRequested();
            SafeFileWriter.WriteAllText(destinationPath, snapshot.Json);
            LogUtil.WriteLog("备份数据导出成功: " + destinationPath);
            return BackupOperationResult.Succeeded;
        }
        catch (OperationCanceledException)
        {
            return BackupOperationResult.Failed(BackupErrorCode.Cancelled, "备份导出已取消。");
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "导出备份数据失败");
            return BackupOperationResult.Failed(BackupErrorCode.WriteFailed, ex.Message);
        }
    }

    private BackupOperationResult Import(string sourcePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return BackupOperationResult.Failed(BackupErrorCode.InvalidPath, "导入路径不能为空。");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppData? importedData = _documentReader.Read(sourcePath);
            if (importedData == null)
            {
                return BackupOperationResult.Failed(
                    BackupErrorCode.ParseFailed,
                    "无法解析备份文件，文件可能已损坏或格式不受支持。");
            }

            AppDataMigrationResult migration = AppDataMigrationRunner.Default.MigrateToCurrent(importedData);
            if (!migration.Success)
            {
                BackupErrorCode code = importedData.SchemaVersion > AppDataSchema.CurrentVersion
                    ? BackupErrorCode.UnsupportedVersion
                    : BackupErrorCode.ValidationFailed;
                return BackupOperationResult.Failed(code, migration.Error ?? "备份数据迁移失败。");
            }

            AppData migratedData = migration.Data;
            ShortcutIdentityService.EnsureIconIds(migratedData);
            AllGroupsShortcutViewService.NormalizeOwnedFolderReferences(migratedData);
            NormalizeSelectedMenu(migratedData);
            cancellationToken.ThrowIfCancellationRequested();

            _saveService.CancelPendingSave();
            CommonCode.CancelDebouncedSave();

            AppData previousData = _dataProvider.Current;
            try
            {
                _dataProvider.Replace(migratedData);
                AppDataSnapshot snapshot = _dataProvider.GetSnapshot();
                SafeFileWriter.WriteAllText(_primaryDataPath, snapshot.Json);
                LogUtil.WriteLog(
                    $"备份数据导入成功: {sourcePath}, v{migration.OriginalVersion} -> v{migration.FinalVersion}");
                return BackupOperationResult.Succeeded;
            }
            catch (Exception ex)
            {
                _dataProvider.Replace(previousData);
                LogUtil.WriteErrorLog(ex, "写入导入数据失败，已恢复原状态");
                return BackupOperationResult.Failed(BackupErrorCode.WriteFailed, ex.Message);
            }
        }
        catch (OperationCanceledException)
        {
            return BackupOperationResult.Failed(BackupErrorCode.Cancelled, "备份导入已取消。");
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "导入备份数据失败");
            return BackupOperationResult.Failed(BackupErrorCode.ParseFailed, ex.Message);
        }
    }

    private static void NormalizeSelectedMenu(AppData data)
    {
        int count = data.MenuList.Count;
        if (count == 0)
        {
            // R6 已知例外：备份恢复后规范化，在 UI 初始化前执行，无法使用命令
            data.AppConfig.SelectedMenuIndex = -1;
            return;
        }

        if (data.AppConfig.SelectedMenuIndex < 0 || data.AppConfig.SelectedMenuIndex >= count)
        {
            // R6 已知例外：备份恢复后规范化，在 UI 初始化前执行，无法使用命令
            data.AppConfig.SelectedMenuIndex = 0;
        }
    }
}
