using rainspace.Control.UserControls.ToDo;
using rainspace.Control.Windows;
using rainspace.Controls;
using rainspace.Util;
using rainspace.ViewModel;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace rainspace.Control.UserControls.Backlog
{
    public sealed partial class TodoPanel : UserControl
    {
        private const int EditorEntranceDurationMs = 210;
        private const int EditorExitDurationMs = 150;
        private string? activeFilterTag;

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
            if (filterChanged)
            {
                DispatcherQueue.TryEnqueue(() =>
                    SurfaceMotion.PlayEntrance(TaskListView, translateY: 4d, duration: MotionTokens.Fast));
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
            ResetEditorTransform(translateX: 18, scale: 0.985);

            if (!MotionPreferences.ShouldAnimate)
            {
                TaskEditorHost.Opacity = 1;
                ResetEditorTransform();
                return;
            }

            var storyboard = new Storyboard();
            AddAnimation(storyboard, TaskEditorHost, nameof(Opacity), 1, 160);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.TranslateX), 0, EditorEntranceDurationMs);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleX), 1, EditorEntranceDurationMs);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleY), 1, EditorEntranceDurationMs);
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

            var storyboard = new Storyboard();
            AddAnimation(storyboard, TaskEditorHost, nameof(Opacity), 0, 120);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.TranslateX), 10, EditorExitDurationMs);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleX), 0.99, EditorExitDurationMs);
            AddAnimation(storyboard, TaskEditorTransform, nameof(CompositeTransform.ScaleY), 0.99, EditorExitDurationMs);
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
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
    }
}
