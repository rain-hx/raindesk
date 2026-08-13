using raindesk.Constant;
using raindesk.Services.Migrations;
using raindesk.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// 程序数据
/// </summary>
namespace raindesk.ViewModel
{
    [Serializable]
    public class AppData : INotifyPropertyChanged
    {
        /// <summary>
        /// 持久化结构版本。旧 JSON 缺少该字段时由 Json.NET 填充为 0，
        /// 新建数据则直接使用当前版本。
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(AppDataSchema.LegacyVersion)]
        public int SchemaVersion { get; set; } = AppDataSchema.CurrentVersion;

        private ObservableCollection<MenuInfo> menuList = new(); //菜单信息及菜单对应icon信息
        private AppConfig appConfig = new(); //程序设置信息
        private ObservableCollection<ToDoInfo> hiToDoList = new(); //历史待办（旧，迁移后不再写入）
        private ObservableCollection<ToDoInfo> toDoList = new(); //未处理待办（旧，迁移后不再写入）
        private List<ToDoTask> tasks = new(); //新任务模型（替代 ToDoList + HiToDoList）
        private List<TaskProject> projects = new(); //项目列表（参考 TaskTrove 的 projects）


        public ObservableCollection<ToDoInfo> HiToDoList
        {
            get => hiToDoList;
            set
            {
                var normalized = value ?? new ObservableCollection<ToDoInfo>();
                SetProperty(ref hiToDoList, normalized);
            }
        }

        public ObservableCollection<ToDoInfo> ToDoList
        {
            get => toDoList;
            set
            {
                var normalized = value ?? new ObservableCollection<ToDoInfo>();
                SetProperty(ref toDoList, normalized);
            }
        }

        public ObservableCollection<MenuInfo> MenuList
        {
            get => menuList;
            set
            {
                var normalized = value ?? new ObservableCollection<MenuInfo>();
                SetProperty(ref menuList, normalized);
            }
        }

        /// <summary>
        /// 新任务模型列表（替代 ToDoList + HiToDoList）。
        /// 使用 Status 字段区分活跃/已完成/已归档。
        /// </summary>
        public List<ToDoTask> Tasks
        {
            get => tasks;
            set
            {
                var normalized = value ?? new();
                SetProperty(ref tasks, normalized);
            }
        }

        /// <summary>
        /// 项目列表（参考 TaskTrove 的 projects）。
        /// 用于将任务分组到不同项目中。
        /// </summary>
        public List<TaskProject> Projects
        {
            get => projects;
            set
            {
                var normalized = value ?? new();
                SetProperty(ref projects, normalized);
            }
        }


        public AppConfig AppConfig
        {
            get
            {
                return appConfig;
            }
            set
            {
                var normalized = value ?? new AppConfig();
                SetProperty(ref appConfig, normalized);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// PERF: Uses CallerMemberName to avoid allocating PropertyChangedEventArgs with magic strings.
        /// The compiler injects the caller's property name at compile time (zero runtime cost).
        /// </summary>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

        /// <summary>
        /// 加载默认数据
        /// </summary>
        public static AppData LoadDefault()
        {
            return CommonCode.GetAppDataByFile(Constants.DATA_FILE_PATH);
        }
    }
}
