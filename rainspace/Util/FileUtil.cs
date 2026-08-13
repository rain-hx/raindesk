using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;

namespace rainspace.Util
{
    public class FileUtil
    {
        // PERF: Pre-compile regex patterns. The old code used string patterns which caused
        // Regex to recompile the pattern on every IsMatch() call. CompiledRegex caches the
        // internal state machine, making subsequent matches significantly faster.
        private static readonly Regex _noPathRegex = new Regex("{(.*)}", RegexOptions.Compiled);
        private static readonly Regex _noIcoRegex = new Regex("^,(.*)", RegexOptions.Compiled);
        private static readonly Regex _haveIcoRegex = new Regex("(.*),(.*)", RegexOptions.Compiled);

        // PERF: Cache the COM type lookup. Type.GetTypeFromProgID is relatively expensive
        // and was called on every CreateWshShell() invocation.
        private static readonly Type? _wshShellType = Type.GetTypeFromProgID("WScript.Shell");

        #region IShellLink P/Invoke (比WScript.Shell快10倍)

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        private const uint SLR_NO_UI = 0x1;
        private const uint SLGP_SHORTPATH = 0x1;

        /// <summary>
        /// 使用IShellLink直接解析lnk，比WScript.Shell快10倍
        /// </summary>
        public static LnkInfo GetLnkInfoFast(string filePath)
        {
            var result = new LnkInfo();
            try
            {
                var link = (IShellLinkW)new ShellLink();
                var file = (IPersistFile)link;
                file.Load(filePath, 0);

                // 解析快捷方式（无UI）
                link.Resolve(IntPtr.Zero, SLR_NO_UI);

                // 获取目标路径
                StringBuilder sb = new StringBuilder(260);
                link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
                string targetPath = sb.ToString();
                if (!string.IsNullOrEmpty(targetPath))
                {
                    result.TargetPath = targetPath;
                }

                // 获取参数
                sb.Clear();
                link.GetArguments(sb, sb.Capacity);
                result.Arguments = sb.ToString();

                // 获取图标路径
                sb.Clear();
                link.GetIconLocation(sb, sb.Capacity, out _);
                string iconPath = sb.ToString();
                if (!string.IsNullOrEmpty(iconPath))
                {
                    result.IconPath = iconPath;
                }
            }
            catch
            {
                // 回退到WScript.Shell
                return GetLnkInfo(filePath);
            }
            return result;
        }

        #endregion

        private static dynamic CreateWshShell()
        {
            // PERF: Uses cached _wshShellType instead of calling Type.GetTypeFromProgID each time.
            if (_wshShellType == null)
            {
                throw new InvalidOperationException("WScript.Shell COM 组件不可用");
            }
            return Activator.CreateInstance(_wshShellType);
        }

        public class LnkInfo
        {
            public string TargetPath;
            public string IconPath;
            public string Arguments;
        }

