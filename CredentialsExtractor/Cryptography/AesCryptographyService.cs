// AesCryptographyService.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CredentialsExtractor.Cryptography
{
    public class AesCryptographyService : ICryptographyService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesCryptographyService(string base64Key = null, string base64Iv = null)
        {
            // In production, these should be securely exchanged, not hardcoded
            // Using base64 encoded 32-byte key (256-bit) and 16-byte IV (128-bit)
            _key = Convert.FromBase64String(base64Key ?? "dGhpc2lzYXZlcnlzZWN1cmVrZXkxMjM0NTY3ODkwMTI=");
            _iv = Convert.FromBase64String(base64Iv ?? "dGhpc2lzMTZieXRlc2l2IQ==");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] encrypted = EncryptBytes(Encoding.UTF8.GetBytes(plainText));
            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decrypted = DecryptBytes(cipherBytes);
            return Encoding.UTF8.GetString(decrypted);
        }

        public byte[] EncryptBytes(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return new byte[0];

            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                        csEncrypt.FlushFinalBlock();
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        public byte[] DecryptBytes(byte[] cipherBytes)
        {
            if (cipherBytes == null || cipherBytes.Length == 0)
                return new byte[0];

            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(cipherBytes))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var msPlain = new MemoryStream())
                {
                    csDecrypt.CopyTo(msPlain);
                    return msPlain.ToArray();
                }
            }
        }
    }

    // Factory for creating cryptography services
    public class CryptographyServiceFactory
    {
        public static ICryptographyService CreateDefault()
        {
            return new AesCryptographyService();
        }

        public static ICryptographyService CreateWithKeys(string base64Key, string base64Iv)
        {
            return new AesCryptographyService(base64Key, base64Iv);
        }
    }
}