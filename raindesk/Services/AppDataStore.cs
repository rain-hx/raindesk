using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using raindesk.Constant;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Services
{
    /// <summary>
    /// 线程安全的应用数据存储，实现 IAppDataProvider。
    /// 替代旧的窗口静态状态，提供受控的数据访问。
    /// </summary>
    public class AppDataStore : IAppDataProvider
    {
        private readonly object _lock = new();
        private AppData _current = new();

        public event Action<AppData>? StateReplaced;

        /// <inheritdoc/>
        public AppData Current
        {
            get { lock (_lock) return _current; }
        }

        /// <inheritdoc/>
        public AppDataSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new AppDataSnapshot(_current);
            }
        }

        /// <inheritdoc/>
        public T Read<T>(Func<AppData, T> reader)
        {
            lock (_lock)
            {
                return reader(_current);
            }
        }

        /// <inheritdoc/>
        public void Write(Action<AppData> writer)
        {
            lock (_lock)
            {
                writer(_current);
            }
        }

        /// <inheritdoc/>
        public void Replace(AppData newData)
        {
            AppData replacement = newData ?? new AppData();
            bool changed;
            lock (_lock)
            {
                changed = !ReferenceEquals(_current, replacement);
                _current = replacement;
            }

            if (changed)
            {
                StateReplaced?.Invoke(replacement);
            }
        }

    }

    /// <summary>
    /// 数据快照，用于后台序列化。
    /// 包含序列化后的 JSON 字符串，避免在序列化期间锁定数据。
    /// </summary>
    public class AppDataSnapshot
    {
        public string Json { get; }
        public DateTime Timestamp { get; }

        private static readonly JsonSerializerSettings _settings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public AppDataSnapshot(AppData data)
        {
            Timestamp = DateTime.Now;
            Json = JsonConvert.SerializeObject(data, Formatting.Indented, _settings);
        }
    }
}
