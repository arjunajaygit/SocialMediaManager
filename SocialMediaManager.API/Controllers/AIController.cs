using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialMediaManager.Application.Services;

namespace SocialMediaManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AIPolicy")]
public class AIController : ControllerBase
{
    private readonly GeminiService _aiService;

    public AIController(GeminiService aiService)
    {
        _aiService = aiService;
    }

    public class GenerateRequest 
    {
        public string Topic { get; set; } = string.Empty;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        try 
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
                return BadRequest(new { Message = "Topic cannot be empty." });

            var result = await _aiService.GenerateCaptionAsync(request.Topic);
            
            return Ok(new { text = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }
}