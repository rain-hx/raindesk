using Newtonsoft.Json;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace rainspace.Services;

/// <summary>
/// 过渡期的集中式模型变更跟踪器。
/// 旧界面仍直接绑定并修改 AppConfig、MenuInfo、IconInfo 等模型时，
/// 由此服务统一请求保存，避免领域/数据模型依赖全局持久化入口。
/// </summary>
public sealed class AppDataChangeTracker : IDisposable
{
    private readonly IAppDataProvider _dataProvider;
    private readonly ISaveService _saveService;
    private readonly HashSet<INotifyPropertyChanged> _observedModels =
        new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<INotifyCollectionChanged> _observedCollections =
        new(ReferenceEqualityComparer.Instance);
    private bool _started;
    private bool _disposed;

    public AppDataChangeTracker(IAppDataProvider dataProvider, ISaveService saveService)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            return;
        }

        _started = true;
        _dataProvider.StateReplaced += OnStateReplaced;
        RebuildSubscriptions(_dataProvider.Current);
    }

    private void OnStateReplaced(AppData data)
    {
        RebuildSubscriptions(data);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_disposed || sender == null)
        {
            return;
        }

        // 集合引用可能刚被替换；先刷新订阅，使后续集合变化不会丢失。
        RebuildSubscriptions(_dataProvider.Current);
        if (IsPersistedProperty(sender, e.PropertyName))
        {
            _saveService.SaveDebounced();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        RebuildSubscriptions(_dataProvider.Current);
        _saveService.SaveDebounced();
    }

    private void RebuildSubscriptions(AppData data)
    {
        DetachAll();

        ObserveModel(data);
        ObserveModel(data.AppConfig);
        ObserveCollection(data.MenuList);
        ObserveCollection(data.AppConfig.CustomBGParams);
        ObserveCollection(data.AppConfig.AllGroupsIconList);
        ObserveModel(data.AppConfig.GradientBGParam);

        foreach (GradientBGParam gradient in data.AppConfig.CustomBGParams)
        {
            ObserveModel(gradient);
        }

        foreach (MenuInfo menu in data.MenuList)
        {
            ObserveModel(menu);
            ObserveCollection(menu.IconList);
            foreach (IconInfo icon in menu.IconList)
            {
                ObserveIconTree(icon);
            }
        }

        foreach (IconInfo icon in data.AppConfig.AllGroupsIconList)
        {
            ObserveIconTree(icon);
        }
    }

    private void ObserveIconTree(IconInfo icon)
    {
        if (!_observedModels.Add(icon))
        {
            return;
        }

        icon.PropertyChanged += OnPropertyChanged;
        ObserveCollection(icon.FolderItems);
        foreach (IconInfo child in icon.FolderItems)
        {
            ObserveIconTree(child);
        }
    }

    private void ObserveModel(INotifyPropertyChanged? model)
    {
        if (model != null && _observedModels.Add(model))
        {
            model.PropertyChanged += OnPropertyChanged;
        }
    }

    private void ObserveCollection(INotifyCollectionChanged? collection)
    {
        if (collection != null && _observedCollections.Add(collection))
        {
            collection.CollectionChanged += OnCollectionChanged;
        }
    }

    private void DetachAll()
    {
        foreach (INotifyPropertyChanged model in _observedModels)
        {
            model.PropertyChanged -= OnPropertyChanged;
        }
        _observedModels.Clear();

        foreach (INotifyCollectionChanged collection in _observedCollections)
        {
            collection.CollectionChanged -= OnCollectionChanged;
        }
        _observedCollections.Clear();
    }

    private static bool IsPersistedProperty(object sender, string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return true;
        }

        if (propertyName.Contains("NoWrite", StringComparison.Ordinal))
        {
            return false;
        }

        PropertyInfo? property = sender.GetType().GetProperty(propertyName);
        return property?.GetCustomAttribute<JsonIgnoreAttribute>() == null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_started)
        {
            _dataProvider.StateReplaced -= OnStateReplaced;
        }
        DetachAll();
    }
}
