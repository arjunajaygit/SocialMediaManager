using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SocialMediaManager.Application.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new Exception("OpenAI Key missing!");
        
        // Attach the VIP Pass for OpenAI
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GenerateCaptionAsync(string topic)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini", // Fast, cheap, and smart
            messages = new[]
            {
                new { 
                    role = "system", 
                    content = "You are a professional social media manager. Write highly engaging, viral social media posts based on the user's topic. Include 2-3 relevant hashtags. Do NOT use quotes around the post." 
                },
                new { 
                    role = "user", 
                    content = $"Write a post about: {topic}" 
                }
            },
            temperature = 0.7
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API Error: {error}");
        }

        // Parse the complex JSON response to grab just the generated text
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        var generatedText = jsonResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return generatedText ?? "Could not generate text.";
    }
}