using Microsoft.AspNetCore.Mvc;
using SocialMediaManager.Application.Services;

namespace SocialMediaManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly MediaService _mediaService;

    public MediaController(MediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        try 
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded. Did you select an image?" });

            using var stream = file.OpenReadStream();
            var url = await _mediaService.UploadImageAsync(stream, file.FileName);
            
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}