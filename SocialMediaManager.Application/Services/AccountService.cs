using SocialMediaManager.Domain.Entities;
using SocialMediaManager.Application.Interfaces;

namespace SocialMediaManager.Application.Services;

public class AccountService
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;

    // FIX: Change 'AppDbContext' to 'IApplicationDbContext' here
    public AccountService(IApplicationDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task LinkAccountAsync(Guid userId, string platform, string token)
    {
        // 1. Encrypt the token immediately
        var (encryptedToken, iv) = _encryptionService.Encrypt(token);

        // 2. Create the entity
        var account = new SocialAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlatformName = platform,
            EncryptedAccessToken = encryptedToken,
            EncryptionVector = iv
        };

        // 3. Save to PostgreSQL
        _context.SocialAccounts.Add(account);
        await _context.SaveChangesAsync();
    }
}