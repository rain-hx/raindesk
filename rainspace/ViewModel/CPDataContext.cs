using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace rainspace.ViewModel
{
    public class CPDataContext : INotifyPropertyChanged
    {
        private BitmapSource pixelIMG;

        private System.Drawing.Color pixelColor_D;

        public Color pixelColor;

        private string colorHtml;

        private string colorRGB;

        private string pixelXY;

        public BitmapSource PixelIMG
        {
            set => SetProperty(ref pixelIMG, value, nameof(PixelIMG));
            get { return pixelIMG; }
        }

        public System.Drawing.Color PixelColor_D
        {
            set
            {
                if (pixelColor_D == value)
                {
                    return;
                }

                pixelColor_D = value;
                ColorHtml = pixelColor_D.Name.ToUpper().Substring(2);
                ColorRGB = pixelColor_D.R + "," + pixelColor_D.G + "," + pixelColor_D.B;
                PixelColor = Color.FromArgb(pixelColor_D.A, pixelColor_D.R, pixelColor_D.G, pixelColor_D.B);
            }
            get { return pixelColor_D; }
        }

        public Color PixelColor
        {
            set => SetProperty(ref pixelColor, value, nameof(PixelColor));
            get { return pixelColor; }
        }

        public string ColorRGB
        {
            set => SetProperty(ref colorRGB, value, nameof(ColorRGB));
            get { return colorRGB; }
        }

        public string ColorHtml
        {
            set => SetProperty(ref colorHtml, value, nameof(ColorHtml));
            get { return colorHtml; }
        }

        public string PixelXY
        {
            set => SetProperty(ref pixelXY, value, nameof(PixelXY));
            get { return pixelXY; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
