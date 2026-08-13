using Newtonsoft.Json;
using rainspace.ViewModel;

namespace rainspace.Services.Migrations;

internal static class AppDataSchema
{
    public const int LegacyVersion = 0;
    public const int CurrentVersion = 3;
}

internal interface IAppDataMigration
{
    int FromVersion { get; }

    int ToVersion { get; }

    void Apply(AppData data);
}

internal readonly record struct AppDataMigrationResult(
    bool Success,
    bool Changed,
    int OriginalVersion,
    int FinalVersion,
    AppData Data,
    string? Error = null);

internal sealed class AppDataMigrationRunner
{
    private readonly IReadOnlyDictionary<int, IAppDataMigration> _migrations;

    public AppDataMigrationRunner(IEnumerable<IAppDataMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        _migrations = migrations.ToDictionary(migration => migration.FromVersion);
    }

    public static AppDataMigrationRunner Default { get; } = new(
        new IAppDataMigration[]
        {
            new LegacyToVersionOneMigration(),
            new VersionOneToVersionTwoMigration(),
            new VersionTwoToVersionThreeMigration()
        });

    public AppDataMigrationResult MigrateToCurrent(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        int originalVersion = data.SchemaVersion;
        if (originalVersion < AppDataSchema.LegacyVersion)
        {
            return Failed(data, originalVersion, "数据版本不能小于 0。");
        }

        if (originalVersion > AppDataSchema.CurrentVersion)
        {
            return Failed(
                data,
                originalVersion,
                $"数据版本 {originalVersion} 高于当前应用支持的版本 {AppDataSchema.CurrentVersion}。");
        }

        AppData workingCopy;
        try
        {
            workingCopy = originalVersion == AppDataSchema.CurrentVersion
                ? data
                : CloneForMigration(data);
        }
        catch (Exception ex)
        {
            return Failed(data, originalVersion, $"创建数据迁移副本失败: {ex.Message}");
        }

        bool changed = false;
        try
        {
            while (workingCopy.SchemaVersion < AppDataSchema.CurrentVersion)
            {
                if (!_migrations.TryGetValue(workingCopy.SchemaVersion, out IAppDataMigration? migration))
                {
                    return Failed(
                        data,
                        originalVersion,
                        $"缺少从数据版本 {workingCopy.SchemaVersion} 开始的迁移步骤。",
                        changed,
                        workingCopy.SchemaVersion);
                }

                if (migration.ToVersion <= migration.FromVersion)
                {
                    return Failed(
                        data,
                        originalVersion,
                        $"迁移版本无效: {migration.FromVersion} -> {migration.ToVersion}。",
                        changed,
                        workingCopy.SchemaVersion);
                }

                migration.Apply(workingCopy);
                workingCopy.SchemaVersion = migration.ToVersion;
                changed = true;
            }
        }
        catch (Exception ex)
        {
            return Failed(
                data,
                originalVersion,
                $"数据迁移失败: {ex.Message}",
                changed,
                workingCopy.SchemaVersion);
        }

        AppDataValidationResult validation = AppDataValidator.Validate(workingCopy);
        if (!validation.Success)
        {
            return Failed(
                data,
                originalVersion,
                $"数据校验失败: {validation.Error}",
                changed,
                workingCopy.SchemaVersion);
        }

        return new AppDataMigrationResult(
            Success: true,
            Changed: changed,
            OriginalVersion: originalVersion,
            FinalVersion: workingCopy.SchemaVersion,
            Data: workingCopy);
    }

    private static AppDataMigrationResult Failed(
        AppData originalData,
        int originalVersion,
        string error,
        bool changed = false,
        int? finalVersion = null)
    {
        return new AppDataMigrationResult(
            Success: false,
            Changed: changed,
            OriginalVersion: originalVersion,
            FinalVersion: finalVersion ?? originalVersion,
            Data: originalData,
            Error: error);
    }

