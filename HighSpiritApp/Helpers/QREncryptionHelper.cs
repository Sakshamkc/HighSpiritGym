using System.Security.Cryptography;
using System.Text;

namespace HighSpiritApp.Helpers
{
    /// <summary>
    /// Encrypts/decrypts customer IDs for QR codes so the raw ID is never exposed.
    /// Uses AES encryption with a secret key from appsettings.
    /// </summary>
    public static class QREncryptionHelper
    {
        /// <summary>
        /// Encrypt a customer ID into a URL-safe base64 string for QR codes.
        /// </summary>
        public static string Encrypt(int customerId, string secretKey)
        {
            var plainText = $"HSG-{customerId}-{DateTime.UtcNow.Ticks}";
            var key = DeriveKey(secretKey);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV + cipher for decryption
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            // URL-safe base64
            return Convert.ToBase64String(result)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Decrypt a QR code string back to the customer ID.
        /// Returns null if decryption fails (invalid/tampered QR).
        /// </summary>
        public static int? Decrypt(string encryptedText, string secretKey)
        {
            try
            {
                // Restore base64 padding
                var base64 = encryptedText
                    .Replace('-', '+')
                    .Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var fullBytes = Convert.FromBase64String(base64);
                var key = DeriveKey(secretKey);

                using var aes = Aes.Create();
                aes.Key = key;

                // Extract IV (first 16 bytes) and cipher
                var iv = new byte[16];
                var cipher = new byte[fullBytes.Length - 16];
                Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
                Buffer.BlockCopy(fullBytes, 16, cipher, 0, cipher.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                var plainText = Encoding.UTF8.GetString(plainBytes);

                // Expected format: HSG-{id}-{ticks}
                if (!plainText.StartsWith("HSG-")) return null;

                var parts = plainText.Split('-');
                if (parts.Length < 3) return null;

                if (int.TryParse(parts[1], out var id))
                    return id;

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Derive a 256-bit key from the secret string using SHA256.
        /// </summary>
        private static byte[] DeriveKey(string secret)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        }
    }
}
