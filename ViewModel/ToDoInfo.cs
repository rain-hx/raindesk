using raindesk.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace raindesk.ViewModel
{

    [Serializable]
    public class ToDoInfo : INotifyPropertyChanged
    {
        private string id = string.Empty;
        private string title = string.Empty; //待办事项
        private string? msg;  //事项详情
        private string? exeTime;  //待办时间
        private string? doneTime; //完成时间
        private TodoTaskExecType execType = TodoTaskExecType.SET_TIME;
        private string? cron;  //cron表达式

        public string Id
        {
            get => id;
            set => SetProperty(ref id, value ?? string.Empty, nameof(Id));
        }

        public string? Cron
        {
            get => cron;
            set => SetProperty(ref cron, value, nameof(Cron));
        }


        public TodoTaskExecType ExecType
        {
            get
            {
                //兼容老版本 需要给个默认值
                if (execType == 0) return TodoTaskExecType.SET_TIME;
                return execType;
            }
            set => SetProperty(ref execType, value, nameof(ExecType));
        }

        public string? DoneTime
        {
            get => doneTime;
            set => SetProperty(ref doneTime, value, nameof(DoneTime));
        }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value, nameof(Title));
        }

        public string? Msg
        {
            get => msg;
            set => SetProperty(ref msg, value, nameof(Msg));
        }

        /// <summary>Description 别名，与 Msg 等效（新代码使用 Description）</summary>
        public string? Description
        {
            get => msg;
            set
            {
                if (SetProperty(ref msg, value, nameof(Description)))
                {
                    OnPropertyChanged(nameof(Msg));
                }
            }
        }

        public string? ExeTime
        {
            get => exeTime;
            set => SetProperty(ref exeTime, value, nameof(ExeTime));
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
