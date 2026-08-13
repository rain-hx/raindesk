using System;

namespace rainspace.Util
{
    public static class ScreenUtil
    {
        public static double GetScreenScalingFactor() => 1.0;
        public static bool IsPrimaryFullScreen() => false;
        public static System.Drawing.Color GetColorAt(System.Drawing.Point p) => System.Drawing.Color.Black;
    }
}
