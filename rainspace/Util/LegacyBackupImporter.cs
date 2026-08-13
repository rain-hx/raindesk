using System;
using System.IO;
using rainspace.ViewModel;

namespace rainspace.Util
{
    /// <summary>
    /// 检测旧版 GeekDesk BinaryFormatter .bak 备份文件。
    /// 由于安全风险（CVE-2020-0646 等），已移除 BinaryFormatter 反序列化支持。
    /// 旧版备份文件需要先用 GeekDesk 导出为 JSON 格式，再导入到 rainspace。
    /// </summary>
    internal static class LegacyBackupImporter
    {
        /// <summary>
        /// 检测文件是否为旧版 BinaryFormatter 格式。
        /// 如果是，记录日志并返回 null（不再尝试反序列化）。
        /// </summary>
        public static AppData? TryImport(string backupFilePath)
        {
            if (!File.Exists(backupFilePath)) return null;

            byte[] header = new byte[4];
            using (var stream = File.OpenRead(backupFilePath))
            {
                if (stream.Read(header, 0, header.Length) < header.Length) return null;
            }

            // BinaryFormatter stream starts with 0x00 0x01 0x00 0x00
            if (header[0] != 0x00 || header[1] != 0x01 || header[2] != 0x00 || header[3] != 0x00)
            {
                return null;
            }

            // 检测到旧版 BinaryFormatter 格式，但不再支持反序列化
            LogUtil.WriteLog($"检测到旧版 GeekDesk BinaryFormatter 备份文件: {backupFilePath}");
            LogUtil.WriteLog("由于安全风险，已移除 BinaryFormatter 反序列化支持。请使用 JSON 格式备份。");
            return null;
        }
    }
}
