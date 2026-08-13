using Microsoft.UI;
using Windows.UI;

namespace rainspace.Util
{
    public static class ColorHelper
    {
        public static Color FromString(string colorStr)
        {
            try
            {
                string hex = colorStr.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                return Colors.Black;
            }
            catch { return Colors.Black; }
        }

        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return Color.FromArgb(a, r, g, b);
        }
    }
}
