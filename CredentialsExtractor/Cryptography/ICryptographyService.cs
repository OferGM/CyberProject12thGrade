namespace CredentialsExtractor.Cryptography
{
    public interface ICryptographyService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        byte[] EncryptBytes(byte[] plainBytes);
        byte[] DecryptBytes(byte[] cipherBytes);
    }
}