using raindesk.Util;

namespace raindesk.Tests
{
    [TestClass]
    public class MD5UtilTests
    {
        [TestMethod]
        public void CreateMD5_AsciiInput_ReturnsConsistentHash()
        {
            string hash1 = MD5Util.CreateMD5("test");
            string hash2 = MD5Util.CreateMD5("test");
            Assert.AreEqual(hash1, hash2, "相同输入应产生相同哈希");
        }

        [TestMethod]
        public void CreateMD5_DifferentInput_ReturnsDifferentHash()
        {
            string hash1 = MD5Util.CreateMD5("test1");
            string hash2 = MD5Util.CreateMD5("test2");
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void CreateMD5_ReturnsHexString()
        {
            string hash = MD5Util.CreateMD5("test");
            // MD5 输出 32 个十六进制字符
            Assert.AreEqual(32, hash.Length);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(hash, @"^[0-9A-F]+$"),
                "应返回大写十六进制字符串");
        }

        [TestMethod]
        public void CreateMD5_UnicodeInput_WorksCorrectly()
        {
            // 验证 UTF-8 编码修复：中文字符不应被丢弃
            string hash = MD5Util.CreateMD5("测试");
            Assert.AreEqual(32, hash.Length);
            Assert.IsFalse(string.IsNullOrEmpty(hash));
        }

        [TestMethod]
        public void CreateMD5_UnicodeInput_DifferentFromAsciiReplacement()
        {
            // 旧 ASCII 编码会将中文替换为 '?'，导致不同中文字符串产生相同哈希
            string hash1 = MD5Util.CreateMD5("你好");
            string hash2 = MD5Util.CreateMD5("世界");
            Assert.AreNotEqual(hash1, hash2,
                "不同中文字符串应产生不同哈希（UTF-8 编码）");
        }
    }
}
