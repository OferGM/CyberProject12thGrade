// AesCryptographyService.cs - Fixed version
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
            try
            {
                // Use the same default key and IV as the Python server
                // Key: 32 bytes for AES-256
                string defaultKey = "dGhpc2lzYXZlcnlzZWN1cmVrZXkxMjM0NTY3ODkwMTI="; // "thisisaverysecurekey123456789012"
                // IV: 16 bytes - using simple "1234567890123456"
                string defaultIv = "MTIzNDU2Nzg5MDEyMzQ1Ng=="; // "1234567890123456"

                _key = Convert.FromBase64String(base64Key ?? defaultKey);
                _iv = Convert.FromBase64String(base64Iv ?? defaultIv);

                // Verify lengths
                if (_key.Length != 32)
                    throw new ArgumentException($"Key must be 32 bytes long, got {_key.Length}");
                if (_iv.Length != 16)
                    throw new ArgumentException($"IV must be 16 bytes long, got {_iv.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing AES service: {ex.Message}");
                // Fallback to guaranteed working values
                _key = Encoding.UTF8.GetBytes("thisisaverysecurekey123456789012"); // 32 bytes
                _iv = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes
            }
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

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decrypted = DecryptBytes(cipherBytes);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                throw;
            }
        }

        public byte[] EncryptBytes(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return new byte[0];

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption error: {ex.Message}");
                Console.WriteLine($"Key length: {_key.Length}, IV length: {_iv.Length}");
                throw;
            }
        }

        public byte[] DecryptBytes(byte[] cipherBytes)
        {
            if (cipherBytes == null || cipherBytes.Length == 0)
                return new byte[0];

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                Console.WriteLine($"Key length: {_key.Length}, IV length: {_iv.Length}");
                throw;
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