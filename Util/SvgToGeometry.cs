using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using raindesk.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace raindesk.Util
{
    public static class SvgToGeometry
    {
        private static readonly object IconfontCacheLock = new();
        private static string? cachedIconfontKey;
        private static List<IconfontInfo>? cachedIconfonts;

        public static List<IconfontInfo> GetIconfonts()
        {
            string baseDir = AppContext.BaseDirectory;
            string iconfontDir = Path.Combine(baseDir, "Resource", "Iconfont");
            string svgPath = Path.Combine(iconfontDir, "iconfont.js");
            string jsonPath = Path.Combine(iconfontDir, "iconfont.json");
            string cacheKey = BuildIconfontCacheKey(svgPath, jsonPath);

            lock (IconfontCacheLock)
            {
                if (cachedIconfontKey == cacheKey && cachedIconfonts != null)
                {
                    return new List<IconfontInfo>(cachedIconfonts);
                }
            }

            string svgJsStr = File.ReadAllText(svgPath);
            string jsonStr = File.ReadAllText(jsonPath);
            List<IconfontInfo> parsed = GetIconfonts(svgJsStr, jsonStr);

            lock (IconfontCacheLock)
            {
                cachedIconfontKey = cacheKey;
                cachedIconfonts = new List<IconfontInfo>(parsed);
            }

            return parsed;
        }

        public static List<IconfontInfo> GetIconfonts(string svgJsStr, string jsonStr)
        {
            return GetIconfonts(svgJsStr, JObject.Parse(jsonStr));
        }

        public static List<IconfontInfo> GetIconfonts(string svgJsStr, JObject json)
        {
            int svgStart = svgJsStr.IndexOf("<svg>", StringComparison.Ordinal);
            int svgEnd = svgJsStr.IndexOf("</svg>", StringComparison.Ordinal);
            if (svgStart < 0 || svgEnd < svgStart)
            {
                return new List<IconfontInfo>();
            }

            svgJsStr = svgJsStr.Substring(svgStart, svgEnd + "</svg>".Length - svgStart);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(svgJsStr);
            XmlNodeList? nodeList = xmlDoc.SelectNodes("/svg/symbol");

            JArray glyphs = json["glyphs"] as JArray ?? new JArray();

            List<IconfontInfo> listInfo = new List<IconfontInfo>();
            if (nodeList == null)
            {
                return listInfo;
            }

            int count = Math.Min(nodeList.Count, glyphs.Count);
            for (int i = 0; i < count; i++)
            {
                XmlNodeList? pathNodes = nodeList[i]?.SelectNodes("path");
                var pathData = new StringBuilder();

                if (pathNodes == null)
                {
                    continue;
                }

                foreach (XmlNode pathNode in pathNodes)
                {
                    string? value = pathNode.Attributes?["d"]?.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        pathData.Append(value);
                    }
                }

                string? name = glyphs[i]?["name"]?.ToString();
                if (pathData.Length == 0 || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                listInfo.Add(new IconfontInfo
                {
                    Text = pathData.ToString(),
                    Name = name
                });
            }
            return listInfo;
        }

        private static string BuildIconfontCacheKey(string svgPath, string jsonPath)
        {
            var svgInfo = new FileInfo(svgPath);
            var jsonInfo = new FileInfo(jsonPath);
            return string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{svgInfo.FullName}|{svgInfo.Length}|{svgInfo.LastWriteTimeUtc.Ticks}|{jsonInfo.FullName}|{jsonInfo.Length}|{jsonInfo.LastWriteTimeUtc.Ticks}");
        }

        public static string GetMd5Str(string convertString)
        {
            using MD5 md5 = MD5.Create();
            string hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(convertString)), 4, 8);
            return hash.Replace("-", "").ToLowerInvariant();
        }
    }
}
