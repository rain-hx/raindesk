using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace rainspace.Util
{
    /// <summary>
    /// WinUI 3 简化版本的 Shell Context Menu
    /// 使用 P/Invoke 直接调用 Shell API
    /// </summary>
    public class ShellContextMenu
    {
        #region Constructor
        public ShellContextMenu()
        {
        }
        #endregion

        #region ShowContextMenu()

        public void ShowContextMenu(FileInfo[] files, System.Drawing.Point pointScreen)
        {
            if (files == null || files.Length == 0) return;
            ShowContextMenuAtPoint(files[0].FullName, pointScreen.X, pointScreen.Y);
        }

        public void ShowContextMenu(DirectoryInfo[] dirs, System.Drawing.Point pointScreen)
        {
            if (dirs == null || dirs.Length == 0) return;
            ShowContextMenuAtPoint(dirs[0].FullName, pointScreen.X, pointScreen.Y);
        }

        private void ShowContextMenuAtPoint(string path, int x, int y)
        {
            // Simplified shell context menu using ShellExecute
            var sei = new SHELLEXECUTEINFO();
            sei.cbSize = Marshal.SizeOf(sei);
            sei.fMask = 0x0000000C; // SEE_MASK_INVOKEIDLIST
            sei.lpVerb = "properties";
            sei.lpFile = path;
            sei.nShow = 1; // SW_SHOWNORMAL
            ShellExecuteEx(ref sei);
        }

        #endregion

        #region DLL Import

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public int fMask;
            public IntPtr hwnd;
            public string lpVerb;
            public string lpFile;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            public string lpClass;
            public IntPtr hkeyClass;
            public int dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        #endregion
    }

    public class ShellContextMenuException : Exception
    {
        public ShellContextMenuException()
        {
        }

        public ShellContextMenuException(string message)
            : base(message)
        {
        }
    }
}
