using rainspace.Constant;
using rainspace.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace rainspace.ViewModel
{

    [Serializable]
    public class MenuInfo : INotifyPropertyChanged
    {


        private string menuName = string.Empty;
        private string menuId = string.Empty;
        private Visibility menuEdit = Visibility.Collapsed;
        private string? menuGeometry;  //菜单几何图标
        private string? geometryColor; //几何图标颜色
        private ObservableCollection<IconInfo> iconList = new();
        private bool isEncrypt;  //是否加密
        private MenuType menuType; //菜单类型 普通,  关联
        private string? linkPath; //关联路径
        [NonSerialized]
        private bool isSelected_NoWrite;
        [NonSerialized]
        private bool isDragDropTarget_NoWrite;
        [NonSerialized]
        private bool isAllGroupsTab_NoWrite;
        [NonSerialized]
        private SolidColorBrush? groupTabLabelBrush_NoWrite;

        private static readonly global::Windows.UI.Color GroupTabSelectedColor = global::Windows.UI.Color.FromArgb(255, 8, 123, 255);
        private static readonly global::Windows.UI.Color GroupTabNormalColor = global::Windows.UI.Color.FromArgb(255, 103, 119, 150);


        public string? LinkPath
        {
            get => linkPath;
            set => SetProperty(ref linkPath, value, nameof(LinkPath));
        }

        public MenuType MenuType
        {
            get
            {
                return menuType;
            }
            set => SetProperty(ref menuType, value, nameof(MenuType));
        }

        public bool IsEncrypt
        {
            get
            {
                return isEncrypt;
            }
            set => SetProperty(ref isEncrypt, value, nameof(IsEncrypt));
        }

        public string MenuGeometry
        {
            get
            {
                if (menuGeometry == null)
                {
                    return Constants.DEFAULT_MENU_GEOMETRY;
                }
                return menuGeometry;
            }
            set => SetProperty(ref menuGeometry, value, nameof(MenuGeometry));
        }

        public string GeometryColor
        {
            get
            {
                if (geometryColor == null)
                {
                    return Constants.DEFAULT_MENU_GEOMETRY_COLOR;
                }
                return geometryColor;
            }
            set => SetProperty(ref geometryColor, value, nameof(GeometryColor));
        }

        public string MenuName
        {
            get
            {
                return menuName;
            }
            set => SetProperty(ref menuName, value, nameof(MenuName));
        }

        public string MenuId
        {
            get
            {
                return menuId;
            }
            set => SetProperty(ref menuId, value, nameof(MenuId));
        }

        [JsonIgnore]
        public Visibility MenuEdit
        {
            get => menuEdit == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
            set
            {
                var normalized = value == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
                if (menuEdit == normalized) return;

                menuEdit = normalized;
                OnPropertyChanged(nameof(MenuEdit));
                OnPropertyChanged(nameof(NotMenuEdit));
            }
        }

        [JsonIgnore]
        public Visibility NotMenuEdit =>
            MenuEdit == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public ObservableCollection<IconInfo> IconList
        {
            get
            {
                return iconList;
            }
            set
            {
                var normalized = value ?? new ObservableCollection<IconInfo>();
                SetProperty(ref iconList, normalized, nameof(IconList));
            }
        }

        [JsonIgnore]
        public bool IsSelected_NoWrite
        {
            get
            {
                return isSelected_NoWrite;
            }
            set
            {
                if (isSelected_NoWrite == value) return;

                isSelected_NoWrite = value;
                if (groupTabLabelBrush_NoWrite != null)
                {
                    groupTabLabelBrush_NoWrite.Color = CurrentGroupTabLabelColor;
                }

                OnPropertyChanged("IsSelected_NoWrite");
                OnPropertyChanged(nameof(SelectionIndicatorOpacity_NoWrite));
                OnPropertyChanged(nameof(GroupTabLabelBrush_NoWrite));
            }
        }

        [JsonIgnore]
        public double SelectionIndicatorOpacity_NoWrite => isSelected_NoWrite ? 1.0 : 0.0;

        [JsonIgnore]
        public SolidColorBrush GroupTabLabelBrush_NoWrite =>
            groupTabLabelBrush_NoWrite ??= new SolidColorBrush(CurrentGroupTabLabelColor);

        [JsonIgnore]
        private global::Windows.UI.Color CurrentGroupTabLabelColor =>
            isSelected_NoWrite ? GroupTabSelectedColor : GroupTabNormalColor;

        [JsonIgnore]
        public bool IsDragDropTarget_NoWrite
        {
            get => isDragDropTarget_NoWrite;
            set
            {
                if (isDragDropTarget_NoWrite == value) return;
                isDragDropTarget_NoWrite = value;
                OnPropertyChanged(nameof(IsDragDropTarget_NoWrite));
                OnPropertyChanged(nameof(DragDropArrowOpacity_NoWrite));
                OnPropertyChanged(nameof(DragDropArrowVisibility_NoWrite));
            }
        }

        [JsonIgnore]
        public double DragDropArrowOpacity_NoWrite => isDragDropTarget_NoWrite ? 1.0 : 0.0;

        [JsonIgnore]
        public Visibility DragDropArrowVisibility_NoWrite =>
            isDragDropTarget_NoWrite ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public bool IsAllGroupsTab_NoWrite
        {
            get => isAllGroupsTab_NoWrite;
            set
            {
                if (isAllGroupsTab_NoWrite == value) return;
                isAllGroupsTab_NoWrite = value;
                OnPropertyChanged(nameof(IsAllGroupsTab_NoWrite));
                OnPropertyChanged(nameof(NotAllGroupsTab_NoWrite));
            }
        }

        [JsonIgnore]
        public bool NotAllGroupsTab_NoWrite => !isAllGroupsTab_NoWrite;

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
