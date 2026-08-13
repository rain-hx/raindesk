using Microsoft.UI.Xaml;

namespace ShowSeconds
{
    public sealed partial class SecondsWindow : Window
    {
        private static SecondsWindow? _instance;

        public SecondsWindow()
        {
            this.InitializeComponent();
            rainspace.Services.ThemeService.RegisterWindow(this);
        }

        public static void ShowWindow()
        {
            if (_instance == null)
            {
                _instance = new SecondsWindow();
            }
            _instance.Activate();
        }

        public static void CloseWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
            }
        }

    }
}
