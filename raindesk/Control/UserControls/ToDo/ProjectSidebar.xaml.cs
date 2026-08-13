using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace raindesk.Control.UserControls.ToDo;

public sealed partial class ProjectSidebar : UserControl
{
    private IToDoService? _todoService;
    private IAppDataProvider? _dataProvider;
    private List<TaskProject> _projects = new();

    public ProjectSidebar()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    public event Action<string?>? ProjectSelected;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _todoService = AppServices.TryGet<IToDoService>();
        _dataProvider = AppServices.TryGet<IAppDataProvider>();
        LoadProjects();
    }

    private void LoadProjects()
    {
        if (_todoService == null) return;

        var appData = _dataProvider?.Current;
        if (appData?.Projects != null)
        {
            _projects = appData.Projects.OrderBy(p => p.SortOrder).ToList();
        }

        ProjectList.Items.Clear();

        var allItem = new ListViewItem
        {
            Content = CreateProjectItemContent("全部任务", "\uE8FD"),
            Tag = null
        };
        ProjectList.Items.Add(allItem);

        foreach (var project in _projects)
        {
            var item = new ListViewItem
            {
                Content = CreateProjectItemContent(project.Name, "\uE8A5"),
                Tag = project.Id
            };
            ProjectList.Items.Add(item);
        }
    }

    private static StackPanel CreateProjectItemContent(string text, string glyph)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                new FontIcon
                {
                    Glyph = glyph,
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = text,
                    FontSize = 15,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }

    private void OnProjectSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectList.SelectedItem is ListViewItem item)
        {
            var projectId = item.Tag?.ToString();
            ProjectSelected?.Invoke(projectId);
        }
    }
}
