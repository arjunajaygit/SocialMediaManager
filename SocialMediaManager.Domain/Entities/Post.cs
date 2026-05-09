namespace SocialMediaManager.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime ScheduledFor { get; set; }
    public bool IsPublished { get; set; } = false;
    
    public User? User { get; set; }
    public List<PostPlatform>? Platforms { get; set; } = new();
}