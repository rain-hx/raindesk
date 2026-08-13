using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

namespace raindesk.Constant
{
    public partial class Constants
    {
        public static string APP_DIR = AppDomain.CurrentDomain.BaseDirectory.Trim();

        // 用户数据目录（敏感文件存储在这里，避免多用户共享）
        private static string _userDataDir;
        public static string USER_DATA_DIR
        {
            get
            {
                if (string.IsNullOrEmpty(_userDataDir))
                {
                    _userDataDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "raindesk");
                    System.IO.Directory.CreateDirectory(_userDataDir);
                }
                return _userDataDir;
            }
        }

        // 是否为开发模式
        public static bool DEV = false;

        public static string MY_NAME = DEV ? "raindesk-D" : "raindesk";

        public static string MY_UUID = "8400A17AEEF7C029";
        // G1: 以下路径全部迁移到 %LOCALAPPDATA%\raindesk\，避免 MSIX 安装目录写入失败。
        // 从 static field 改为 static property，确保 USER_DATA_DIR 已初始化后再计算。

        /// <summary>
        /// app数据文件路径
        /// </summary>
        public static string DATA_FILE_PATH => System.IO.Path.Combine(USER_DATA_DIR, "Data");

        /// <summary>
        /// 备份文件路径
        /// </summary>
        public static string DATA_FILE_BAK_DIR_PATH => System.IO.Path.Combine(USER_DATA_DIR, "bak");
        public static string DATA_FILE_TEMP_DIR_PATH => System.IO.Path.Combine(USER_DATA_DIR, "temp");

        // 敏感文件存储在用户数据目录（LocalApplicationData），避免多用户共享
        public static string PW_FILE_BAK_PATH => System.IO.Path.Combine(USER_DATA_DIR, "bak", "pw.txt");  //密码文件路径

        public static string UUID_FILE_BAK_PATH => System.IO.Path.Combine(USER_DATA_DIR, "bak", "uuid.txt");  //uuid文件路径

        public static string LOG_FILE_PATH => System.IO.Path.Combine(USER_DATA_DIR, "logs", "log.log"); //日志文件

        public static string ERROR_FILE_PATH => System.IO.Path.Combine(USER_DATA_DIR, "logs", "error.log"); // 错误日志

        /// <summary>
        /// 插件文件夹
        /// </summary>
        public static string PLUGINS_PATH => System.IO.Path.Combine(USER_DATA_DIR, "plugins") + System.IO.Path.DirectorySeparatorChar;


        public static int SHADOW_WIDTH = 20;

        //系统图标
        public static Dictionary<string, string> SYSTEM_ICONS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".txt", "txt.ico" },
            { ".doc", "doc.ico" },
            { ".docx", "doc.ico" },
            { ".xls", "xls.ico" },
            { ".xlsx", "xls.ico" },
            { ".ppt", "ppt.ico" },
            { ".pptx", "ppt.ico" },
            { ".pdf", "pdf.ico" },
            { ".html", "html.ico" },
            { ".htm", "html.ico" },
            { ".zip", "zip.ico" },
            { ".rar", "rar.ico" },
            { ".7z", "7z.ico" },
            { ".mp3", "mp3.ico" },
            { ".wav", "wav.ico" },
            { ".mp4", "mp4.ico" },
            { ".avi", "avi.ico" },
            { ".mkv", "mkv.ico" },
            { ".jpg", "jpg.ico" },
            { ".jpeg", "jpg.ico" },
            { ".png", "png.ico" },
            { ".gif", "gif.ico" },
            { ".bmp", "bmp.ico" },
            { ".ico", "ico.ico" },
            { ".svg", "svg.ico" },
            { ".psd", "psd.ico" },
            { ".exe", "exe.ico" },
            { ".msi", "msi.ico" },
            { ".bat", "bat.ico" },
            { ".cmd", "cmd.ico" },
            { ".lnk", "lnk.ico" },
            { ".iso", "iso.ico" },
            { ".dll", "dll.ico" },
            { ".js", "js.ico" },
            { ".css", "css.ico" },
            { ".json", "json.ico" },
            { ".xml", "xml.ico" },
            { ".md", "md.ico" },
            { ".py", "py.ico" },
            { ".java", "java.ico" },
            { ".cs", "cs.ico" },
            { ".c", "c.ico" },
            { ".cpp", "cpp.ico" },
            { ".h", "h.ico" },
            { ".php", "php.ico" },
            { ".rb", "rb.ico" },
            { ".go", "go.ico" },
            { ".rs", "rs.ico" },
            { ".swift", "swift.ico" },
            { ".kt", "kt.ico" },
            { ".ts", "ts.ico" },
            { ".sql", "sql.ico" },
            { ".db", "db.ico" },
            { ".ttf", "ttf.ico" },
            { ".otf", "otf.ico" },
            { ".woff", "woff.ico" },
            { ".woff2", "woff2.ico" },
        };

        //系统应用名称 (从App.config读取)
        private static Hashtable _systemAppNames;
        public static Hashtable SYSTEM_APP_NAMES
        {
            get
            {
                if (_systemAppNames == null)
                {
                    _systemAppNames = new Hashtable();
                    try
                    {
                        var section = ConfigurationManager.GetSection("SystemIcons") as NameValueCollection;
                        if (section != null)
                        {
                            foreach (string key in section.AllKeys)
                            {
                                _systemAppNames[key] = section[key];
                            }
                        }
                    }
                    catch { }
                    // 如果配置读取失败，使用硬编码的默认值
                    if (_systemAppNames.Count == 0)
                    {
                        _systemAppNames["Calculator"] = "计算器";
                        _systemAppNames["Computer"] = "此电脑";
                        _systemAppNames["GroupPolicy"] = "组策略";
                        _systemAppNames["Network"] = "网络连接";
                        _systemAppNames["RecycleBin"] = "回收站";
                        _systemAppNames["Registry"] = "注册表编辑器";
                        _systemAppNames["Notepad"] = "记事本";
                        _systemAppNames["Mstsc"] = "远程桌面连接";
                        _systemAppNames["Control"] = "控制面板";
                        _systemAppNames["CMD"] = "命令行";
                        _systemAppNames["Services"] = "服务";
                    }
                }
                return _systemAppNames;
            }
        }

        //显秒设置
        public static Dictionary<string, string> SHOW_SECONDS_SETTINGS = new Dictionary<string, string>
        {
            { "Version", "1.0.01" },
            { "LProportion", "0.82" },
            { "TProportion", "0.03" },
            { "DelayTime", "1500" }
        };
    }
}
