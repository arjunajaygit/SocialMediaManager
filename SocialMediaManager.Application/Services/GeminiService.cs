using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SocialMediaManager.Application.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new Exception("Gemini Key missing!");
    }

    public async Task<string> GenerateCaptionAsync(string topic)
{
    // FIX: Using the EXACT version and model that worked in your curl command
    var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

    var requestBody = new
    {
        contents = new[]
        {
            new 
            { 
                parts = new[] 
                { 
                    new { text = $"You are a professional social media manager. Write a highly engaging, viral social media post about: {topic}. Include 2-3 relevant hashtags. Do NOT use quotes around the post." } 
                } 
            }
        }
    };

    // Ensure no extra headers are confusing the Google API
    _httpClient.DefaultRequestHeaders.Clear();

    var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Gemini API Error ({response.StatusCode}): {error}");
    }

    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
    
    // Extracting the text from the JSON structure you just saw in the terminal
    var generatedText = jsonResponse
        .GetProperty("candidates")[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString();

    return generatedText ?? "Could not generate text.";
}
} // End of Class