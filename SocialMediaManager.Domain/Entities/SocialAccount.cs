namespace SocialMediaManager.Domain.Entities;

public class SocialAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public byte[] EncryptedAccessToken { get; set; } = Array.Empty<byte>();
    public byte[] EncryptionVector { get; set; } = Array.Empty<byte>();
    public string? RefreshToken { get; set; } // Added for Step 2 of the OAuth flow
    public DateTime CreatedAt { get; set; }
    public string? AccountUsername { get; set; }
    public User? User { get; set; }
}