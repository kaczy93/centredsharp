using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CentrED.Utils;

public class ChatGPTClient
{
    private readonly string apiKey;

    public ChatGPTClient(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public string SendPrompt(string prompt)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var body = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = prompt } }
            };
            using var content = new StringContent(JsonSerializer.Serialize(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = client.PostAsync("https://api.openai.com/v1/chat/completions", content).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }
        catch (Exception e)
        {
            Console.WriteLine($"ChatGPT request failed: {e.Message}");
            return string.Empty;
        }
    }
}
