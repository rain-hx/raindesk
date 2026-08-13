using System;
using System.Runtime.InteropServices;

namespace raindesk.Util
{
    class MouseUtil
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        /// <summary>
        /// 获取鼠标坐标
        /// </summary>
        /// <returns></returns>
        public static (double X, double Y) GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return (w32Mouse.X, w32Mouse.Y);
        }

        /// <summary>
        /// 获取鼠标位置（兼容旧接口）
        /// </summary>
        public static Windows.Foundation.Point GetMousePositionPoint()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Windows.Foundation.Point(w32Mouse.X, w32Mouse.Y);
        }
    }
}
