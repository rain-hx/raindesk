using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace rainspace.Controls
{
    public sealed partial class SimpleColorPicker : UserControl
    {
        public Windows.UI.Color SelectedColor { get; private set; } = Colors.White;
        public event EventHandler<Windows.UI.Color> ColorConfirmed;
        public event EventHandler ColorCanceled;

        public SimpleColorPicker()
        {
            this.InitializeComponent();
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                try
                {
                    SelectedColor = HexToColor(hex);
                    ColorPreview.Background = new SolidColorBrush(SelectedColor);
                    HexInput.Text = hex;
                }
                catch { }
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedColor = HexToColor(HexInput.Text);
                ColorConfirmed?.Invoke(this, SelectedColor);
            }
            catch
            {
                // WinUI3: no MessageBox.Show, use ContentDialog instead
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ColorCanceled?.Invoke(this, EventArgs.Empty);
        }

        private static Windows.UI.Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return Windows.UI.Color.FromArgb(255, r, g, b);
            }
            else if (hex.Length == 8)
            {
                byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                return Windows.UI.Color.FromArgb(a, r, g, b);
            }
            throw new FormatException("Invalid hex color format");
        }
    }
}
