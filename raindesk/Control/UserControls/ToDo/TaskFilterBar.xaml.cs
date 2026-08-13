using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.ViewModel;

namespace raindesk.Control.UserControls.ToDo;

public sealed partial class TaskFilterBar : UserControl
{
    private ToDoFilter? _filter;

    public TaskFilterBar()
    {
        this.InitializeComponent();
    }

    public ToDoFilter? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            UpdateFilterUI();
        }
    }

    public event Action<ToDoFilter>? FilterChanged;

    private void OnStatusFilterChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && _filter != null)
        {
            _filter.StatusFilter = radioButton.Tag?.ToString() switch
            {
                "Active" => ToDoStatusFilter.Active,
                "Completed" => ToDoStatusFilter.Completed,
                _ => ToDoStatusFilter.All
            };
            FilterChanged?.Invoke(_filter);
        }
    }

    private void OnPriorityFilterChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && _filter != null)
        {
            _filter.PriorityFilter = radioButton.Tag?.ToString() switch
            {
                "High" => ToDoPriorityFilter.High,
                "Medium" => ToDoPriorityFilter.Medium,
                "Low" => ToDoPriorityFilter.Low,
                _ => ToDoPriorityFilter.All
            };
            FilterChanged?.Invoke(_filter);
        }
    }

    private void OnDateFilterChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && _filter != null)
        {
            _filter.DateFilter = radioButton.Tag?.ToString() switch
            {
                "Today" => ToDoDateFilter.Today,
                "ThisWeek" => ToDoDateFilter.ThisWeek,
                "Overdue" => ToDoDateFilter.Overdue,
                _ => ToDoDateFilter.All
            };
            FilterChanged?.Invoke(_filter);
        }
    }

    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox.SelectedItem is ComboBoxItem item && _filter != null)
        {
            _filter.SortBy = item.Tag?.ToString() switch
            {
                "Priority" => ToDoSortBy.Priority,
                "CreatedAt" => ToDoSortBy.CreatedAt,
                "Manual" => ToDoSortBy.Manual,
                _ => ToDoSortBy.DueDate
            };
            FilterChanged?.Invoke(_filter);
        }
    }

    private void UpdateFilterUI()
    {
        if (_filter == null) return;

        StatusAll.IsChecked = _filter.StatusFilter == ToDoStatusFilter.All;
        StatusActive.IsChecked = _filter.StatusFilter == ToDoStatusFilter.Active;
        StatusCompleted.IsChecked = _filter.StatusFilter == ToDoStatusFilter.Completed;

        PriorityAll.IsChecked = _filter.PriorityFilter == ToDoPriorityFilter.All;
        PriorityHigh.IsChecked = _filter.PriorityFilter == ToDoPriorityFilter.High;
        PriorityMedium.IsChecked = _filter.PriorityFilter == ToDoPriorityFilter.Medium;
        PriorityLow.IsChecked = _filter.PriorityFilter == ToDoPriorityFilter.Low;

        DateAll.IsChecked = _filter.DateFilter == ToDoDateFilter.All;
        DateToday.IsChecked = _filter.DateFilter == ToDoDateFilter.Today;
        DateThisWeek.IsChecked = _filter.DateFilter == ToDoDateFilter.ThisWeek;
        DateOverdue.IsChecked = _filter.DateFilter == ToDoDateFilter.Overdue;

        SortComboBox.SelectedIndex = _filter.SortBy switch
        {
            ToDoSortBy.Priority => 1,
            ToDoSortBy.CreatedAt => 2,
            ToDoSortBy.Manual => 3,
            _ => 0
        };
    }
}
