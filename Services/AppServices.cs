using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using raindesk.Constant;
using raindesk.Features.ImportExport.Contracts;
using raindesk.Features.ImportExport.Application;
using raindesk.Features.ImportExport.Infrastructure;
using raindesk.Features.ImportExport.Presentation;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Infrastructure;
using raindesk.Features.Launcher.Presentation;
using raindesk.Features.Search.Application;
using raindesk.Features.Search.Contracts;
using raindesk.Features.Search.Infrastructure;
using raindesk.Features.Settings.Application;
using raindesk.Features.Settings.Contracts;
using raindesk.Features.Settings.Presentation;
using raindesk.Features.Sidecar.Application;
using raindesk.Features.Sidecar.Contracts;
using raindesk.Features.Sidecar.Infrastructure;
using raindesk.Features.Plugins.Application;
using raindesk.Features.Plugins.Contracts;
using raindesk.Features.Plugins.Infrastructure;
using raindesk.Features.Plugins.Presentation.WinUI;
using raindesk.Features.Appearance.Contracts;
using raindesk.Features.Appearance.Infrastructure;
using raindesk.Features.Tray.Contracts;
using raindesk.Features.Tray.Infrastructure;
using raindesk.Features.Workspace.Contracts;
using raindesk.Features.Workspace.Infrastructure;
using raindesk.Services.Interfaces;

namespace raindesk.Services;

/// <summary>
/// 应用级 DI 容器配置。
/// 在 App.OnLaunched 中调用 ConfigureServices() 初始化，
/// 然后通过 AppServices.Provider 获取服务。
/// </summary>
public static class AppServices
{
    private static IServiceProvider? _provider;

    /// <summary>
    /// 全局服务提供者。在 ConfigureServices() 调用前访问会抛出异常。
    /// </summary>
    public static IServiceProvider Provider
    {
        get => _provider ?? throw new InvalidOperationException(
            "服务尚未初始化，请先调用 AppServices.ConfigureServices()");
    }

    /// <summary>
    /// 服务是否已初始化。
    /// </summary>
    public static bool IsConfigured => _provider != null;

    /// <summary>
    /// 注册所有应用服务。应在 App.OnLaunched 中、创建 MainWindow 之前调用。
    /// </summary>
    public static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // ── 核心数据服务 ──────────────────────────────────────────
        // AppDataStore 作为单例，同时注册为 IAppDataProvider
        var dataStore = new AppDataStore();
        services.AddSingleton<IAppDataProvider>(dataStore);
        services.AddSingleton(dataStore); // 也允许直接注入 AppDataStore

        // ── 持久化服务 ────────────────────────────────────────────
        services.AddSingleton<ISaveService>(sp =>
        {
            var provider = sp.GetRequiredService<IAppDataProvider>();
            return new SaveService(provider, Constants.DATA_FILE_PATH);
        });
        services.AddSingleton<AppDataChangeTracker>();

        // ── ImportExport 模块 ─────────────────────────────────────
        services.AddSingleton<BackupDocumentReader>();
        services.AddSingleton<IBackupService>(sp => new BackupService(
            sp.GetRequiredService<IAppDataProvider>(),
            sp.GetRequiredService<ISaveService>(),
            sp.GetRequiredService<BackupDocumentReader>(),
            Constants.DATA_FILE_PATH));
        services.AddSingleton<IBackupFilePicker, WinUiBackupFilePicker>();

        // ── 窗口服务（延迟初始化，MainWindow 构造后调用 Initialize） ──
        var dispatcher = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("ConfigureServices must be called on the UI thread");

        services.AddSingleton(dispatcher);
        services.AddSingleton<TrayService>();
        services.AddSingleton<ITrayController>(sp => sp.GetRequiredService<TrayService>());
        services.AddSingleton<IHotkeyRegistrar, WindowsHotkeyRegistrar>();
        services.AddSingleton<WindowGeometryService>();
        services.AddSingleton<NavigationService>();

        // ── 业务服务 ──────────────────────────────────────────────
        services.AddSingleton<TaskHistoryService>();
        services.AddSingleton<IToDoService, ToDoService>();

