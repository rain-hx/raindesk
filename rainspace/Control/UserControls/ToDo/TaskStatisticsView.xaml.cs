using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using rainspace.Controls;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace rainspace.Control.UserControls.ToDo;

/// <summary>
/// 任务统计视图控件。
/// 显示任务统计信息和图表。
/// </summary>
public sealed partial class TaskStatisticsView : UserControl
{
    private static readonly SolidColorBrush TrendBrush = new(ColorHelper.FromArgb(255, 52, 152, 219));
    private static readonly SolidColorBrush MutedTextBrush = new(ColorHelper.FromArgb(255, 103, 119, 150));
    private static readonly SolidColorBrush BarTrackBrush = new(ColorHelper.FromArgb(34, 0, 0, 0));

    private static readonly Dictionary<TaskPriority, (string Label, SolidColorBrush Brush)> PriorityColors = new()
    {
        [TaskPriority.Critical] = ("紧急", new SolidColorBrush(ColorHelper.FromArgb(255, 231, 76, 60))),
        [TaskPriority.High] = ("高", new SolidColorBrush(ColorHelper.FromArgb(255, 230, 126, 34))),
        [TaskPriority.Medium] = ("中", TrendBrush),
        [TaskPriority.Low] = ("低", new SolidColorBrush(ColorHelper.FromArgb(255, 149, 165, 166))),
    };

    private static readonly SolidColorBrush[] ProjectBarBrushes =
    [
        TrendBrush,
        new SolidColorBrush(ColorHelper.FromArgb(255, 46, 204, 113)),
        new SolidColorBrush(ColorHelper.FromArgb(255, 230, 126, 34)),
        new SolidColorBrush(ColorHelper.FromArgb(255, 155, 89, 182)),
        new SolidColorBrush(ColorHelper.FromArgb(255, 26, 188, 156)),
        PriorityColors[TaskPriority.Critical].Brush
    ];

    private IToDoService? _todoService;

