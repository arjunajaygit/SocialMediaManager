using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SocialMediaManager.Application.Services;

public class MediaService
{
    private readonly Cloudinary _cloudinary;

    public MediaService(IConfiguration config)
    {
        var acc = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(acc);
    }

    // Line 22: Using pure C# Stream here, NO IFormFile!
    public async Task<string> UploadImageAsync(Stream fileStream, string fileName)
    {
        var uploadResult = new ImageUploadResult();

        if (fileStream.Length > 0)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Transformation = new Transformation().Height(1080).Width(1920).Crop("limit")
            };
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }

        // NEW: If Cloudinary rejected it, throw their exact error message!
        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Error: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString() ?? throw new Exception("Image upload failed for an unknown reason.");
    }
}