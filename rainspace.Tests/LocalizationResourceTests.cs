using System.Xml.Linq;

namespace rainspace.Tests
{
    [TestClass]
    public class LocalizationResourceTests
    {
        private static readonly string? StringsDir = LocateStringsDir();
        private static readonly string? RepositoryRoot = LocateRepositoryRoot();

        private static string? LocateStringsDir()
        {
            string? directory = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(directory))
            {
                string candidate = Path.Combine(directory, "rainspace", "Strings");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }

            return null;
        }

        private static string? LocateRepositoryRoot()
        {
            string? directory = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(directory))
            {
                if (Directory.Exists(Path.Combine(directory, "rainspace", "Strings"))
                    && File.Exists(Path.Combine(directory, "GeekDesk-WinUI3.sln")))
                {
                    return directory;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }

            return null;
        }

        private static Dictionary<string, HashSet<string>> ReadResourceKeys()
        {
            Assert.IsNotNull(StringsDir, "Could not locate rainspace/Strings from the test output directory.");
            var byLang = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.GetFiles(StringsDir!, "Resources.resw", SearchOption.AllDirectories))
            {
                string lang = Path.GetFileName(Path.GetDirectoryName(file)!)!;
                var keys = new HashSet<string>(StringComparer.Ordinal);
                XDocument doc = XDocument.Load(file);
                foreach (XElement data in doc.Root!.Elements("data"))
                {
                    string? name = data.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(name))
                    {
                        keys.Add(name);
                    }
                }

                byLang[lang] = keys;
            }

            Assert.IsTrue(byLang.Count >= 2, "Expected at least a default and an en-US resource file.");
            return byLang;
        }

        /// <summary>
        /// 扫描 rainspace 源码中所有 L10n.Get("key") 调用，返回引用的资源键。
        /// </summary>
        private static HashSet<string> ReadL10nGetKeys()
        {
            Assert.IsNotNull(RepositoryRoot, "Could not locate repository root from the test output directory.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            string[] sourceFiles = Directory.GetFiles(Path.Combine(RepositoryRoot!, "rainspace"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                               && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .ToArray();
            foreach (string file in sourceFiles)
            {
                string source = File.ReadAllText(file);
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                             source, @"L10n\.Get\(\s*""([A-Za-z0-9_]+)""\s*\)"))
                {
                    keys.Add(match.Groups[1].Value);
                }
            }

            return keys;
        }

        /// <summary>
        /// 扫描 XAML 中所有 x:Uid="uid" 用法，返回使用的 uid 前缀。
        /// 每个 uid 对应的资源键形如 "uid.Property"（如 uid.Text、uid.Content）。
        /// </summary>
        private static HashSet<string> ReadXamlUidPrefixes()
        {
            Assert.IsNotNull(RepositoryRoot, "Could not locate repository root from the test output directory.");
            var uids = new HashSet<string>(StringComparer.Ordinal);
            string[] xamlFiles = Directory.GetFiles(Path.Combine(RepositoryRoot!, "rainspace"), "*.xaml", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                               && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .ToArray();
            foreach (string file in xamlFiles)
            {
                string source = File.ReadAllText(file);
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                             source, @"x:Uid=""([A-Za-z0-9_]+)"""))
                {
                    uids.Add(match.Groups[1].Value);
                }
            }

            return uids;
        }

        /// <summary>
        /// 判断资源键是否被代码引用：要么是 L10n.Get 精确引用的键，
        /// 要么是 XAML x:Uid 前缀下的属性键（形如 "uid.Property"）。
        /// </summary>
        private static bool IsKeyReferenced(string key, HashSet<string> l10nKeys, HashSet<string> xamlUids)
        {
            if (l10nKeys.Contains(key)) return true;
            int dotIndex = key.LastIndexOf('.');
            if (dotIndex > 0 && xamlUids.Contains(key.Substring(0, dotIndex))) return true;
            return false;
        }

        [TestMethod]
        public void AllLanguages_HaveIdenticalKeySets()
        {
            Dictionary<string, HashSet<string>> byLang = ReadResourceKeys();
            var reference = byLang.First().Value;
            foreach (var (lang, keys) in byLang)
            {
                Assert.IsTrue(
                    reference.SetEquals(keys),
                    $"资源键集合不一致：{lang} 缺少 {string.Join(",", reference.Except(keys))} 或多余 {string.Join(",", keys.Except(reference))}");
            }
        }

        [TestMethod]
        public void ResourceKeys_AreNotEmpty_AndHaveNonEmptyValues()
        {
            Assert.IsNotNull(StringsDir, "Could not locate rainspace/Strings from the test output directory.");
            foreach (string file in Directory.GetFiles(StringsDir!, "Resources.resw", SearchOption.AllDirectories))
            {
                XDocument doc = XDocument.Load(file);
                foreach (XElement data in doc.Root!.Elements("data"))
                {
                    string? value = data.Element("value")?.Value;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"{Path.GetFileName(file)} 中的 {data.Attribute("name")?.Value} 为空");
                }
            }
        }

        [TestMethod]
        public void EveryTrayResourceKey_ResolvesInBothLanguages()
        {
            Dictionary<string, HashSet<string>> byLang = ReadResourceKeys();
            string[] trayKeys =
            {
                "Tray_OpenPanel",
                "Tray_ExportBackup",
                "Tray_ImportBackup",
                "Tray_OpenAppDir",
                "Tray_RestartApp",
                "Tray_ExitApp"
            };

            foreach (string key in trayKeys)
            {
                Assert.IsTrue(
                    byLang.All(kv => kv.Value.Contains(key)),
                    $"托盘菜单资源键 {key} 必须在所有语言文件中存在");
            }
        }

        [TestMethod]
        public void EveryCodeReferencedKey_ExistsInAllLanguages()
        {
            Dictionary<string, HashSet<string>> byLang = ReadResourceKeys();
            HashSet<string> referenced = ReadL10nGetKeys();

            Assert.IsTrue(referenced.Count > 0, "没有扫描到任何 L10n.Get 调用，检查扫描路径是否失效。");
            foreach (string key in referenced)
            {
                Assert.IsTrue(
                    byLang.All(kv => kv.Value.Contains(key)),
                    $"代码引用的资源键 {key} 必须在所有语言文件中存在，否则 L10n.Get 会回退返回键名。");
            }
        }

        [TestMethod]
        public void NoOrphanedResourceKeys()
        {
            Dictionary<string, HashSet<string>> byLang = ReadResourceKeys();
            HashSet<string> l10nKeys = ReadL10nGetKeys();
            HashSet<string> xamlUids = ReadXamlUidPrefixes();
            var allKeys = byLang.First().Value;

            // 允许保留少量文档性前缀或测试专用键，但报告未引用键，避免无声膨胀。
            var orphaned = allKeys
                .Where(key => !IsKeyReferenced(key, l10nKeys, xamlUids))
                .ToArray();
            Assert.IsTrue(
                orphaned.Length <= 0,
                $"资源文件存在未被代码引用的键：{string.Join(",", orphaned)}。要么接入代码，要么删除。");
        }
    }
}
