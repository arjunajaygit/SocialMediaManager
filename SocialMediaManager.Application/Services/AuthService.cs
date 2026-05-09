using SocialMediaManager.Domain.Entities;
using SocialMediaManager.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace SocialMediaManager.Application.Services;

public class AuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> RegisterAsync(string username, string email, string password)
    {
        // 1. Check if email exists
        if (_context.Users.Any(u => u.Email == email))
            throw new Exception("Email already exists.");

        // 2. Hash the password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // 3. Create the user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 4. Return a login token immediately so they don't have to log in again
        return GenerateJwtToken(user);
    }

    public string Login(string email, string password)
    {
        // 1. Find user
        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
            throw new Exception("Invalid email or password.");

        // 2. Verify password
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!isValid)
            throw new Exception("Invalid email or password.");

        // 3. Generate token
        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        // We use a secret key to sign the token so hackers can't forge it
        var secretKey = _configuration["JwtSettings:Secret"] ?? "SuperSecretKeyThatNeedsToBeVeryLongToWorkProperly12345!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: "SocialSync",
            audience: "SocialSyncApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7), // Token lasts 7 days
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}