    private static AppData CloneForMigration(AppData data)
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        string json = JsonConvert.SerializeObject(data, settings);
        return JsonConvert.DeserializeObject<AppData>(json, settings)
            ?? throw new JsonSerializationException("数据副本反序列化结果为空。");
    }

    private sealed class LegacyToVersionOneMigration : IAppDataMigration
    {
        public int FromVersion => AppDataSchema.LegacyVersion;

        public int ToVersion => 1;

        public void Apply(AppData data)
        {
            if (!ToDoMigrationService.NeedsMigration(data))
            {
                return;
            }

            data.Tasks = ToDoMigrationService.Migrate(data);
            ToDoMigrationService.SyncBackToLegacy(data);
        }
    }

    private sealed class VersionOneToVersionTwoMigration : IAppDataMigration
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public void Apply(AppData data)
        {
            var menuIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (MenuInfo menu in data.MenuList)
            {
                if (string.IsNullOrWhiteSpace(menu.MenuId) || !menuIds.Add(menu.MenuId))
                {
                    string id;
                    do
                    {
                        id = Guid.NewGuid().ToString("N");
                    }
                    while (!menuIds.Add(id));

                    menu.MenuId = id;
                }
            }

            ShortcutIdentityService.EnsureIconIds(data);
        }
    }

    private sealed class VersionTwoToVersionThreeMigration : IAppDataMigration
    {
        public int FromVersion => 2;

        public int ToVersion => 3;

        public void Apply(AppData data)
        {
            var taskIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ToDoTask task in data.Tasks)
            {
                if (string.IsNullOrWhiteSpace(task.Id) || !taskIds.Add(task.Id))
                {
                    string id;
                    do
                    {
                        id = Guid.NewGuid().ToString("N");
                    }
                    while (!taskIds.Add(id));

                    task.Id = id;
                }
            }

            ToDoMigrationService.SyncBackToLegacy(data);
        }
    }
}

internal readonly record struct AppDataValidationResult(bool Success, string? Error = null);

internal static class AppDataValidator
{
    public static AppDataValidationResult Validate(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.SchemaVersion != AppDataSchema.CurrentVersion)
        {
            return Invalid($"数据版本应为 {AppDataSchema.CurrentVersion}，实际为 {data.SchemaVersion}。");
        }

        if (data.MenuList.Any(menu => menu is null))
        {
            return Invalid("菜单集合包含空元素。");
        }

        if (data.MenuList.Any(menu => string.IsNullOrWhiteSpace(menu.MenuId)))
        {
            return Invalid("菜单集合包含空 ID。");
        }

        if (HasDuplicateNonEmptyIds(data.MenuList.Select(menu => menu.MenuId)))
        {
            return Invalid("菜单集合包含重复 ID。");
        }

        if (data.MenuList.Any(menu => menu.IconList.Any(icon => icon is null)))
        {
            return Invalid("快捷方式集合包含空元素。");
        }

        List<IconInfo> icons = ShortcutFolderTreeService.EnumerateIcons(data.MenuList).ToList();
        if (icons.Any(icon => string.IsNullOrWhiteSpace(icon.IconId)))
        {
            return Invalid("快捷方式集合包含空 ID。");
        }

        if (HasDuplicateNonEmptyIds(icons.Select(icon => icon.IconId)))
        {
            return Invalid("快捷方式集合包含重复 ID。");
        }

        if (data.Tasks.Any(task => task is null))
        {
            return Invalid("任务集合包含空元素。");
        }

        if (data.Tasks.Any(task => string.IsNullOrWhiteSpace(task.Id)))
        {
            return Invalid("任务集合包含空 ID。");
        }

        if (HasDuplicateNonEmptyIds(data.Tasks.Select(task => task.Id)))
        {
            return Invalid("任务集合包含重复 ID。");
        }

        foreach (ToDoTask task in data.Tasks)
        {
            if (task.Subtasks.Any(subtask => subtask is null))
            {
                return Invalid($"任务 {task.Id} 的子任务集合包含空元素。");
            }

            if (HasDuplicateNonEmptyIds(task.Subtasks.Select(subtask => subtask.Id)))
            {
                return Invalid($"任务 {task.Id} 的子任务集合包含重复 ID。");
            }
        }

        if (data.Projects.Any(project => project is null))
        {
            return Invalid("项目集合包含空元素。");
        }

        if (HasDuplicateNonEmptyIds(data.Projects.Select(project => project.Id)))
        {
            return Invalid("项目集合包含重复 ID。");
        }

        if (data.ToDoList.Any(todo => todo is null) || data.HiToDoList.Any(todo => todo is null))
        {
            return Invalid("兼容待办集合包含空元素。");
        }

        IEnumerable<ToDoInfo> legacyTodos = data.ToDoList.Concat(data.HiToDoList);
        if (legacyTodos.Any(todo => string.IsNullOrWhiteSpace(todo.Id)))
        {
            return Invalid("兼容待办集合包含空 ID。");
        }

        if (HasDuplicateNonEmptyIds(legacyTodos.Select(todo => todo.Id)))
        {
            return Invalid("兼容待办集合包含重复 ID。");
        }

        return new AppDataValidationResult(Success: true);
    }

    private static bool HasDuplicateNonEmptyIds(IEnumerable<string?> ids)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string? id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id) && !seen.Add(id))
            {
                return true;
            }
        }

        return false;
    }

    private static AppDataValidationResult Invalid(string error) => new(Success: false, Error: error);
}