    public TaskStatisticsView()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _todoService = AppServices.TryGet<IToDoService>();
        LoadStatistics();
    }

    public void Refresh()
    {
        LoadStatistics();
    }

    private void LoadStatistics()
    {
        if (_todoService == null) return;

        var tasks = _todoService.GetAll().ToList();
        var stats = new TaskStatistics(tasks);

        TotalCountText.Text = stats.TotalCount.ToString();
        ActiveCountText.Text = stats.ActiveCount.ToString();
        CompletedCountText.Text = stats.CompletedCount.ToString();
        OverdueCountText.Text = stats.OverdueCount.ToString();

        var rate = stats.CompletionRate * 100;
        CompletionRateText.Text = $"{rate:F0}%";
        CompletionRateBar.Value = rate;

        RenderTrendChart(stats.GetCompletionTrend(7));
        RenderPriorityChart(stats.GetPriorityDistribution());
        RenderProjectChart(stats.GetProjectDistribution());
    }

    private void RenderTrendChart(List<DailyCompletion> trend)
    {
        TrendCanvas.Children.Clear();
        TrendLabels.Children.Clear();

        var hasData = trend.Any(t => t.Count > 0);
        TrendEmptyText.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        TrendChart.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        if (!hasData) return;

        var maxCount = Math.Max(trend.Max(t => t.Count), 1);
        var canvasWidth = TrendCanvas.ActualWidth;
        var canvasHeight = TrendCanvas.ActualHeight;

        if (canvasWidth <= 0) canvasWidth = 600;
        if (canvasHeight <= 0) canvasHeight = 140;

        var barCount = trend.Count;
        var totalSpacing = canvasWidth * 0.3;
        var barSpacing = totalSpacing / (barCount + 1);
        var barWidth = (canvasWidth - totalSpacing) / barCount;

        for (int i = 0; i < barCount; i++)
        {
            var item = trend[i];
            var barHeight = item.Count > 0
                ? Math.Max((double)item.Count / maxCount * (canvasHeight - 20), 4)
                : 0;

            var x = barSpacing + i * (barWidth + barSpacing);

            if (barHeight > 0)
            {
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = TrendBrush,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, canvasHeight - barHeight);
                TrendCanvas.Children.Add(rect);
                AnimateChartElement(rect, i * 18);

                var countLabel = new TextBlock
                {
                    Text = item.Count.ToString(),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = barWidth
                };
                Canvas.SetLeft(countLabel, x);
                Canvas.SetTop(countLabel, canvasHeight - barHeight - 18);
                TrendCanvas.Children.Add(countLabel);
                AnimateChartElement(countLabel, i * 18);
            }

            var dateLabel = new TextBlock
            {
                Text = item.Date.ToString("MM/dd"),
                FontSize = 11,
                Foreground = MutedTextBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = barWidth + barSpacing
            };
            TrendLabels.Children.Add(dateLabel);
        }
    }

    private void RenderPriorityChart(Dictionary<TaskPriority, int> distribution)
    {
        PriorityChart.Children.Clear();

        var hasData = distribution.Count > 0;
        PriorityEmptyText.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        PriorityChart.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        if (!hasData) return;

        var total = distribution.Values.Sum();
        var orderedPriorities = new[] { TaskPriority.Critical, TaskPriority.High, TaskPriority.Medium, TaskPriority.Low };

        foreach (var priority in orderedPriorities)
        {
            if (!distribution.TryGetValue(priority, out var count)) continue;

            var (label, brush) = PriorityColors[priority];
            var pct = (double)count / total * 100;

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 0);
            row.Children.Add(labelText);

            var barContainer = new Grid
            {
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(8, 0, 8, 0)
            };
            var bgBar = new Rectangle
            {
                Fill = BarTrackBrush,
                RadiusX = 3,
                RadiusY = 3,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            barContainer.Children.Add(bgBar);
            var bar = new Rectangle
            {
                Width = Math.Max(pct / 100 * 200, 4),
                Fill = brush,
                RadiusX = 3,
                RadiusY = 3,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            barContainer.Children.Add(bar);
            AnimateChartElement(bar, PriorityChart.Children.Count * 24);
            Grid.SetColumn(barContainer, 1);
            row.Children.Add(barContainer);

            var countLabel = new TextBlock
            {
                Text = $"{count} ({pct:F0}%)",
                FontSize = 12,
                Foreground = MutedTextBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(countLabel, 2);
            row.Children.Add(countLabel);

            PriorityChart.Children.Add(row);
        }
    }

    private void RenderProjectChart(Dictionary<string, int> distribution)
    {
        ProjectChart.Children.Clear();

        var hasData = distribution.Count > 0;
        ProjectEmptyText.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        ProjectChart.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        if (!hasData) return;

        var total = distribution.Values.Sum();
        var colorIndex = 0;

        foreach (var kvp in distribution.OrderByDescending(x => x.Value))
        {
            var pct = (double)kvp.Value / total * 100;
            var brush = ProjectBarBrushes[colorIndex % ProjectBarBrushes.Length];
            colorIndex++;

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameText = new TextBlock
            {
                Text = kvp.Key,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameText, 0);
            row.Children.Add(nameText);

            var barContainer = new Grid
            {
                Height = 20,
                Margin = new Thickness(8, 0, 0, 0)
            };
            var bgBar = new Rectangle
            {
                Fill = BarTrackBrush,
                RadiusX = 3,
                RadiusY = 3,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            barContainer.Children.Add(bgBar);
            var bar = new Rectangle
            {
                Width = Math.Max(pct / 100 * 60, 4),
                Fill = brush,
                RadiusX = 3,
                RadiusY = 3,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            barContainer.Children.Add(bar);
            AnimateChartElement(bar, ProjectChart.Children.Count * 20);

            var countLabel = new TextBlock
            {
                Text = kvp.Value.ToString(),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 0)
            };
            barContainer.Children.Add(countLabel);
            Grid.SetColumn(barContainer, 1);
            row.Children.Add(barContainer);

            ProjectChart.Children.Add(row);
        }
    }

    private static void AnimateChartElement(UIElement element, int delayMs)
    {
        if (!MotionPreferences.ShouldAnimate)
        {
            return;
        }

        element.Opacity = 0.72;
        element.RenderTransform = new TranslateTransform { Y = 4 };

        var fade = new DoubleAnimation
        {
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, element);
        Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));

        var move = new DoubleAnimation
        {
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(move, element.RenderTransform);
        Storyboard.SetTargetProperty(move, nameof(TranslateTransform.Y));

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(move);
        storyboard.Begin();
    }
}
