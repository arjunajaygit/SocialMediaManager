using Microsoft.EntityFrameworkCore;
using SocialMediaManager.Domain.Entities;
using SocialMediaManager.Application.Interfaces; 

namespace SocialMediaManager.Infrastructure.Data;

// We add the interface here so the "Brain" (Application) can talk to this "Tool" (Database)
public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<SocialAccount> SocialAccounts { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostPlatform> PostPlatforms { get; set; }

    // This makes the Interface's SaveChangesAsync map directly to the DbContext's method
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}