        // ── Plugins 模块（第一阶段仅显式内置插件） ────────────────
        services.AddSingleton<IBuiltInPlugin>(new StaticBuiltInPlugin(new PluginManifest(
            "raindesk.everything",
            "Everything Search",
            "1.0.0",
            PluginApi.CurrentVersion,
            IsBuiltIn: true,
            new[] { PluginCapability.Search },
            new[]
            {
                new PluginContributionDescriptor(
                    "everything",
                    PluginContributionKind.SearchProvider,
                    "Everything 搜索")
            })));
        services.AddSingleton<IPluginManifestValidator>(_ => new PluginManifestValidator());
        services.AddSingleton<PluginCatalog>();
        services.AddSingleton<IPluginCatalog>(sp => sp.GetRequiredService<PluginCatalog>());
        services.AddSingleton<IPluginContributionGate>(sp => sp.GetRequiredService<PluginCatalog>());
        services.AddSingleton<ILauncherPluginCommandRouter>(sp =>
            new LauncherPluginCommandRouter(
                sp.GetServices<ILauncherPluginCommandContribution>(),
                sp.GetRequiredService<IPluginContributionGate>()));
        services.AddSingleton<IPluginImporterRouter>(sp =>
            new PluginImporterRouter(
                sp.GetServices<IPluginImporterContribution>(),
                sp.GetRequiredService<IPluginContributionGate>()));

        // ── Appearance 模块 ──────────────────────────────────────
        services.AddSingleton<IAppearanceQueryHandler, AppDataAppearanceQueryHandler>();
        services.AddSingleton<IAppearanceCommandHandler, AppDataAppearanceCommandHandler>();

        // ── Search 模块 ──────────────────────────────────────────
        services.AddSingleton<ISearchProvider, LauncherSearchProvider>();
        services.AddSingleton<ISearchProvider, EverythingSearchProvider>();
        services.AddSingleton<ISearchQueryHandler, SearchQueryHandler>();

        // ── Settings 模块 ────────────────────────────────────────
        services.AddSingleton<ISettingsSectionProvider, BuiltInSettingsSectionProvider>();
        services.AddSingleton<ISettingsSectionCatalog, SettingsSectionCatalog>();
        services.AddSingleton<ISettingsPageFactory, BuiltInSettingsPageFactory>();
        services.AddSingleton<SettingsPageResolver>();

        // ── Workspace / Launcher 模块 ────────────────────────────
        services.AddSingleton<IWorkspaceQueryHandler, AppDataWorkspaceQueryHandler>();
        services.AddSingleton<IWorkspaceCommandHandler, AppDataWorkspaceCommandHandler>();
        services.AddSingleton<ILauncherCommandHandler, AppDataLauncherCommandHandler>();
        services.AddSingleton<ILauncherShortcutDraftSource, WindowsLauncherShortcutDraftSource>();
        services.AddSingleton<ILauncherExternalImportService, LauncherExternalImportService>();
        services.AddSingleton<ILauncherActivationPort, WindowsLauncherActivationPort>();
        services.AddSingleton<ILauncherActivationService, LauncherActivationService>();
        services.AddSingleton<ILauncherSystemCandidateSource, WindowsLauncherSystemCandidateSource>();
        services.AddSingleton<LauncherBatchModeState>();

        // ── Sidecar 模块 ─────────────────────────────────────────
        // 默认安全降级；配置 Node 路径后只需在 composition root 替换 ISidecarConnection。
        services.AddSingleton<ISidecarConnection>(
            new UnavailableSidecarConnection("Node sidecar 尚未配置。"));
        services.AddSingleton(SidecarClientOptions.Disabled);
        services.AddSingleton<ISidecarClient, SidecarClient>();

        _provider = services.BuildServiceProvider();
    }

    /// <summary>
    /// 获取服务。如果服务未初始化，抛出异常。
    /// </summary>
    public static T Get<T>() where T : notnull
    {
        return Provider.GetRequiredService<T>();
    }

    /// <summary>
    /// 尝试获取服务，未注册时返回 null。
    /// </summary>
    public static T? TryGet<T>() where T : class
    {
        return Provider.GetService<T>();
    }

    /// <summary>
    /// 仅用于测试：重置容器状态。
    /// </summary>
    internal static void ResetForTests()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _provider = null;
    }
}
