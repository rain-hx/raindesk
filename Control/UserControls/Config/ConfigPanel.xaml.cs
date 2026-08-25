using raindesk.Features.Settings.Contracts;
using raindesk.Features.Settings.Presentation;
using raindesk.Controls;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace raindesk.Control.UserControls.Config
{
    public sealed partial class ConfigPanel : UserControl
    {
        private readonly ISettingsSectionCatalog sectionCatalog;
        private readonly SettingsPageResolver pageResolver;
        private readonly IAppDataProvider dataProvider;
        private readonly Dictionary<string, FrameworkElement> pages =
            new(StringComparer.OrdinalIgnoreCase);
        private AppConfig observedConfig;
        private FrameworkElement? currentPage;

        public ConfigPanel()
        {
            sectionCatalog = AppServices.Get<ISettingsSectionCatalog>();
            pageResolver = AppServices.Get<SettingsPageResolver>();
            dataProvider = AppServices.Get<IAppDataProvider>();
            InitializeComponent();
            BuildTabs();
            Loaded += ConfigPanel_Loaded;
            Unloaded += ConfigPanel_Unloaded;
            DataContextChanged += ConfigPanel_DataContextChanged;
            ShowPanel("Theme");
        }

        private void ConfigPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ObserveConfig(DataContext as AppConfig ?? dataProvider.Current.AppConfig);
        }

        private void ConfigPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            ObserveConfig(null);
        }

        private void ConfigPanel_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ObserveConfig(args.NewValue as AppConfig ?? dataProvider.Current.AppConfig);
        }

        private void ObserveConfig(AppConfig config)
        {
            if (ReferenceEquals(observedConfig, config))
            {
                RefreshGlassBackdrop();
                return;
            }

            if (observedConfig != null)
            {
                observedConfig.PropertyChanged -= Config_PropertyChanged;
            }

            observedConfig = config;
            if (observedConfig != null)
            {
                observedConfig.PropertyChanged += Config_PropertyChanged;
            }

            RefreshGlassBackdrop();
        }

        private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppConfig.BlurEffect) || e.PropertyName == nameof(AppConfig.BlurValue))
            {
                RefreshGlassBackdrop();
            }
        }

        public void RefreshGlassBackdrop()
        {
            GlassBackdropUtil.ApplyCardGlass(RootGlass);
        }

        private void MenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not ListBox listBox || listBox.SelectedItem is not FrameworkElement item)
                {
                    return;
                }

                ShowPanel(item.Tag?.ToString());
            }
            catch
            {
            }
        }

        private void ShowPanel(string tag)
        {
            if (!pages.TryGetValue(tag, out FrameworkElement? page))
            {
                page = pageResolver.Create(tag);
                if (page != null)
                {
                    pages[tag] = page;
                }
            }
            if (page != null)
            {
                if (ReferenceEquals(currentPage, page))
                {
                    return;
                }

                RightCard.Child = page;
                bool shouldAnimate = currentPage != null;
                currentPage = page;
                if (shouldAnimate)
                {
                    DispatcherQueue.TryEnqueue(() =>
                        SurfaceMotion.PlaySecondaryEntrance(RightCard, translateX: MotionTokens.DisplacementSecondary, translateY: 0d, duration: MotionTokens.Fast));
                }
            }
        }

        private void BuildTabs()
        {
            ConfigTabs.Items.Clear();
            foreach (SettingsSectionDescriptor section in sectionCatalog.Sections)
            {
                ConfigTabs.Items.Add(CreateTab(section.Id, section.Title, section.IconGlyph));
            }

            ConfigTabs.SelectedIndex = sectionCatalog.Sections.Count > 0 ? 0 : -1;
        }

        private static ListBoxItem CreateTab(string id, string title, string glyph)
        {
            return new ListBoxItem
            {
                Tag = id,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FontIcon { Glyph = glyph, FontSize = 16 },
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 14,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        }
                    }
                }
            };
        }
    }
}
