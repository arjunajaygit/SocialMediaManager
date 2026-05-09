using Azure;
using Azure.AI.OpenAI;

namespace SocialMediaManager.Infrastructure.Services;

public class OpenAiService
{
    public async Task<string> GenerateCaption(string topic)
    {
        // This is a placeholder. You'll need an OpenAI API Key later!
        // For now, let's simulate the AI response
        await Task.Delay(1000);
        return $"🚀 Just launched my new project about {topic}! #Innovation #Tech";
    }
}