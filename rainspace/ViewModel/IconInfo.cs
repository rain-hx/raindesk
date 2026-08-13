using rainspace.Constant;
using rainspace.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

/// <summary>
/// 图标信息
/// </summary>
namespace rainspace.ViewModel
{
    [Serializable]
    public class IconInfo : INotifyPropertyChanged
    {
        private string path = string.Empty;
        private string name = string.Empty;
        private int count;
        [JsonIgnore]
        private BitmapImage? bitmapImage;
        private byte[]? imageByteArr;
        private string content = string.Empty;
        private bool adminStartUp;
        private byte[]? defaultImage;
        private string iconId = Guid.NewGuid().ToString("N");
        private string? startArg;
        private string? lnkPath;
        private string? relativePath;
        private IconType iconType = IconType.OTHER;
        private bool isChecked;
        [NonSerialized]
        private bool isListSelected;
        [NonSerialized]
        private bool isAddShortcut;
        [NonSerialized]
        private bool isDragYieldPlaceholder;
        [NonSerialized]
        private int batchDragGroupCount = 1;
        [NonSerialized]
        private int dragPlaceholderColumnSpan = 1;
        [NonSerialized]
        private int dragPlaceholderRowSpan = 1;
        private bool isFolder;
        private ShortcutFolderSize folderSize = ShortcutFolderSize.Small;
        private ObservableCollection<IconInfo> folderItems = new();
        private int launcherGridColumn = -1;
        private int launcherGridRow = -1;

        public IconInfo()
        {
            AttachFolderItems(folderItems);
        }

        /// <summary>
        /// Updates the Content property based on current Path, Name, and Count values.
        /// </summary>
        private void UpdateContent()
        {
            content = IsFolder
                ? BuildFolderContent()
                : BuildShortcutContent();
        }

        private string BuildShortcutContent()
        {
            string countText = "\n使用次数: " + count;
            return string.IsNullOrEmpty(path)
                ? name + countText
                : path + "\n" + name + countText;
        }

        private string BuildFolderContent()
        {
            return $"{name}\n{FolderItems.Count} 个项目";
        }

        public void RefreshFolderMetadata()
        {
            string previous = content;
            UpdateContent();
            OnPropertyChanged(nameof(FolderItemCount_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewItems_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewOverflowCount_NoWrite));
            OnPropertyChanged(nameof(HasFolderPreviewOverflow_NoWrite));
            RaiseFolderPreviewImageChanges();
            if (!string.Equals(previous, content, StringComparison.Ordinal))
            {
                OnPropertyChanged(nameof(Content));
            }
        }

        private void AttachFolderItems(ObservableCollection<IconInfo> items)
        {
            items.CollectionChanged += FolderItems_CollectionChanged;
        }

        private void DetachFolderItems(ObservableCollection<IconInfo>? items)
        {
            if (items != null)
            {
                items.CollectionChanged -= FolderItems_CollectionChanged;
            }
        }

