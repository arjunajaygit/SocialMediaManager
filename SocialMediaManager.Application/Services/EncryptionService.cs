using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SocialMediaManager.Application.Interfaces;

namespace SocialMediaManager.Application.Services;
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        // We get the master key from environment variables/settings
        var keyString = configuration["EncryptionSettings:MasterKey"] ?? "TwelveByteKey!!1234567890123456"; 
        _key = System.Text.Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
    }

    public (byte[] encryptedData, byte[] iv) Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return (ms.ToArray(), iv);
    }

    public string Decrypt(byte[] encryptedData, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}