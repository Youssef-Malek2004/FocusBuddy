using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Focus.AI.Helpers;

public class OllamaResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public class OllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaClient(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    public async Task<string> GenerateAsync(string model, string prompt, List<string>? base64Images = null)
    {
        var requestBody = new
        {
            model = model,
            prompt = prompt,
            images = base64Images ?? new List<string>(),
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse the Ollama response
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseBody);

            return ollamaResponse?.Response ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] Ollama API HTTP error: {ex.Message}");
            Console.WriteLine($"[ERROR] Make sure Ollama is running: ollama serve");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ollama API call failed: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
