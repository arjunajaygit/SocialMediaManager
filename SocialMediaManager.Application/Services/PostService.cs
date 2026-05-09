using Microsoft.Extensions.Logging;
using SocialMediaManager.Application.Interfaces;
using SocialMediaManager.Domain.Entities;

namespace SocialMediaManager.Application.Services;

public class PostService : IPostService
{
    private readonly IEnumerable<ISocialMediaProvider> _providers;
    private readonly ILogger<PostService> _logger;

    // Dependency Injection will hand us ALL our social providers at once
    public PostService(IEnumerable<ISocialMediaProvider> providers, ILogger<PostService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task ProcessScheduledPostAsync(Guid postId)
    {
        _logger.LogInformation($"⚙️ Hangfire activated Job for Post ID: {postId}");

        // For this test, we are bypassing the database and faking the data
        var fakePost = new Post { Content = "Hello world from the Hangfire Queue!" };
        var fakeAccount = new SocialAccount { PlatformName = "LinkedIn" };

        // Find the specific provider for this account
        var provider = _providers.FirstOrDefault(p => p.PlatformName == fakeAccount.PlatformName);
        
        if (provider != null)
        {
            await provider.PublishPostAsync(fakePost, fakeAccount);
        }
        else
        {
            _logger.LogWarning($"❌ No provider found for {fakeAccount.PlatformName}");
        }
    }
}