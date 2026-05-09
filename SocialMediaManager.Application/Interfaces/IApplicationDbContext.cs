using Microsoft.EntityFrameworkCore;
using SocialMediaManager.Domain.Entities;

namespace SocialMediaManager.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<SocialAccount> SocialAccounts { get; }
    DbSet<Post> Posts { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}