using System.Collections.ObjectModel;
using System.ComponentModel;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

public sealed class LauncherFolderOverlayViewModel : INotifyPropertyChanged
{
    private readonly LauncherFolderNavigationState _navigation = new();

    public IconInfo? Folder => _navigation.Folder;

    public IReadOnlyList<IconInfo> Path => _navigation.Path;

    public ObservableCollection<IconInfo>? Items => Folder?.FolderItems;

    public bool IsOpen => _navigation.IsOpen;

    public bool CanGoBack => _navigation.CanGoBack;

    public string Title => string.Join(" / ", Path.Select(item => item.Name));

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OpenRoot(IconInfo folder)
    {
        _navigation.OpenRoot(folder);
        PublishState();
    }

    public void OpenChild(IconInfo folder)
    {
        _navigation.OpenChild(folder);
        PublishState();
    }

    public void Back()
    {
        _navigation.Back();
        PublishState();
    }

    public void Close()
    {
        _navigation.Close();
        PublishState();
    }

    public void Refresh() => PublishState();

    private void PublishState()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Folder)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoBack)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
    }
}
