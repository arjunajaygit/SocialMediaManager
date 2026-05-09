using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMediaManager.Domain.Entities;
using SocialMediaManager.Infrastructure.Data;

namespace SocialMediaManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly AppDbContext _context;

    public PostController(AppDbContext context)
    {
        _context = context;
    }

    public class CreatePostRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string>? SelectedPlatforms { get; set; }
        public DateTime? ScheduledFor { get; set; } 
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                user = new User 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "DemoUser", 
                    Email = "demo@example.com",
                    PasswordHash = "dummyhash"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                
                ScheduledFor = request.ScheduledFor?.ToUniversalTime() ?? DateTime.UtcNow, 
                IsPublished = false
            };

            if (request.SelectedPlatforms != null && request.SelectedPlatforms.Any())
            {
                post.Platforms = request.SelectedPlatforms.Select(name => new PostPlatform
                {
                    Id = Guid.NewGuid(),
                    PlatformName = name,
                    IsPublished = false,
                    PostId = post.Id
                }).ToList();
            }
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post saved successfully!", postId = post.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts()
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                return Ok(new List<object>());
            }

            var posts = await _context.Posts
                .Where(p => p.UserId == user.Id)
                .Include(p => p.Platforms)
                .OrderByDescending(p => p.ScheduledFor)
                .Select(p => new 
                {
                    id = p.Id,
                    content = p.Content,
                    imageUrl = p.ImageUrl,
                    scheduledFor = p.ScheduledFor,
                    isPublished = p.IsPublished,
                    platforms = p.Platforms.Select(pl => new {
                        id = pl.Id,
                        platformName = pl.PlatformName,
                        isPublished = pl.IsPublished
                    })
                })
                .ToListAsync();

            return Ok(posts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}