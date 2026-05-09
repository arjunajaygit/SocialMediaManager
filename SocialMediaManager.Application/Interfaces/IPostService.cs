using SocialMediaManager.Domain.Entities;

namespace SocialMediaManager.Application.Interfaces;

public interface IPostService
{
    // This is the method Hangfire will call in the background
    Task ProcessScheduledPostAsync(Guid postId);
}