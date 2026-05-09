using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SocialMediaManager.Domain.Entities;
using SocialMediaManager.Infrastructure.Data;
using SocialMediaManager.Application.Interfaces;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace SocialMediaManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryptionService;

    public OAuthController(AppDbContext context, IConfiguration config, IEncryptionService encryptionService)
    {
        _context = context;
        _config = config;
        _httpClient = new HttpClient();
        _encryptionService = encryptionService;
    }

    public class ExchangeRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        Console.WriteLine("🚀 THE TERMINAL IS ALIVE!");
        return Ok("Success");
    }

    [HttpPost("{platform}/exchange")]
    public async Task<IActionResult> ExchangeToken(string platform, [FromBody] ExchangeRequest request)
    {
        Console.WriteLine($"--- Processing {platform} exchange ---");

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "DemoUser");
            if (user == null)
            {
                Console.WriteLine("❌ DB ERROR: Could not find DemoUser!");
                return Unauthorized();
            }

            var platformTitle = platform.ToLower() == "x" ? "X" : "LinkedIn";
            string accessToken = "";
            string? refreshToken = null;
            string? accountUsername = null;

            if (platform.ToLower() == "linkedin")
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("client_id", _config["OAuth:LinkedIn:ClientId"]!),
                    new KeyValuePair<string, string>("client_secret", _config["OAuth:LinkedIn:ClientSecret"]!),
                    new KeyValuePair<string, string>("redirect_uri", _config["OAuth:LinkedIn:RedirectUri"]!)
                });

                var response = await _httpClient.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return BadRequest(json);

                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
                accessToken = tokenData.GetProperty("access_token").GetString()!;
                if (tokenData.TryGetProperty("refresh_token", out var refreshTokenElement))
                {
                    refreshToken = refreshTokenElement.GetString();
                }

                using var liClient = new HttpClient();
                liClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var liResponse = await liClient.GetAsync("https://api.linkedin.com/v2/userinfo");
                if (liResponse.IsSuccessStatusCode)
                {
                    var liJson = await liResponse.Content.ReadAsStringAsync();
                    var liData = JsonSerializer.Deserialize<JsonElement>(liJson);
                    accountUsername = liData.GetProperty("name").GetString();
                }
            }
            else if (platform.ToLower() == "x")
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("client_id", _config["OAuth:X:ClientId"]!),
                    new KeyValuePair<string, string>("redirect_uri", _config["OAuth:X:RedirectUri"]!),
                    new KeyValuePair<string, string>("code_verifier", "challenge_string")
                });

                var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config["OAuth:X:ClientId"]}:{_config["OAuth:X:ClientSecret"]}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

                var response = await _httpClient.PostAsync("https://api.twitter.com/2/oauth2/token", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return BadRequest(json);

                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
                accessToken = tokenData.GetProperty("access_token").GetString()!;
                if (tokenData.TryGetProperty("refresh_token", out var refreshTokenElement))
                {
                    refreshToken = refreshTokenElement.GetString();
                }

                using var xClient = new HttpClient();
                xClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                Console.WriteLine("--- Fetching X User Info ---");

                var xResponse = await xClient.GetAsync("https://api.twitter.com/2/users/me");
                if (xResponse.IsSuccessStatusCode)
                {
                    var xJson = await xResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"--- X User Info JSON: {xJson} ---");
                    try
                    {
                        var xData = JsonSerializer.Deserialize<JsonElement>(xJson);
                        accountUsername = "@" + xData.GetProperty("data").GetProperty("username").GetString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ JSON Parse Error for X: {ex.Message}");
                    }
                }
                else
                {
                    var err = await xResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ X User Fetch Error: {xResponse.StatusCode} - {err}");
                }
            }

            var (cipherText, iv) = _encryptionService.Encrypt(accessToken);

            var socialAccount = await _context.SocialAccounts.FirstOrDefaultAsync(sa => sa.UserId == user.Id && sa.PlatformName == platformTitle);
            if (socialAccount == null)
            {
                _context.SocialAccounts.Add(new SocialAccount
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PlatformName = platformTitle,
                    EncryptedAccessToken = cipherText,
                    RefreshToken = refreshToken,
                    EncryptionVector = iv,
                    AccountUsername = accountUsername,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                socialAccount.EncryptedAccessToken = cipherText;
                socialAccount.EncryptionVector = iv;
                socialAccount.RefreshToken = refreshToken;
                socialAccount.AccountUsername = accountUsername;
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Success: Account ({accountUsername}) saved to DB.");
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Crash: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("exchange")]
    public async Task<IActionResult> Exchange([FromBody] ExchangeRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return BadRequest("Code is missing from the request.");
        }

        return Ok(new { message = "Code received" });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetConnectionStatus()
    {
        var user = await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
        if (user == null) return NotFound("User not found");

        var connectedAccounts = await _context.SocialAccounts
            .Where(sa => sa.UserId == user.Id)
            .ToListAsync();

        var linkedInAcc = connectedAccounts.FirstOrDefault(a => a.PlatformName == "LinkedIn");
        var xAcc = connectedAccounts.FirstOrDefault(a => a.PlatformName == "X");

        return Ok(new {
            linkedIn = new { isConnected = linkedInAcc != null, username = linkedInAcc?.AccountUsername },
            x = new { isConnected = xAcc != null, username = xAcc?.AccountUsername }
        });
    }

    [HttpDelete("{platform}/disconnect")]
    public async Task<IActionResult> Disconnect(string platform)
    {
        var user = await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
        if (user == null) return NotFound("User not found");

        var platformTitle = platform.ToLower() == "x" ? "X" : "LinkedIn";

        var account = await _context.SocialAccounts.FirstOrDefaultAsync(sa => sa.UserId == user.Id && sa.PlatformName == platformTitle);
        if (account != null)
        {
            _context.SocialAccounts.Remove(account);
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    private async Task<string> RefreshLinkedInToken(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", _config["OAuth:LinkedIn:ClientId"]!),
            new KeyValuePair<string, string>("client_secret", _config["OAuth:LinkedIn:ClientSecret"]!)
        });

        var response = await _httpClient.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", content);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json).GetProperty("access_token").GetString()!;
    }
}