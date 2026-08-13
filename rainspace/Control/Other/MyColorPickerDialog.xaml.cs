using rainspace.Controls;
using rainspace.Control.Windows;
using rainspace.Util;
using rainspace.ViewModel;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using rainspace.Features.Appearance.Contracts;
using rainspace.Services;

namespace rainspace.Control.Other
{
    public enum ColorType
    {
        COLOR_1 = 1,
        COLOR_2 = 2,
        TEXT_COLOR = 3
    }

    /// <summary>
    /// WinUI 3 color picker dialog with preset colors.
    /// </summary>
    public partial class MyColorPickerDialog : UserControl
    {
        private readonly IAppearanceCommandHandler _appearanceCommands =
            AppServices.Get<IAppearanceCommandHandler>();

        public static Microsoft.UI.Xaml.Window? dialog;
        private static ColorType colorType;

        public MyColorPickerDialog()
        {
            InitializeComponent();
        }

        public MyColorPickerDialog(string strType, string token)
        {
            InitializeComponent();
            switch (strType)
            {
                case "Color1":
                    colorType = ColorType.COLOR_1; break;
                case "Color2":
                    colorType = ColorType.COLOR_2; break;
                default:
                    colorType = ColorType.TEXT_COLOR; break;
            }
            dialog = DialogService.Show(this, token);
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // Apply the color from the text box
            string hex = ColorTextBox.Text.Trim();
            try
            {
                var c = Converts.TextToColorConverter.ParseColor(hex);
                OnColorSelected(c);
            }
            catch { }
            dialog?.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            dialog?.Close();
        }

        private void OnColorSelected(global::Windows.UI.Color c)
        {
            string hex = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
            AppearanceColorTarget target = colorType switch
            {
                ColorType.COLOR_1 => AppearanceColorTarget.GradientStart,
                ColorType.COLOR_2 => AppearanceColorTarget.GradientEnd,
                _ => AppearanceColorTarget.Text
            };
            _appearanceCommands.SetColor(new SetAppearanceColorCommand(target, hex));
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                try
                {
                    var c = Converts.TextToColorConverter.ParseColor(hex);
                    ColorTextBox.Text = hex;
                    OnColorSelected(c);
                }
                catch { }
            }
        }
    }
}
