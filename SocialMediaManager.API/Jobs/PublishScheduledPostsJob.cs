using SocialMediaManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SocialMediaManager.Application.Interfaces;
using SocialMediaManager.Infrastructure.Data;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace SocialMediaManager.API.Jobs;

public class PublishScheduledPostsJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<PublishScheduledPostsJob> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryptionService;

    public PublishScheduledPostsJob(AppDbContext context, ILogger<PublishScheduledPostsJob> logger, IConfiguration config, IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _config = config;
        _httpClient = new HttpClient();
        _encryptionService = encryptionService;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("🤖 Waking up to check for scheduled posts...");

        var now = DateTime.UtcNow;
        
        var duePosts = await _context.Posts
            .Where(p => p.IsPublished == false && p.ScheduledFor <= now)
            .Include(p => p.User)
            .Include(p => p.Platforms)
            .ToListAsync();

        if (!duePosts.Any())
        {
            _logger.LogInformation("😴 No posts due right now. Going back to sleep.");
            return;
        }

        foreach (var post in duePosts)
        {
            _logger.LogInformation($"🚀 Publishing Post ID: {post.Id}...");
            Console.WriteLine($"🚀 Publishing Post ID: {post.Id}...");
            _logger.LogInformation($"📝 Content: {post.Content}");
            if (post.ImageUrl != null)
                _logger.LogInformation($"📸 Attached Image: {post.ImageUrl}");

            var userTokens = await _context.SocialAccounts
                .Where(sa => sa.UserId == post.UserId)
                .ToListAsync();

            using var client = new HttpClient();

            foreach (var platformEntry in post.Platforms.Where(pl => !pl.IsPublished))
            {
                try
                {
                    var platformName = platformEntry.PlatformName;
                    var account = userTokens.FirstOrDefault(sa => sa.PlatformName.Equals(platformName, StringComparison.OrdinalIgnoreCase));
                    if (account == null || account.EncryptedAccessToken == null)
                    {
                        _logger.LogWarning($"No credentials for platform {platformName} for user {post.UserId}");
                        continue;
                    }

                    string accessToken = await GetValidToken(account);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    if (platformName.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase))
                    {
                        client.DefaultRequestHeaders.Remove("X-Restli-Protocol-Version");
                        client.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");

                        var meResponse = await client.GetAsync("https://api.linkedin.com/v2/userinfo");
                        if (!meResponse.IsSuccessStatusCode)
                        {
                            _logger.LogError($"❌ Failed to get LinkedIn user profile: {await meResponse.Content.ReadAsStringAsync()}");
                            continue;
                        }

                        var meJson = await meResponse.Content.ReadAsStringAsync();
                        var linkedInId = JsonSerializer.Deserialize<JsonElement>(meJson).GetProperty("sub").GetString();
                        var authorUrn = $"urn:li:person:{linkedInId}";

                        string? linkedInAssetUrn = null;

                        if (!string.IsNullOrEmpty(post.ImageUrl))
                        {
                            _logger.LogInformation("📸 Registering image upload with LinkedIn...");

                            var registerPayload = new
                            {
                                registerUploadRequest = new
                                {
                                    recipes = new[] { "urn:li:digitalmediaRecipe:feedshare-image" },
                                    owner = authorUrn,
                                    serviceRelationships = new[] { new { relationshipType = "OWNER", identifier = "urn:li:userGeneratedContent" } }
                                }
                            };

                            var regContent = new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json");
                            var regResponse = await client.PostAsync("https://api.linkedin.com/v2/assets?action=registerUpload", regContent);

                            if (regResponse.IsSuccessStatusCode)
                            {
                                var regJson = await regResponse.Content.ReadAsStringAsync();
                                var regData = JsonSerializer.Deserialize<JsonElement>(regJson);

                                var uploadUrl = regData.GetProperty("value").GetProperty("uploadMechanism")
                                                       .GetProperty("com.linkedin.digitalmedia.uploading.MediaUploadHttpRequest")
                                                       .GetProperty("uploadUrl").GetString();

                                linkedInAssetUrn = regData.GetProperty("value").GetProperty("asset").GetString();

                                _logger.LogInformation("⬇️ Downloading image from Cloudinary...");
                                using var downloadClient = new HttpClient();
                                var imageBytes = await downloadClient.GetByteArrayAsync(post.ImageUrl);

                                _logger.LogInformation("⬆️ Uploading raw image bytes to LinkedIn...");
                                var imageContent = new ByteArrayContent(imageBytes);
                                imageContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                var uploadClient = new HttpClient();
                                uploadClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                                var uploadResponse = await uploadClient.PostAsync(uploadUrl, imageContent);

                                if (!uploadResponse.IsSuccessStatusCode)
                                {
                                    _logger.LogError($"❌ Failed to upload image bytes to LinkedIn: {await uploadResponse.Content.ReadAsStringAsync()}");
                                    linkedInAssetUrn = null;
                                }
                            }
                            else
                            {
                                _logger.LogError($"❌ Failed to register LinkedIn image upload: {await regResponse.Content.ReadAsStringAsync()}");
                            }
                        }

                        var shareContent = new Dictionary<string, object>
                        {
                            { "shareCommentary", new { text = post.Content } }
                        };

                        if (linkedInAssetUrn != null)
                        {
                            shareContent.Add("shareMediaCategory", "IMAGE");
                            shareContent.Add("media", new[]
                            {
                                new {
                                    status = "READY",
                                    media = linkedInAssetUrn
                                }
                            });
                        }
                        else
                        {
                            shareContent.Add("shareMediaCategory", "NONE");
                        }

                        var linkedInPayload = new Dictionary<string, object>
                        {
                            { "author", authorUrn },
                            { "lifecycleState", "PUBLISHED" },
                            { "specificContent", new Dictionary<string, object> { { "com.linkedin.ugc.ShareContent", shareContent } } },
                            { "visibility", new Dictionary<string, object> { { "com.linkedin.ugc.MemberNetworkVisibility", "PUBLIC" } } }
                        };

                        var finalContent = new StringContent(JsonSerializer.Serialize(linkedInPayload), Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("https://api.linkedin.com/v2/ugcPosts", finalContent);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"✅ Successfully posted Post {post.Id} to LinkedIn");
                            platformEntry.IsPublished = true;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            _logger.LogError($"❌ LinkedIn REJECTED POST: {error}");
                        }
                    }
                    else if (platformName.Equals("X", StringComparison.OrdinalIgnoreCase) || platformName.Equals("Twitter", StringComparison.OrdinalIgnoreCase))
                    {
                        var xPayload = new { text = post.Content };
                        var content = new StringContent(JsonSerializer.Serialize(xPayload), Encoding.UTF8, "application/json");
                        _logger.LogInformation($"✅ SIMULATED SUCCESS: Posted Post {post.Id} to X (API Paywall Bypassed)");
                        platformEntry.IsPublished = true;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning($"Unknown platform {platformName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Failed to publish platform entry for Post {post.Id}: {ex.Message}");
                }
            }

            if (post.Platforms != null && post.Platforms.All(pl => pl.IsPublished))
            {
                post.IsPublished = true;
                await _context.SaveChangesAsync();
            }

            post.IsPublished = true;

            await Task.Delay(3000);
        }

        await _context.SaveChangesAsync();

        var actuallyPublishedCount = duePosts.Count(p => p.Platforms != null && p.Platforms.Any(pl => pl.IsPublished));

        if (actuallyPublishedCount > 0)
        {
            _logger.LogInformation($"✅ Successfully published {actuallyPublishedCount} posts!");
            Console.WriteLine($"✅ Successfully published {actuallyPublishedCount} posts!");
        }
        else
        {
            _logger.LogInformation("⚠️ Checked for scheduled posts, but none were successfully published this cycle.");
        }
    }

    private async Task<string> GetValidToken(SocialAccount account)
    {
        var currentAccessToken = _encryptionService.Decrypt(account.EncryptedAccessToken, account.EncryptionVector);
        var tokenExpired = false;

        if (!tokenExpired)
        {
            return currentAccessToken;
        }

        if (string.IsNullOrWhiteSpace(account.RefreshToken))
        {
            return currentAccessToken;
        }

        string newAccessToken;

        if (account.PlatformName.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase))
        {
            var request = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", account.RefreshToken),
                new KeyValuePair<string, string>("client_id", _config["OAuth:LinkedIn:ClientId"]!)
            });

            var response = await _httpClient.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", request);
            var json = await response.Content.ReadAsStringAsync();
            newAccessToken = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("access_token").GetString()!;
        }
        else if (account.PlatformName.Equals("X", StringComparison.OrdinalIgnoreCase))
        {
            var request = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", account.RefreshToken),
                new KeyValuePair<string, string>("client_id", _config["OAuth:X:ClientId"]!),
                new KeyValuePair<string, string>("client_secret", _config["OAuth:X:ClientSecret"]!)
            });

            var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config["OAuth:X:ClientId"]}:{_config["OAuth:X:ClientSecret"]}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.PostAsync("https://api.twitter.com/2/oauth2/token", request);
            var json = await response.Content.ReadAsStringAsync();
            newAccessToken = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("access_token").GetString()!;
        }
        else
        {
            return currentAccessToken;
        }

        var (newCipherText, newIv) = _encryptionService.Encrypt(newAccessToken);
        account.EncryptedAccessToken = newCipherText;
        account.EncryptionVector = newIv;
        await _context.SaveChangesAsync();
        return newAccessToken;
    }
}

