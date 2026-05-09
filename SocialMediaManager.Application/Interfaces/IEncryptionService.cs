namespace SocialMediaManager.Application.Interfaces;

public interface IEncryptionService
{
    (byte[] encryptedData, byte[] iv) Encrypt(string plainText);
    string Decrypt(byte[] encryptedData, byte[] iv);
}
