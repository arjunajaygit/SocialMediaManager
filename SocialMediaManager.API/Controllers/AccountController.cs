using Microsoft.AspNetCore.Mvc;
using SocialMediaManager.Application.Services;
using SocialMediaManager.API.Models;

namespace SocialMediaManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("link")]
    public async Task<IActionResult> Link([FromBody] LinkAccountRequest request)
    {
        await _accountService.LinkAccountAsync(request.UserId, request.PlatformName, request.AccessToken);
        return Ok(new { Message = $"{request.PlatformName} account linked and encrypted successfully!" });
    }
}