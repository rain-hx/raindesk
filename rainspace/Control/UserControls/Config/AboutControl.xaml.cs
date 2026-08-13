using rainspace.Constant;
using rainspace.Util;
using System.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace rainspace.Control.UserControls.Config
{
    /// <summary>
    /// AboutControl.xaml 的交互逻辑
    /// </summary>
    public partial class AboutControl : UserControl
    {
        public AboutControl()
        {
            this.InitializeComponent();
            AppInfo.Text += ConfigurationManager.AppSettings["Version"];
            string showPublicWeChat = ConfigurationManager.AppSettings["ShowPublicWeChat"];
            if ("Y".Equals(showPublicWeChat))
            {
                PublicWeChatPanel.Visibility = Visibility.Visible;
            } else
            {
                PublicWeChatPanel.Visibility = Visibility.Collapsed;
            }

            PublicWeChat.Source = ImageUtil.Base64ToBitmapImage(Constants.PUBLIC_WE_CHAT_IMG_BASE64);
            WeChatCode.Source = ImageUtil.Base64ToBitmapImage(Constants.WE_CHAT_CODE_IMG_BASE64);
            ZFBCode.Source = ImageUtil.Base64ToBitmapImage(Constants.ZFB_CODE_IMG_BASE64);
        }


        private void SC_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        }
        private void SC_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
        }

        private async void SC_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string url && !string.IsNullOrEmpty(url))
            {
                await Launcher.LaunchUriAsync(new System.Uri(url));
            }
        }
    }
}
