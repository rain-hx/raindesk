/*
* Virtual Router v1.0 - http://virtualrouter.codeplex.com
* Wifi Hot Spot for Windows 8, 7 and 2008 R2
* Copyright (c) 2013 Chris Pietschmann (http://pietschsoft.com)
* Licensed under the Microsoft Public License (Ms-PL)
* http://virtualrouter.codeplex.com/license
*/
using System;
using System.Runtime.InteropServices;

namespace rainspace.Util
{
    public static class AeroGlassHelper
    {
        #region "Generic Static Methods"

        public static void ExtendGlass(IntPtr windowHandle)
        {
            ExtendGlass(windowHandle, -1, -1, -1, -1);
        }

        public static void ExtendGlass(IntPtr windowHandle, int left, int right, int top, int bottom)
        {
            internalExtendGlass(windowHandle, left, right, top, bottom);
        }

        private static int internalExtendGlass(IntPtr windowHandle, int left, int right, int top, int bottom)
        {
            var retVal = -1; // Returning less than zero will indicate that Aero Glass could not be extended

            // Calculate the Aero Glass Margins
            Win32.Margins margins = Win32.GetDpiAdjustedMargins(windowHandle, left, right, top, bottom);

            try
            {
                // Actually Enable Aero Glass
                retVal = Win32.DwmExtendFrameIntoClientArea(windowHandle, ref margins);
            }
            catch (Exception)
            {
                retVal = -1;
            }

            return retVal;
        }

        #endregion

        #region "WinUI3 Methods"

        public static void ExtendGlass(IntPtr windowHandle, bool enableComposition)
        {
            if (enableComposition && Win32.DwmIsCompositionEnabled())
            {
                internalExtendGlass(windowHandle, -1, -1, -1, -1);
            }
        }

        #endregion

        #region "Win32 / pinvoke"

        private static class Win32
        {
            [DllImport("DwmApi.dll")]
            public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

            [DllImport("dwmapi.dll", PreserveSig = false)]
            public static extern bool DwmIsCompositionEnabled();


            [StructLayout(LayoutKind.Sequential)]
            public struct Margins
            {
                public int Left;    // width of left border that retains its size
                public int Right;   // width of right border that retains its size
                public int Top;     // height of top border that retains its size
                public int Bottom;  // height of bottom border that retains its size
            }

            public static Win32.Margins GetDpiAdjustedMargins(IntPtr windowHandle, int left, int right, int top, int bottom)
            {
                float DesktopDpiX;
                float DesktopDpiY;
                // Get System Dpi
                using (System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(windowHandle))
                {
                    DesktopDpiX = desktop.DpiX;
                    DesktopDpiY = desktop.DpiY;
                }

                // Set Margins
                Win32.Margins margins = new Win32.Margins();

                // Note that the default desktop Dpi is 96dpi. The  margins are
                // adjusted for the system Dpi.
                margins.Left = Convert.ToInt32(left * (DesktopDpiX / 96));
                margins.Right = Convert.ToInt32(right * (DesktopDpiX / 96));
                margins.Top = Convert.ToInt32(top * (DesktopDpiX / 96));
                margins.Bottom = Convert.ToInt32(bottom * (DesktopDpiX / 96));

                return margins;
            }
        }

        #endregion
    }
}
