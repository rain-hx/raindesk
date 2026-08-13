using rainspace.Util;

namespace rainspace.Tests
{
    [TestClass]
    public class PasswordUtilTests
    {
        [TestMethod]
        public void HashPassword_ValidInput_ReturnsPbkdf2Format()
        {
            string hash = PasswordUtil.HashPassword("test123");
            Assert.IsFalse(string.IsNullOrEmpty(hash));
            Assert.IsTrue(hash.StartsWith("$pbkdf2$"), "应返回 PBKDF2 格式");
        }

        [TestMethod]
        public void HashPassword_SameInput_DifferentHashes()
        {
            // PBKDF2 使用随机盐，相同输入应产生不同哈希
            string hash1 = PasswordUtil.HashPassword("test123");
            string hash2 = PasswordUtil.HashPassword("test123");
            Assert.AreNotEqual(hash1, hash2, "相同密码的哈希应不同（随机盐）");
        }

        [TestMethod]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            string hash = PasswordUtil.HashPassword("test123");
            Assert.IsTrue(PasswordUtil.VerifyPassword("test123", hash));
        }

        [TestMethod]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            string hash = PasswordUtil.HashPassword("test123");
            Assert.IsFalse(PasswordUtil.VerifyPassword("wrong", hash));
        }

        [TestMethod]
        public void VerifyPassword_EmptyPassword_ReturnsFalse()
        {
            string hash = PasswordUtil.HashPassword("test123");
            Assert.IsFalse(PasswordUtil.VerifyPassword("", hash));
        }

        [TestMethod]
        public void VerifyPassword_NullHash_ReturnsFalse()
        {
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", null));
        }

        [TestMethod]
        public void VerifyPassword_EmptyHash_ReturnsFalse()
        {
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", ""));
        }

        [TestMethod]
        public void VerifyPassword_LegacyMd5_StillWorks()
        {
            // 兼容旧 MD5 格式
            string oldHash = MD5Util.CreateMD5("test123");
            Assert.IsTrue(PasswordUtil.VerifyPassword("test123", oldHash),
                "应兼容旧 MD5 格式");
        }

        [TestMethod]
        public void VerifyPassword_LegacyMd5_WrongPassword_ReturnsFalse()
        {
            string oldHash = MD5Util.CreateMD5("test123");
            Assert.IsFalse(PasswordUtil.VerifyPassword("wrong", oldHash));
        }

        [TestMethod]
        public void IsLegacyFormat_Md5Hash_ReturnsTrue()
        {
            string md5Hash = MD5Util.CreateMD5("test");
            Assert.IsTrue(PasswordUtil.IsLegacyFormat(md5Hash));
        }

        [TestMethod]
        public void IsLegacyFormat_Pbkdf2Hash_ReturnsFalse()
        {
            string pbkdf2Hash = PasswordUtil.HashPassword("test");
            Assert.IsFalse(PasswordUtil.IsLegacyFormat(pbkdf2Hash));
        }

        [TestMethod]
        public void IsLegacyFormat_Null_ReturnsFalse()
        {
            Assert.IsFalse(PasswordUtil.IsLegacyFormat(null));
        }

        [TestMethod]
        public void HashPassword_UnicodeInput_WorksCorrectly()
        {
            // 验证 UTF-8 编码正确处理中文
            string hash = PasswordUtil.HashPassword("密码测试");
            Assert.IsTrue(PasswordUtil.VerifyPassword("密码测试", hash));
            Assert.IsFalse(PasswordUtil.VerifyPassword("wrong", hash));
        }

        [TestMethod]
        public void HashPassword_LongInput_WorksCorrectly()
        {
            string longPassword = new string('a', 1000);
            string hash = PasswordUtil.HashPassword(longPassword);
            Assert.IsTrue(PasswordUtil.VerifyPassword(longPassword, hash));
        }

        [TestMethod]
        public void VerifyPassword_TamperedPbkdf2Hash_ReturnsFalse()
        {
            string hash = PasswordUtil.HashPassword("test123");
            string tampered = hash.Replace("$pbkdf2$", "$pbkdf3$");
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", tampered));
        }

        [TestMethod]
        public void VerifyPassword_ExcessiveIterations_RejectedWithinLimit()
        {
            string hash = PasswordUtil.HashPassword("test123");
            string[] parts = hash.Split('$');
            string excessive = $"$pbkdf2${int.MaxValue}${parts[3]}${parts[4]}";
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", excessive),
                "超大迭代次数必须立即拒绝，避免验证时产生过度的 CPU 消耗");
        }

        [TestMethod]
        public void VerifyPassword_OversizedSalt_Rejected()
        {
            string hash = PasswordUtil.HashPassword("test123");
            string[] parts = hash.Split('$');
            string oversizedSalt = Convert.ToBase64String(new byte[1024]);
            string malicious = $"$pbkdf2${parts[2]}${oversizedSalt}${parts[4]}";
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", malicious),
                "超大 salt 必须立即拒绝，避免验证时产生过度的内存消耗");
        }

        [TestMethod]
        public void VerifyPassword_OversizedHash_Rejected()
        {
            string hash = PasswordUtil.HashPassword("test123");
            string[] parts = hash.Split('$');
            string oversizedHash = Convert.ToBase64String(new byte[1024]);
            string malicious = $"$pbkdf2${parts[2]}${parts[3]}${oversizedHash}";
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", malicious),
                "超大期望哈希长度必须立即拒绝，避免验证时产生过度的内存消耗");
        }

        [TestMethod]
        public void VerifyPassword_NonBase64Hash_ReturnsFalse()
        {
            string hash = PasswordUtil.HashPassword("test123");
            string[] parts = hash.Split('$');
            string malicious = $"$pbkdf2${parts[2]}${parts[3]}$not!base64";
            Assert.IsFalse(PasswordUtil.VerifyPassword("test123", malicious));
        }
    }
}
