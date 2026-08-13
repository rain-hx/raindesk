namespace raindesk.Tests
{
    [TestClass]
    public class VersionComparisonTests
    {
        [TestMethod]
        public void VersionCompare_MajorVersion_WorksCorrectly()
        {
            var v1 = Version.Parse("2.0.0");
            var v2 = Version.Parse("3.0.0");
            Assert.IsTrue(v2 > v1);
        }

        [TestMethod]
        public void VersionCompare_MinorVersion_WorksCorrectly()
        {
            // 字符串比较会失败: "2.10.0" < "2.9.0" (字典序)
            // Version 比较正确: 2.10.0 > 2.9.0
            var v29 = Version.Parse("2.9.0");
            var v210 = Version.Parse("2.10.0");
            Assert.IsTrue(v210 > v29, "2.10.0 应大于 2.9.0");
        }

        [TestMethod]
        public void VersionCompare_PatchVersion_WorksCorrectly()
        {
            var v1 = Version.Parse("1.0.1");
            var v2 = Version.Parse("1.0.2");
            Assert.IsTrue(v2 > v1);
        }

        [TestMethod]
        public void VersionCompare_SameVersion_ReturnsEqual()
        {
            var v1 = Version.Parse("2.5.1");
            var v2 = Version.Parse("2.5.1");
            Assert.AreEqual(0, v1.CompareTo(v2));
        }

        [TestMethod]
        public void VersionCompare_StringComparison_WrongForMultiDigit()
        {
            // 演示字符串比较的错误
            string s29 = "2.9.0";
            string s210 = "2.10.0";
            // 字符串比较: "2.10.0" < "2.9.0" (因为 '1' < '9')
            Assert.IsTrue(s210.CompareTo(s29) < 0, "字符串比较错误地认为 2.10.0 < 2.9.0");

            // Version 比较正确
            var v29 = Version.Parse(s29);
            var v210 = Version.Parse(s210);
            Assert.IsTrue(v210 > v29, "Version 比较正确: 2.10.0 > 2.9.0");
        }

        [TestMethod]
        public void VersionTryParse_ValidVersion_ReturnsTrue()
        {
            Assert.IsTrue(Version.TryParse("2.5.1", out _));
            Assert.IsTrue(Version.TryParse("1.0.0", out _));
            Assert.IsTrue(Version.TryParse("10.20.30", out _));
        }

        [TestMethod]
        public void VersionTryParse_InvalidVersion_ReturnsFalse()
        {
            Assert.IsFalse(Version.TryParse("abc", out _));
            Assert.IsFalse(Version.TryParse("", out _));
            Assert.IsFalse(Version.TryParse(null, out _));
        }
    }
}
