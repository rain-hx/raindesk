using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace raindesk.Util
{
    /// <summary>
    /// 帮助文档辅助工具类
    /// </summary>
    public static class HelpHelper
    {
        /// <summary>
        /// 显示帮助对话框
        /// </summary>
        public static async System.Threading.Tasks.Task ShowHelpAsync()
        {
            try
            {
                var mainWindow = App.MainWindow as MainWindow;
                if (mainWindow?.Content?.XamlRoot == null) return;

                var content = CreateHelpContent();

                var dialog = new ContentDialog
                {
                    Title = "帮助文档",
                    Content = content,
                    CloseButtonText = "关闭",
                    XamlRoot = mainWindow.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "ShowHelpAsync error");
            }
        }

        /// <summary>
        /// 创建帮助内容
        /// </summary>
        private static ScrollViewer CreateHelpContent()
        {
            var stackPanel = new StackPanel
            {
                Spacing = 20,
                Padding = new Thickness(0, 0, 0, 0)
            };

            // 快速开始
            stackPanel.Children.Add(CreateSection("快速开始",
                "1. 右键点击主界面，选择「添加URL项目」或「添加系统项目」\n" +
                "2. 选择要添加的应用或输入网址\n" +
                "3. 双击图标即可快速启动"));

            // 快捷键
            stackPanel.Children.Add(CreateSection("快捷键",
                "Ctrl + F：聚焦搜索框\n" +
                "Ctrl + N：添加新快捷方式\n" +
                "Esc：返回或关闭"));

            // 分组管理
            stackPanel.Children.Add(CreateSection("分组管理",
                "• 点击「全部」按钮查看所有分组\n" +
                "• 点击「+」按钮创建新分组\n" +
                "• 右键点击分组可以重命名或删除\n" +
                "• 拖拽分组可以调整顺序"));

            // 待办事项
            stackPanel.Children.Add(CreateSection("待办事项",
                "• 点击左侧「待办」按钮进入待办管理\n" +
                "• 点击「+」按钮添加新任务\n" +
                "• 点击任务可以标记完成\n" +
                "• 右键点击任务可以编辑或删除"));

            // 设置
            stackPanel.Children.Add(CreateSection("设置",
                "• 点击左侧「设置」按钮进入设置\n" +
                "• 可以自定义背景图片或颜色\n" +
                "• 可以调整界面效果和动画\n" +
                "• 可以设置开机自启动"));

            // 常见问题
            stackPanel.Children.Add(CreateSection("常见问题",
                "Q: 如何备份数据？\n" +
                "A: 设置 → 其他 → 导出备份文件\n\n" +
                "Q: 如何恢复数据？\n" +
                "A: 设置 → 其他 → 导入备份文件\n\n" +
                "Q: 如何联系支持？\n" +
                "A: 设置 → 关于 → 联系我们"));

            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel,
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            return scrollViewer;
        }

        /// <summary>
        /// 创建帮助章节
        /// </summary>
        private static StackPanel CreateSection(string title, string content)
        {
            return new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppForegroundBrush"]
                    },
                    new TextBlock
                    {
                        Text = content,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SecondaryTextBrush"]
                    }
                }
            };
        }
    }
}
