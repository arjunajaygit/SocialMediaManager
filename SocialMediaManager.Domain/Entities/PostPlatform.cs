using System;

namespace SocialMediaManager.Domain.Entities;

public class PostPlatform
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;

    public Post? Post { get; set; }
}
