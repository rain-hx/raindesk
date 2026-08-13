using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace raindesk.Util
{
    public class HideWindowUtil
    {

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        public static void HideAltTab(IntPtr windowHandle)
        {
            try
            {
                int exStyle = (int)HideWindowUtil.GetWindowLong(windowHandle, (int)HideWindowUtil.GetWindowLongFields.GWL_EXSTYLE);
                exStyle |= (int)HideWindowUtil.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                HideWindowUtil.SetWindowLong(windowHandle, (int)HideWindowUtil.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
            } catch (Exception)
            {

            }
        }


        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        /// <summary>
        /// 扩展的窗口风格
        /// </summary>
        [Flags]
        public enum ExtendedWindowStyles : long
        {
            WS_EX_ACCEPTFILES = 0x00000010L,
            WS_EX_APPWINDOW = 0x00040000L,
            WS_EX_CLIENTEDGE = 0x00000200L,
            WS_EX_COMPOSITED = 0x02000000L,
            WS_EX_CONTEXTHELP = 0x00000400L,
            WS_EX_CONTROLPARENT = 0x00010000L,
            WS_EX_DLGMODALFRAME = 0x00000001L,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_LAYOUTRTL = 0x00400000L,
            WS_EX_LEFT = 0x00000000L,
            WS_EX_LEFTSCROLLBAR = 0x00004000L,
            WS_EX_LTRREADING = 0x00000000L,
            WS_EX_MDICHILD = 0x00000040L,
            WS_EX_NOACTIVATE = 0x08000000L,
            WS_EX_NOINHERITLAYOUT = 0x00100000L,
            WS_EX_NOPARENTNOTIFY = 0x00000004L,
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000L,
            WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),
            WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),
            WS_EX_RIGHT = 0x00001000L,
            WS_EX_RIGHTSCROLLBAR = 0x00000000L,
            WS_EX_RTLREADING = 0x00002000L,
            WS_EX_STATICEDGE = 0x00020000L,
            WS_EX_TOOLWINDOW = 0x00000080L,
            WS_EX_TOPMOST = 0x00000008L,
            WS_EX_TRANSPARENT = 0x00000020L,
            WS_EX_WINDOWEDGE = 0x00000100L
        }

    }
}
