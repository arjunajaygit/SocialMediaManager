namespace SocialMediaManager.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // 👇 NEW: This is where the scrambled password will live!
    public string PasswordHash { get; set; } = string.Empty; 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property: One User can have many Social Accounts and Posts
    public ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}