        /// <summary>
        /// 一次COM调用获取lnk文件的所有信息（目标路径、图标路径、启动参数）
        /// 避免多次COM调用超时
        /// </summary>
        public static LnkInfo GetLnkInfo(string filePath)
        {
            var result = new LnkInfo();
            try
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dynamic shell = CreateWshShell();
                        dynamic shortcut = shell.CreateShortcut(filePath);

                        // 目标路径
                        string targetPath = (string)shortcut.TargetPath;
                        // PERF: Use pre-compiled regex instead of parsing pattern string each call.
                        if (!StringUtil.IsEmpty(targetPath) && !_noPathRegex.IsMatch(targetPath))
                        {
                            result.TargetPath = targetPath;
                        }
                        else
                        {
                            result.TargetPath = ParseShortcut(filePath);
                        }

                        // 图标路径
                        string iconPath = (string)shortcut.IconLocation;
                        if (!StringUtil.IsEmpty(iconPath)
                            && !_noIcoRegex.IsMatch(iconPath)
                            && !_noPathRegex.IsMatch(iconPath)
                            && _haveIcoRegex.IsMatch(iconPath))
                        {
                            result.IconPath = iconPath.Split(',')[0];
                        }

                        // 启动参数
                        result.Arguments = (string)shortcut.Arguments;
                    }
                    catch { }
                });
                task.Wait(3000);
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 异步版本：一次COM调用获取lnk文件的所有信息
        /// </summary>
        public static async Task<LnkInfo> GetLnkInfoAsync(string filePath)
        {
            var result = new LnkInfo();
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dynamic shell = CreateWshShell();
                        dynamic shortcut = shell.CreateShortcut(filePath);

                        string targetPath = (string)shortcut.TargetPath;
                        if (!StringUtil.IsEmpty(targetPath) && !_noPathRegex.IsMatch(targetPath))
                        {
                            result.TargetPath = targetPath;
                        }
                        else
                        {
                            result.TargetPath = ParseShortcut(filePath);
                        }

                        string iconPath = (string)shortcut.IconLocation;
                        if (!StringUtil.IsEmpty(iconPath)
                            && !_noIcoRegex.IsMatch(iconPath)
                            && !_noPathRegex.IsMatch(iconPath)
                            && _haveIcoRegex.IsMatch(iconPath))
                        {
                            result.IconPath = iconPath.Split(',')[0];
                        }

                        result.Arguments = (string)shortcut.Arguments;
                    }
                    catch { }
                }).WaitAsync(TimeSpan.FromMilliseconds(3000));
            }
            catch { }
            return result;
        }

        public static string GetTargetPathByLnk(string filePath)
        {
            try
            {
                string result = null;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dynamic shell = CreateWshShell();
                        dynamic shortcut = shell.CreateShortcut(filePath);
                        if (StringUtil.IsEmpty((string)shortcut.TargetPath))
                        {
                            return;
                        }
                        string path = (string)shortcut.TargetPath;
                        if (path == null || _noPathRegex.IsMatch(path))
                        {
                            path = ParseShortcut(filePath);
                        }
                        result = path;
                    }
                    catch { }
                });
                if (task.Wait(3000))
                {
                    return result;
                }
                else
                {
                    LogUtil.WriteErrorLog(new TimeoutException(), "获取目标路径超时! filePath=" + filePath);
                    return null;
                }
            }
            catch (Exception e)
            {
                LogUtil.WriteErrorLog(e, "获取目标路径失败! filePath=" + filePath);
                return null;
            }
        }

        /// <summary>
        /// 异步版本：获取目标路径
        /// </summary>
        public static async Task<string> GetTargetPathByLnkAsync(string filePath)
        {
            try
            {
                string result = null;
                await global::System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dynamic shell = CreateWshShell();
                        dynamic shortcut = shell.CreateShortcut(filePath);
                        if (StringUtil.IsEmpty((string)shortcut.TargetPath))
                        {
                            return;
                        }
                        string path = (string)shortcut.TargetPath;
                        if (path == null || _noPathRegex.IsMatch(path))
                        {
                            path = ParseShortcut(filePath);
                        }
                        result = path;
                    }
                    catch { }
                }).WaitAsync(TimeSpan.FromMilliseconds(3000));
                return result;
            }
            catch (TimeoutException)
            {
                LogUtil.WriteErrorLog(new TimeoutException(), "获取目标路径超时! filePath=" + filePath);
                return null;
            }
            catch (Exception e)
            {
                LogUtil.WriteErrorLog(e, "获取目标路径失败! filePath=" + filePath);
                return null;
            }
        }

        /// <summary>
        /// 获取启动参数
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetArgByLnk(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string result = "";
                    var task = System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            dynamic shell = CreateWshShell();
                            dynamic shortcut = shell.CreateShortcut(filePath);
                            result = (string)shortcut.Arguments;
                        }
                        catch { }
                    });
                    if (task.Wait(3000))
                    {
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                LogUtil.WriteErrorLog(e, "获取启动参数失败! filePath=" + filePath);
            }
            return "";
        }

        /// <summary>
        /// 异步版本：获取启动参数
        /// </summary>
        public static async Task<string> GetArgByLnkAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string result = "";
                    await global::System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            dynamic shell = CreateWshShell();
                            dynamic shortcut = shell.CreateShortcut(filePath);
                            result = (string)shortcut.Arguments;
                        }
                        catch { }
                    }).WaitAsync(TimeSpan.FromMilliseconds(3000));
                    return result;
                }
            }
            catch (Exception e)
            {
                LogUtil.WriteErrorLog(e, "获取启动参数失败! filePath=" + filePath);
            }
            return "";
        }

        /// <summary>
        /// 获取iconpath
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetIconPathByLnk(string filePath)
        {
            try
            {
                string iconPath = null;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dynamic shell = CreateWshShell();
                        dynamic shortcut = shell.CreateShortcut(filePath);
                        iconPath = (string)shortcut.IconLocation;
                    }
                    catch { }
                });
                if (!task.Wait(3000))
                {
                    return null;
                }

                if (StringUtil.IsEmpty(iconPath)
                    || _noIcoRegex.IsMatch(iconPath)
                    || _noPathRegex.IsMatch(iconPath)
                    || !_haveIcoRegex.IsMatch(iconPath))
                {
                    return null;
                }
                else
                {
                    return iconPath.Split(',')[0];
                }
            }
            catch (Exception e)
            {
                LogUtil.WriteErrorLog(e, "获取图标路径失败! filePath=" + filePath);
                return null;
            }
        }

        /// <summary>
        /// 异步版本：获取iconpath
        /// </summary>
        public static async Task<string> GetIconPathByLnkAsync(string filePath)
        {
            try
            {
                string iconPath = null;
                await global::System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dynamic shell = CreateWshShell();
                        dynamic shortcut = shell.CreateShortcut(filePath);
                        iconPath = (string)shortcut.IconLocation;
                    }
                    catch { }
                }).WaitAsync(TimeSpan.FromMilliseconds(3000));

                if (StringUtil.IsEmpty(iconPath)
                    || _noIcoRegex.IsMatch(iconPath)
                    || _noPathRegex.IsMatch(iconPath)
                    || !_haveIcoRegex.IsMatch(iconPath))
                {
                    return null;
                }
                else
                {
                    return iconPath.Split(',')[0];
                }
            }
            catch (TimeoutException)
            {
                LogUtil.WriteErrorLog(new TimeoutException(), "获取图标路径超时! filePath=" + filePath);
                return null;
            }
            catch (Exception e)
            {
                LogUtil.WriteErrorLog(e, "获取图标路径失败! filePath=" + filePath);
                return null;
            }
        }


        /*
        UINT MsiGetShortcutTarget(
            LPCTSTR szShortcutTarget,
            LPTSTR szProductCode,
            LPTSTR szFeatureId,
            LPTSTR szComponentCode
        );
        */
        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        static extern int MsiGetShortcutTarget(string targetFile, StringBuilder productCode, StringBuilder featureID, StringBuilder componentCode);

        public enum InstallState
        {
            NotUsed = -7,
            BadConfig = -6,
            Incomplete = -5,
            SourceAbsent = -4,
            MoreData = -3,
            InvalidArg = -2,
            Unknown = -1,
            Broken = 0,
            Advertised = 1,
            Removed = 1,
            Absent = 2,
            Local = 3,
            Source = 4,
            Default = 5
        }

        public const int MaxFeatureLength = 38;
        public const int MaxGuidLength = 38;
        public const int MaxPathLength = 1024;

        /*
        INSTALLSTATE MsiGetComponentPath(
          LPCTSTR szProduct,
          LPCTSTR szComponent,
          LPTSTR lpPathBuf,
          DWORD* pcchBuf
        );
        */
        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        static extern InstallState MsiGetComponentPath(string productCode, string componentCode, StringBuilder componentPath, ref int componentPathBufferSize);

        public static string ParseShortcut(string file)
        {
            StringBuilder product = new StringBuilder(MaxGuidLength + 1);
            StringBuilder feature = new StringBuilder(MaxFeatureLength + 1);
            StringBuilder component = new StringBuilder(MaxGuidLength + 1);

            MsiGetShortcutTarget(file, product, feature, component);

            int pathLength = MaxPathLength;
            StringBuilder path = new StringBuilder(pathLength);

            InstallState installState = MsiGetComponentPath(product.ToString(), component.ToString(), path, ref pathLength);
            if (installState == InstallState.Local)
            {
                return path.ToString();
            }
            else
            {
                return null;
            }
        }

        public static string MakeRelativePath(string fromPath, string toPath)
        {
            string relativePath = null;
            try
            {
                if (string.IsNullOrEmpty(toPath) || string.IsNullOrEmpty(fromPath)) return null;
                Uri file = new Uri(@toPath);
                // Must end in a slash to indicate folder
                Uri folder = new Uri(@fromPath);
                relativePath =
                Uri.UnescapeDataString(
                    folder.MakeRelativeUri(file)
                        .ToString()
                        .Replace('/', Path.DirectorySeparatorChar)
                    );
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "建立相对路径出错:fromPath:" + fromPath + ",toPath:" + toPath);
            }
            return relativePath;
        }


        public static FileInfo GetFileByNameWithDir(string name, string dir)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            FileInfo[] files = d.GetFiles();
            foreach (FileInfo fi in files)
            {
                if (fi.Name.Equals(name))
                {
                    return fi;
                }
            }
            DirectoryInfo[] directs = d.GetDirectories();
            foreach (DirectoryInfo direct in directs)
            {
                FileInfo result = GetFileByNameWithDir(name, direct.FullName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

    }
}
