using Microsoft.Extensions.Logging;
using SocialMediaManager.Application.Interfaces;
using SocialMediaManager.Domain.Entities;

namespace SocialMediaManager.Infrastructure.Services;

public class LinkedInProvider : ISocialMediaProvider
{
    private readonly ILogger<LinkedInProvider> _logger;

    public LinkedInProvider(ILogger<LinkedInProvider> logger)
    {
        _logger = logger;
    }

    public string PlatformName => "LinkedIn";

    public async Task<bool> PublishPostAsync(Post post, SocialAccount account)
    {
        _logger.LogInformation("⏳ Connecting to LinkedIn API...");
        
        // Simulate a 2-second network delay
        await Task.Delay(2000); 
        
        _logger.LogInformation($"✅ SUCCESS: Published '{post.Content}' to {PlatformName}!");
        return true;
    }
}