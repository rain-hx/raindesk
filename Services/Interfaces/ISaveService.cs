namespace raindesk.Services.Interfaces;

/// <summary>
/// 数据持久化服务，负责将 AppData 保存到磁盘。
/// 替代 CommonCode 中散落的静态保存方法。
/// </summary>
public interface ISaveService
{
    /// <summary>
    /// 立即保存当前数据到磁盘。
    /// </summary>
    void SaveNow();

    /// <summary>
    /// 防抖保存：延迟 500ms 后写入，期间重复调用会重置计时器。
    /// 适用于 PropertyChanged 等高频触发场景。
    /// </summary>
    void SaveDebounced();

    /// <summary>
    /// 取消尚未执行的防抖保存，并等待已经开始的保存结束。
    /// 导入数据前调用，避免旧数据在导入完成后覆盖新数据。
    /// </summary>
    void CancelPendingSave();

    /// <summary>
    /// 异步取消尚未执行的防抖保存，并等待已经开始的保存结束。
    /// </summary>
    global::System.Threading.Tasks.Task CancelPendingSaveAsync(global::System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// 立即写入并取消待执行的防抖保存。
    /// 适用于退出、备份前等需要确保数据落盘的场景。
    /// </summary>
    void Flush();

    /// <summary>
    /// 异步立即写入并取消待执行的防抖保存。
    /// </summary>
    global::System.Threading.Tasks.Task FlushAsync(global::System.Threading.CancellationToken cancellationToken = default);

}
