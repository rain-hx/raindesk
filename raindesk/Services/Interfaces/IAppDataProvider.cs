using raindesk.ViewModel;

namespace raindesk.Services.Interfaces;

/// <summary>
/// 应用数据的统一访问点，替代旧的窗口静态状态。
/// 所有需要读取或修改 AppData 的代码都应通过此接口。
/// </summary>
public interface IAppDataProvider
{
    /// <summary>
    /// 整个 AppData 实例被导入、恢复或切换时触发。
    /// 普通属性修改不会触发该事件。
    /// </summary>
    event Action<AppData>? StateReplaced;

    /// <summary>
    /// 获取当前 AppData 实例。
    /// 返回的是实际对象引用，调用者不应在持有时期望它不变。
    /// 如需遍历集合，请使用 Read() 或 GetSnapshot()。
    /// </summary>
    AppData Current { get; }

    /// <summary>
    /// 获取数据的不可变快照，用于后台序列化（备份、保存）。
    /// </summary>
    AppDataSnapshot GetSnapshot();

    /// <summary>
    /// 在锁内安全读取数据，避免并发修改。
    /// </summary>
    T Read<T>(Func<AppData, T> reader);

    /// <summary>
    /// 在锁内安全修改数据。
    /// </summary>
    void Write(Action<AppData> writer);

    /// <summary>
    /// 替换整个数据实例（用于加载备份、恢复等场景）。
    /// </summary>
    void Replace(AppData newData);
}
