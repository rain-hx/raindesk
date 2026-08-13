using System;
using System.Security.Cryptography;

namespace rainspace.Util
{
    /// <summary>
    /// 安全密码哈希工具，使用 PBKDF2-SHA256。
    /// 兼容旧 MD5 格式（验证时自动降级），新密码一律使用 PBKDF2。
    /// </summary>
    internal static class PasswordUtil
    {
        private const int SaltSize = 16;       // 128 bits
        private const int HashSize = 32;       // 256 bits
        private const int Iterations = 100_000; // OWASP 2023 推荐最低值

        // PBKDF2 自描述格式解析的资源上限（防止恶意/损坏备份构造超大迭代次数或哈希长度）。
        private const int MinIterations = 10_000;
        private const int MaxIterations = 10_000_000;
        private const int MinSaltSize = 12;    // 96 bits
        private const int MaxSaltSize = 64;    // 512 bits
        private const int MinHashSize = 16;    // 128 bits
        private const int MaxHashSize = 128;   // 1024 bits

        /// <summary>
        /// 使用 PBKDF2-SHA256 哈希密码，返回自描述格式字符串。
        /// 格式: $pbkdf2$iterations$base64(salt)$base64(hash)
        /// </summary>
        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return $"$pbkdf2${Iterations}$" +
                   $"{Convert.ToBase64String(salt)}$" +
                   $"{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// 验证密码是否匹配存储的哈希。
        /// 自动识别 PBKDF2 格式和旧 MD5 格式。
        /// </summary>
        public static bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            // PBKDF2 格式
            if (storedHash.StartsWith("$pbkdf2$"))
            {
                return VerifyPbkdf2(password, storedHash);
            }

            // 旧 MD5 格式（兼容迁移期）
            return VerifyLegacyMd5(password, storedHash);
        }

        /// <summary>
        /// 判断存储的哈希是否为旧 MD5 格式（需要迁移）
        /// </summary>
        public static bool IsLegacyFormat(string? storedHash)
        {
            return !string.IsNullOrEmpty(storedHash) && !storedHash.StartsWith("$pbkdf2$");
        }

        /// <summary>
        /// 验证密码并自动迁移到 PBKDF2 格式。
        /// 如果密码正确且使用旧 MD5 格式，会返回新的 PBKDF2 哈希。
        /// </summary>
        /// <param name="password">用户输入的密码</param>
        /// <param name="storedHash">存储的哈希</param>
        /// <param name="newHash">如果迁移成功，返回新的 PBKDF2 哈希；否则为 null</param>
        /// <returns>密码是否正确</returns>
        public static bool VerifyAndMigrate(string password, string storedHash, out string? newHash)
        {
            newHash = null;

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            bool isValid = VerifyPassword(password, storedHash);

            // 如果密码正确且使用旧格式，自动迁移到 PBKDF2
            if (isValid && IsLegacyFormat(storedHash))
            {
                try
                {
                    newHash = HashPassword(password);
                    LogUtil.WriteLog("密码已从 MD5 迁移到 PBKDF2 格式");
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "密码迁移失败");
                }
            }

            return isValid;
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 5) return false;

                if (!int.TryParse(parts[2], out int iterations)
                    || iterations < MinIterations
                    || iterations > MaxIterations)
                {
                    return false;
                }

                byte[] salt;
                byte[] expectedHash;
                try
                {
                    salt = Convert.FromBase64String(parts[3]);
                    expectedHash = Convert.FromBase64String(parts[4]);
                }
                catch (FormatException)
                {
                    return false;
                }

                if (salt.Length < MinSaltSize
                    || salt.Length > MaxSaltSize
                    || expectedHash.Length < MinHashSize
                    || expectedHash.Length > MaxHashSize)
                {
                    return false;
                }

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证旧 MD5 哈希（仅用于迁移兼容，验证成功后应迁移到 PBKDF2）
        /// </summary>
        private static bool VerifyLegacyMd5(string password, string storedHash)
        {
            try
            {
                string legacyHash = MD5Util.CreateMD5(password);
                return string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
