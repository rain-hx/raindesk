using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

namespace rainspace.Util
{
    /// <summary>
    /// 首次使用引导辅助工具类
    /// </summary>
    public static class OnboardingHelper
    {
        private const string HasShownOnboardingKey = "HasShownOnboarding";

        /// <summary>
        /// 检查是否需要显示引导
        /// </summary>
        public static bool ShouldShowOnboarding()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey(HasShownOnboardingKey))
                {
                    return !(bool)localSettings.Values[HasShownOnboardingKey];
                }
                return true; // 首次运行
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 标记引导已显示
        /// </summary>
        public static void MarkOnboardingShown()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[HasShownOnboardingKey] = true;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "MarkOnboardingShown error");
            }
        }

        /// <summary>
        /// 显示首次使用引导
        /// </summary>
        public static async System.Threading.Tasks.Task ShowOnboardingAsync()
        {
            try
            {
                var mainWindow = App.MainWindow as MainWindow;
                if (mainWindow?.Content?.XamlRoot == null) return;

                // 第一步：欢迎
                var result1 = await ShowStep1_Welcome(mainWindow.Content.XamlRoot);
                if (result1 == ContentDialogResult.None) // 用户点击"跳过"
                {
                    MarkOnboardingShown();
                    return;
                }

                // 第二步：功能介绍
                var result2 = await ShowStep2_Features(mainWindow.Content.XamlRoot);
                if (result2 == ContentDialogResult.None) // 用户点击"跳过"
                {
                    MarkOnboardingShown();
                    return;
                }

                // 第三步：快捷键
                await ShowStep3_Shortcuts(mainWindow.Content.XamlRoot);

                // 标记引导已显示
                MarkOnboardingShown();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "ShowOnboardingAsync error");
            }
        }

        /// <summary>
        /// 第一步：欢迎
        /// </summary>
        private static async System.Threading.Tasks.Task<ContentDialogResult> ShowStep1_Welcome(Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = "欢迎使用 rainspace",
                Content = "rainspace 是一款高效的桌面快捷方式管理工具，帮助您快速启动应用、管理待办事项。\n\n让我们快速了解一下主要功能！",
                PrimaryButtonText = "下一步",
                CloseButtonText = "跳过",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// 第二步：功能介绍
        /// </summary>
        private static async System.Threading.Tasks.Task<ContentDialogResult> ShowStep2_Features(Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            var content = new StackPanel
            {
                Spacing = 16
            };

            content.Children.Add(CreateFeatureItem("🏠", "首页", "查看和管理您的快捷方式"));
            content.Children.Add(CreateFeatureItem("📝", "待办", "管理您的待办事项和任务"));
            content.Children.Add(CreateFeatureItem("⚙️", "设置", "自定义应用外观和行为"));
            content.Children.Add(CreateFeatureItem("🔍", "搜索", "快速搜索应用、路径和待办"));

            var dialog = new ContentDialog
            {
                Title = "主要功能",
                Content = content,
                PrimaryButtonText = "下一步",
                CloseButtonText = "跳过",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// 第三步：快捷键
        /// </summary>
        private static async System.Threading.Tasks.Task<ContentDialogResult> ShowStep3_Shortcuts(Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            var content = new StackPanel
            {
                Spacing = 12
            };

            content.Children.Add(CreateShortcutItem("Ctrl + F", "聚焦搜索框"));
            content.Children.Add(CreateShortcutItem("Ctrl + N", "添加新快捷方式"));
            content.Children.Add(CreateShortcutItem("Esc", "返回或关闭"));

            var dialog = new ContentDialog
            {
                Title = "快捷键",
                Content = content,
                PrimaryButtonText = "开始使用",
                CloseButtonText = "跳过",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// 创建功能介绍项
        /// </summary>
        private static StackPanel CreateFeatureItem(string icon, string title, string description)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = icon,
                        FontSize = 24,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = title,
                                FontSize = 16,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                            },
                            new TextBlock
                            {
                                Text = description,
                                FontSize = 14,
                                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SecondaryTextBrush"]
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 创建快捷键项
        /// </summary>
        private static StackPanel CreateShortcutItem(string key, string description)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    new Border
                    {
                        Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentLightBrush"],
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8, 4, 8, 4),
                        Child = new TextBlock
                        {
                            Text = key,
                            FontSize = 14,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["rainspaceBlueBrush"]
                        }
                    },
                    new TextBlock
                    {
                        Text = description,
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
        }
    }
}
