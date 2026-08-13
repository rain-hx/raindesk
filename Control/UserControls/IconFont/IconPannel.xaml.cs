using Microsoft.UI.Xaml.Controls;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Control.UserControls.IconFont
{
    /// <summary>
    /// IconPannel.xaml 的交互逻辑
    /// </summary>
    public partial class IconPannel : UserControl
    {
        public IconPannel()
        {
            this.InitializeComponent();
        }

        private void IconListBox_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue || args.Item is not IconfontInfo icon)
            {
                return;
            }

            if (args.Phase == 0)
            {
                args.RegisterUpdateCallback(IconListBox_ContainerContentChanging);
                return;
            }

            if (args.ItemContainer?.ContentTemplateRoot is Microsoft.UI.Xaml.DependencyObject root)
            {
                var iconPath = MenuGeometryHelper.FindDescendantByName<Microsoft.UI.Xaml.Shapes.Path>(root, "IconPreviewPath");
                MenuGeometryHelper.ApplyToPath(iconPath, icon.Text);
            }
        }
    }
}
