namespace SocialMediaManager.API.Models;

public class LinkAccountRequest
{
    public Guid UserId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}