using SocialMediaManager.Domain.Entities;

namespace SocialMediaManager.Application.Interfaces;

public interface ISocialMediaProvider
{
    // E.g., "LinkedIn", "Facebook"
    string PlatformName { get; } 
    
    // The method that will actually make the HTTP call to the social network
    Task<bool> PublishPostAsync(Post post, SocialAccount account);
}