        private void FolderItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsFolder)
            {
                RefreshFolderMetadata();
            }
        }

        public bool IsChecked_NoWrite
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value, nameof(IsChecked_NoWrite));
        }

        [JsonIgnore]
        public bool IsListSelected_NoWrite
        {
            get => isListSelected;
            set
            {
                if (isListSelected == value) return;
                isListSelected = value;
                OnPropertyChanged(nameof(IsListSelected_NoWrite));
            }
        }

        [JsonIgnore]
        public bool IsAddShortcut_NoWrite
        {
            get => isAddShortcut;
            set
            {
                if (SetProperty(ref isAddShortcut, value, nameof(IsAddShortcut_NoWrite)))
                {
                    OnPropertyChanged(nameof(IsRegularShortcut_NoWrite));
                    OnPropertyChanged(nameof(IsLaunchableShortcut_NoWrite));
                }
            }
        }

        [JsonIgnore]
        public bool IsDragYieldPlaceholder_NoWrite
        {
            get => isDragYieldPlaceholder;
            set
            {
                if (isDragYieldPlaceholder == value) return;
                isDragYieldPlaceholder = value;
                OnPropertyChanged(nameof(IsDragYieldPlaceholder_NoWrite));
                OnPropertyChanged(nameof(IsRegularShortcut_NoWrite));
                OnPropertyChanged(nameof(IsLaunchableShortcut_NoWrite));
            }
        }

        [JsonIgnore]
        public int BatchDragGroupCount_NoWrite
        {
            get => batchDragGroupCount;
            set
            {
                if (batchDragGroupCount == value) return;
                batchDragGroupCount = value;
                OnPropertyChanged(nameof(BatchDragGroupCount_NoWrite));
            }
        }

        [JsonIgnore]
        public int DragPlaceholderColumnSpan_NoWrite
        {
            get => Math.Max(1, dragPlaceholderColumnSpan);
            set
            {
                int normalized = Math.Max(1, value);
                if (dragPlaceholderColumnSpan == normalized) return;
                dragPlaceholderColumnSpan = normalized;
                OnPropertyChanged(nameof(DragPlaceholderColumnSpan_NoWrite));
            }
        }

        [JsonIgnore]
        public int DragPlaceholderRowSpan_NoWrite
        {
            get => Math.Max(1, dragPlaceholderRowSpan);
            set
            {
                int normalized = Math.Max(1, value);
                if (dragPlaceholderRowSpan == normalized) return;
                dragPlaceholderRowSpan = normalized;
                OnPropertyChanged(nameof(DragPlaceholderRowSpan_NoWrite));
            }
        }

        [JsonIgnore]
        public bool IsRegularShortcut_NoWrite => !isAddShortcut && !isDragYieldPlaceholder;

        public string IconId
        {
            get => iconId;
            set => SetProperty(ref iconId, value ?? string.Empty, nameof(IconId));
        }

        public bool IsFolder
        {
            get => isFolder;
            set
            {
                if (SetProperty(ref isFolder, value, nameof(IsFolder)))
                {
                    UpdateContent();
                    OnPropertyChanged(nameof(IsLaunchableShortcut_NoWrite));
                    OnPropertyChanged(nameof(FolderItemCount_NoWrite));
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public ShortcutFolderSize FolderSize
        {
            get => Enum.IsDefined(typeof(ShortcutFolderSize), folderSize)
                ? folderSize
                : ShortcutFolderSize.Small;
            set
            {
                ShortcutFolderSize normalized = Enum.IsDefined(typeof(ShortcutFolderSize), value)
                    ? value
                    : ShortcutFolderSize.Small;
                if (SetProperty(ref folderSize, normalized, nameof(FolderSize)))
                {
                    OnPropertyChanged(nameof(FolderColumnSpan_NoWrite));
                    OnPropertyChanged(nameof(FolderRowSpan_NoWrite));
                    OnPropertyChanged(nameof(FolderPreviewItems_NoWrite));
                    OnPropertyChanged(nameof(FolderPreviewOverflowCount_NoWrite));
                    OnPropertyChanged(nameof(HasFolderPreviewOverflow_NoWrite));
                    RaiseFolderPreviewImageChanges();
                }
            }
        }

        public ObservableCollection<IconInfo> FolderItems
        {
            get
            {
                if (folderItems == null)
                {
                    folderItems = new ObservableCollection<IconInfo>();
                    AttachFolderItems(folderItems);
                }

                return folderItems;
            }
            set
            {
                var normalized = value ?? new ObservableCollection<IconInfo>();
                if (ReferenceEquals(folderItems, normalized))
                {
                    return;
                }

                DetachFolderItems(folderItems);
                if (SetProperty(ref folderItems, normalized, nameof(FolderItems)))
                {
                    AttachFolderItems(folderItems);
                    UpdateContent();
                    OnPropertyChanged(nameof(FolderItemCount_NoWrite));
                    OnPropertyChanged(nameof(FolderPreviewItems_NoWrite));
                    OnPropertyChanged(nameof(FolderPreviewOverflowCount_NoWrite));
                    OnPropertyChanged(nameof(HasFolderPreviewOverflow_NoWrite));
                    RaiseFolderPreviewImageChanges();
                    OnPropertyChanged(nameof(Content));
                }
                else
                {
                    AttachFolderItems(folderItems);
                }
            }
        }

        [JsonIgnore]
        public bool IsLaunchableShortcut_NoWrite => IsRegularShortcut_NoWrite && !IsFolder;

        [JsonIgnore]
        public int FolderItemCount_NoWrite => IsFolder ? FolderItems.Count : 0;

        [JsonIgnore]
        public IReadOnlyList<IconInfo> FolderPreviewItems_NoWrite
        {
            get
            {
                int limit = GetFolderPreviewLimit();
                int count = Math.Min(limit, FolderItems.Count);
                var previewItems = new List<IconInfo>(count);
                for (int i = 0; i < count; i++)
                {
                    previewItems.Add(FolderItems[i]);
                }

                return previewItems;
            }
        }

        [JsonIgnore]
        public int FolderPreviewOverflowCount_NoWrite => Math.Max(0, FolderItems.Count - GetFolderPreviewLimit());

        [JsonIgnore]
        public bool HasFolderPreviewOverflow_NoWrite => FolderPreviewOverflowCount_NoWrite > 0;

        [JsonIgnore]
        public BitmapImage? FolderPreviewImage1_NoWrite => GetFolderPreviewImage(0);

        [JsonIgnore]
        public BitmapImage? FolderPreviewImage2_NoWrite => GetFolderPreviewImage(1);

        [JsonIgnore]
        public BitmapImage? FolderPreviewImage3_NoWrite => GetFolderPreviewImage(2);

        [JsonIgnore]
        public BitmapImage? FolderPreviewImage4_NoWrite => GetFolderPreviewImage(3);

        [JsonIgnore]
        public BitmapImage? FolderPreviewImage5_NoWrite => GetFolderPreviewImage(4);

        [JsonIgnore]
        public BitmapImage? FolderPreviewImage6_NoWrite => GetFolderPreviewImage(5);

        private BitmapImage? GetFolderPreviewImage(int index)
        {
            return index >= 0 && index < FolderPreviewItems_NoWrite.Count
                ? FolderPreviewItems_NoWrite[index].BitmapImage_NoWrite
                : null;
        }

        private void RaiseFolderPreviewImageChanges()
        {
            OnPropertyChanged(nameof(FolderPreviewImage1_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewImage2_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewImage3_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewImage4_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewImage5_NoWrite));
            OnPropertyChanged(nameof(FolderPreviewImage6_NoWrite));
        }

        private int GetFolderPreviewLimit()
            => FolderSize == ShortcutFolderSize.Large ? 6 : 4;

        [JsonIgnore]
        public int FolderColumnSpan_NoWrite => FolderSize == ShortcutFolderSize.Small ? 1 : 2;

        [JsonIgnore]
        public int FolderRowSpan_NoWrite => FolderSize == ShortcutFolderSize.Large ? 2 : 1;

        public int LauncherGridColumn
        {
            get => launcherGridColumn;
            set
            {
                int normalized = value < 0 ? -1 : value;
                if (SetProperty(ref launcherGridColumn, normalized, nameof(LauncherGridColumn)))
                {
                    OnPropertyChanged(nameof(HasManualLauncherGridPosition_NoWrite));
                }
            }
        }

        public int LauncherGridRow
        {
            get => launcherGridRow;
            set
            {
                int normalized = value < 0 ? -1 : value;
                if (SetProperty(ref launcherGridRow, normalized, nameof(LauncherGridRow)))
                {
                    OnPropertyChanged(nameof(HasManualLauncherGridPosition_NoWrite));
                }
            }
        }

        [JsonIgnore]
        public bool HasManualLauncherGridPosition_NoWrite => LauncherGridColumn >= 0 && LauncherGridRow >= 0;

        internal bool SetLauncherGridPositionSilently(int column, int row)
        {
            int normalizedColumn = column < 0 ? -1 : column;
            int normalizedRow = row < 0 ? -1 : row;
            if (launcherGridColumn == normalizedColumn && launcherGridRow == normalizedRow)
            {
                return false;
            }

            launcherGridColumn = normalizedColumn;
            launcherGridRow = normalizedRow;
            return true;
        }

        public string? RelativePath_NoWrite
        {
            get => relativePath;
            set => relativePath = value;
        }

        public string? RelativePath
        {
            get => relativePath;
            set => SetProperty(ref relativePath, value, nameof(RelativePath));
        }

        public string? LnkPath_NoWrite
        {
            get => lnkPath;
            set => lnkPath = value;
        }

        public string? LnkPath
        {
            get => lnkPath;
            set => SetProperty(ref lnkPath, value, nameof(LnkPath));
        }

        public string? StartArg
        {
            get => startArg;
            set => SetProperty(ref startArg, value, nameof(StartArg));
        }

        public string? StartArg_NoWrite
        {
            get => startArg;
            set => startArg = value;
        }

        public IconType IconType
        {
            get => iconType == 0 ? IconType.OTHER : iconType;
            set => SetProperty(ref iconType, value, nameof(IconType));
        }

        public byte[]? DefaultImage
        {
            get => defaultImage;
            set => SetProperty(ref defaultImage, value, nameof(DefaultImage));
        }

        public byte[]? DefaultImage_NoWrite
        {
            get => defaultImage;
            set => defaultImage = value;
        }

        public bool AdminStartUp
        {
            get => adminStartUp;
            set => SetProperty(ref adminStartUp, value, nameof(AdminStartUp));
        }

        public int Count
        {
            get => count;
            set
            {
                if (count == value) return;
                count = value;
                UpdateContent();
                OnPropertyChanged(nameof(Count));
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (name == value) return;
                name = value;
                UpdateContent();
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Name_NoWrite
        {
            get => name;
            set
            {
                name = value;
                UpdateContent();
            }
        }

        public string Path
        {
            get => path;
            set
            {
                if (path == value) return;
                path = value;
                UpdateContent();
                OnPropertyChanged(nameof(Path));
            }
        }

        public string Path_NoWrite
        {
            get => path;
            set
            {
                path = value;
                UpdateContent();
            }
        }


        [JsonIgnore]
        public BitmapImage? BitmapImage
        {
            get
            {
                if (bitmapImage != null) return bitmapImage;
                // 与原始 WPF 版本保持一致：每次都从 byte[] 解码
                if (imageByteArr != null && imageByteArr.Length > 0)
                    return bitmapImage = ImageUtil.ByteArrToImage(imageByteArr);
                if (defaultImage != null && defaultImage.Length > 0)
                    return bitmapImage = ImageUtil.ByteArrToImage(defaultImage);
                return null;
            }
            set
            {
                bitmapImage = value;
                // WinUI 3 中 BitmapImageToByte 返回 null，需要从文件路径提取字节
                if (value != null && (imageByteArr == null || imageByteArr.Length == 0))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(path))
                        {
                            var bytes = FileIcon.GetIconBytes(path);
                            if (bytes != null && bytes.Length > 0)
                                imageByteArr = bytes;
                        }
                    }
                    catch { }
                }
                OnPropertyChanged("BitmapImage");
            }
        }

        [JsonIgnore]
        public BitmapImage? BitmapImage_NoWrite
        {
            get
            {
                if (bitmapImage != null) return bitmapImage;
                // 与原始 WPF 版本保持一致：每次都从 byte[] 解码
                if (imageByteArr != null && imageByteArr.Length > 0)
                    return bitmapImage = ImageUtil.ByteArrToImage(imageByteArr);
                if (defaultImage != null && defaultImage.Length > 0)
                    return bitmapImage = ImageUtil.ByteArrToImage(defaultImage);
                return null;
            }
            set
            {
                bitmapImage = value;
                // WinUI 3 中 BitmapImageToByte 返回 null，需要从文件路径提取字节
                if (value != null && (imageByteArr == null || imageByteArr.Length == 0))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(path))
                        {
                            var bytes = FileIcon.GetIconBytes(path);
                            if (bytes != null && bytes.Length > 0)
                                imageByteArr = bytes;
                        }
                    }
                    catch { }
                }
                OnPropertyChanged("BitmapImage_NoWrite");
            }
        }

        /// <summary>
        /// 直接设置 BitmapImage 缓存（不触发 BitmapImageToByte 转换）
        /// </summary>
        public void SetBitmapImageDirect(BitmapImage bi)
        {
            bitmapImage = bi;
            OnPropertyChanged(nameof(BitmapImage));
            OnPropertyChanged(nameof(BitmapImage_NoWrite));
        }


        public byte[]? ImageByteArr
        {
            get => imageByteArr;
            set
            {
                imageByteArr = value;
                // PERF: Invalidate cached bitmap when raw bytes change.
                bitmapImage = null;
                OnPropertyChanged(nameof(ImageByteArr));
                OnPropertyChanged(nameof(BitmapImage));
                OnPropertyChanged(nameof(BitmapImage_NoWrite));
            }
        }

        public byte[]? ImageByteArr_NoWrite
        {
            get => imageByteArr;
            set
            {
                imageByteArr = value;
                // PERF: Invalidate cached bitmap when raw bytes change.
                bitmapImage = null;
            }
        }

        public string Content
        {
            get
            {
                return content;
            }
            set => SetProperty(ref content, value, nameof(Content));
        }


        public string Content_NoWrite
        {
            get
            {
                return content;
            }
            set
            {
                content = value;
            }
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
