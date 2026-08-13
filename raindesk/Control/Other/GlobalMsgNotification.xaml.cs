using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace raindesk.Control.Other
{
    public sealed partial class GlobalMsgNotification : UserControl
    {
        public Window ntf;
        public GlobalMsgNotification(DialogMsg msg)
        {
            this.InitializeComponent();
            this.DataContext = msg;
        }

        public class DialogMsg
        {
            public string msg;
            public string title;

            public string Msg
            {
                get { return msg; }
                set { msg = value; }
            }
            public string Title
            {
                get { return title; }
                set { title = value; }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // WinUI 3: Close handled by parent
        }
    }
}
