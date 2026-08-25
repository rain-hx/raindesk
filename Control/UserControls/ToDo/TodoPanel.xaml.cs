using raindesk.Control.UserControls.ToDo;
using raindesk.Control.Windows;
using raindesk.Controls;
using raindesk.Util;
using raindesk.ViewModel;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace raindesk.Control.UserControls.Backlog
{
    public sealed partial class TodoPanel : UserControl
    {
        private string? activeFilterTag;
        private Storyboard? _tabIndicatorStoryboard;
        private Storyboard? _editorStoryboard;

        public TodoPanel()
        {
            InitializeComponent();
            Loaded += OnTodoPanelLoaded;
        }

        private void OnTodoPanelLoaded(object sender, RoutedEventArgs e)
        {
            ActiveTab.IsChecked = true;
            RefreshGlassBackdrop();
        }

        public void RefreshGlassBackdrop()
        {
            GlassBackdropUtil.ApplyCardGlass(RootCard);
        }

        public void Refresh()
        {
            TaskListView.RefreshList();
        }

        private void TodoTab_Checked(object sender, RoutedEventArgs e)
        {
            if (TaskListView == null) return;
            if (sender is not RadioButton item) return;

            var tag = item.Tag?.ToString();
            bool filterChanged = activeFilterTag != null && activeFilterTag != tag;
            activeFilterTag = tag;

            TaskListView.Visibility = Visibility.Visible;

            switch (tag)
            {
                case "Active":
                    TaskListView.Filter.StatusFilter = ToDoStatusFilter.Active;
                    TaskListView.Filter.DateFilter = ToDoDateFilter.All;
                    break;
                case "Completed":
                    TaskListView.Filter.StatusFilter = ToDoStatusFilter.Completed;
                    TaskListView.Filter.DateFilter = ToDoDateFilter.All;
                    break;
                case "All":
                    TaskListView.Filter.StatusFilter = ToDoStatusFilter.All;
                    TaskListView.Filter.DateFilter = ToDoDateFilter.All;
                    break;
            }
            TaskListView.RefreshList();
            AnimateSharedTabIndicator(item, animate: filterChanged && MotionPreferences.ShouldAnimate);
            if (filterChanged)
            {
                CrossFadeTaskList();
            }
        }

        private void CrossFadeTaskList()
        {
            _tabIndicatorStoryboard?.Stop();
            var storyboard = new Storyboard();
            var fadeIn = new DoubleAnimation
            {
                From = 0d,
                To = 1d,
                Duration = MotionTokens.Feedback,
                EasingFunction = MotionTokens.EaseOut()
            };
            Storyboard.SetTarget(fadeIn, TaskListView);
            Storyboard.SetTargetProperty(fadeIn, nameof(Opacity));
            TaskListView.Opacity = 0d;
            storyboard.Children.Add(fadeIn);
            _tabIndicatorStoryboard = storyboard;
            storyboard.Begin();
        }

        private void AnimateSharedTabIndicator(RadioButton selectedTab, bool animate)
        {
            if (SharedTabIndicator == null || TodoTabsPanel == null) return;

            double targetLeft = ComputeIndicatorLeft(selectedTab);
            double fromLeft = SharedTabIndicatorTransform.TranslateX;
            SharedTabIndicator.Visibility = Visibility.Visible;

            if (!animate)
            {
                SharedTabIndicatorTransform.TranslateX = targetLeft;
                return;
            }

            _tabIndicatorStoryboard?.Stop();
            var moveX = new DoubleAnimation
            {
                From = fromLeft,
                To = targetLeft,
                Duration = MotionTokens.Fast,
                EasingFunction = MotionTokens.EaseOut()
            };
            Storyboard.SetTarget(moveX, SharedTabIndicatorTransform);
            Storyboard.SetTargetProperty(moveX, nameof(CompositeTransform.TranslateX));
            _tabIndicatorStoryboard = new Storyboard();
            _tabIndicatorStoryboard.Children.Add(moveX);
            _tabIndicatorStoryboard.Begin();
        }

        private double ComputeIndicatorLeft(RadioButton selectedTab)
        {
            try
            {
                if (TodoTabsPanel.XamlRoot == null)
                {
                    return 0d;
                }

                // 相对标签容器计算选中标签的左边距，把指示器中心对齐到标签中心。
                GeneralTransform transform = selectedTab.TransformToVisual(TodoTabsPanel);
                Point topLeft = transform.TransformPoint(default);
                double indicatorWidth = SharedTabIndicator?.Width ?? 42d;
                return topLeft.X + (selectedTab.ActualWidth - indicatorWidth) / 2d;
            }
            catch
            {
                return 0d;
            }
        }

        private void CreateTodo_Click(object sender, RoutedEventArgs e)
        {
            ShowTaskEditor();
        }

        private void OnTaskClicked(ToDoTask task)
        {
            ShowTaskEditor(task);
        }

        private void ShowTaskEditor(ToDoTask? task = null)
        {
            TaskEditDialog editor;
            try
            {
                editor = task == null ? new TaskEditDialog() : new TaskEditDialog(task);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "打开任务编辑器失败");
                return;
            }

            editor.CloseRequested += TaskEditor_CloseRequested;
            TaskEditorHost.Content = editor;

            TaskListShell.Visibility = Visibility.Collapsed;
            TaskEditorHost.Visibility = Visibility.Visible;
            TaskEditorHost.Opacity = 0;
            ResetEditorTransform(translateX: MotionTokens.DisplacementPrimary, scale: 0.985);

            if (!MotionPreferences.ShouldAnimate)
            {
                TaskEditorHost.Opacity = 1;
                ResetEditorTransform();
                return;
            }

            _editorStoryboard?.Stop();
            var storyboard = new Storyboard();
            AddAnimation(storyboard, TaskEditorHost, nameof(Opacity), 1, 160);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.TranslateX), 0, 180);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleX), 1, 180);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleY), 1, 180);
            _editorStoryboard = storyboard;
            storyboard.Begin();
        }

        private void TaskEditor_CloseRequested(object? sender, EventArgs e)
        {
            HideTaskEditor(refreshList: true);
        }

        private void HideTaskEditor(bool refreshList)
        {
            if (TaskEditorHost.Visibility != Visibility.Visible)
            {
                return;
            }

            void Finish()
            {
                if (TaskEditorHost.Content is TaskEditDialog currentEditor)
                {
                    currentEditor.CloseRequested -= TaskEditor_CloseRequested;
                }

                TaskEditorHost.Content = null;
                TaskEditorHost.Visibility = Visibility.Collapsed;
                TaskEditorHost.Opacity = 0;
                ResetEditorTransform();
                TaskListShell.Visibility = Visibility.Visible;

                if (refreshList)
                {
                    TaskListView.RefreshList();
                }
            }

            if (!MotionPreferences.ShouldAnimate)
            {
                Finish();
                return;
            }

            _editorStoryboard?.Stop();
            var storyboard = new Storyboard();
            AddAnimation(storyboard, TaskEditorHost, nameof(Opacity), 0, 120);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.TranslateX), MotionTokens.DisplacementSecondary, 150);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleX), 0.99, 150);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleY), 0.99, 150);
            _editorStoryboard = storyboard;
            storyboard.Completed += (_, _) => Finish();
            storyboard.Begin();
        }

        private void ResetEditorTransform(double translateX = 0, double scale = 1)
        {
            TaskEditorTransform.TranslateX = translateX;
            TaskEditorTransform.TranslateY = 0;
            TaskEditorTransform.ScaleX = scale;
            TaskEditorTransform.ScaleY = scale;
        }

        private static void AddAnimation(Storyboard storyboard, DependencyObject target, string property, double to, int milliseconds)
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                EasingFunction = MotionTokens.EaseOut()
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
    }
}
