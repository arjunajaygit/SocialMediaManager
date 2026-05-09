using Microsoft.AspNetCore.Mvc;
using SocialMediaManager.Application.Services;

namespace SocialMediaManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    public class AuthRequest {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        try {
            var token = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            return Ok(new { Token = token, Message = "Registration successful" });
        } 
        catch (Exception ex) {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthRequest request)
    {
        try {
            var token = _authService.Login(request.Email, request.Password);
            return Ok(new { Token = token, Message = "Login successful" });
        } 
        catch (Exception ex) {
            return Unauthorized(new { Message = ex.Message });
        }
    }
}