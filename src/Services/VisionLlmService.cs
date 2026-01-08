using Focus.AI.Models;
using Focus.AI.Helpers;

namespace Focus.AI.Services;

public class VisionLlmService
{
    private readonly OllamaClient _ollamaClient;
    private const string Model = "qwen3-vl:4b";

    public VisionLlmService()
    {
        _ollamaClient = new OllamaClient("http://localhost:11434");
    }

    public async Task<VisionAnalysis> AnalyzeAsync(byte[] screenshot, FocusTask task)
    {
        try
        {
            // Check if Ollama is available
            if (!await _ollamaClient.IsAvailableAsync())
            {
                Console.WriteLine("[WARN] Ollama is not running. Start it with: ollama serve");
                return CreateFallbackAnalysis();
            }

            // Convert screenshot to base64
            var images = new List<string>();

            if (screenshot.Length > 0)
            {
                images.Add(Convert.ToBase64String(screenshot));
            }

            // Build prompt
            var prompt = BuildFocusPrompt(task.Task);

            // Call Ollama API
            Console.WriteLine($"[VLLM] Calling Ollama with {images.Count} images...");
            var response = await _ollamaClient.GenerateAsync(Model, prompt, images);

            if (string.IsNullOrEmpty(response))
            {
                Console.WriteLine("[WARN] Empty response from Ollama");
                return CreateFallbackAnalysis();
            }

            // Show raw response from VLLM
            Console.WriteLine("\n" + new string('=', 80));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[VLLM RAW RESPONSE]");
            Console.ResetColor();
            Console.WriteLine(response);
            Console.WriteLine(new string('=', 80) + "\n");

            // Parse response
            var analysis = ParseResponse(response);

            Console.WriteLine($"[VLLM] Analysis: Focused={analysis.IsFocused}");
            return analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] VLLM analysis failed: {ex.Message}");
            return CreateFallbackAnalysis();
        }
    }

    private string BuildFocusPrompt(string focusTask)
    {
        return $@"You are a focus monitoring assistant. The user wants to focus on: ""{focusTask}""

Analyze the provided screen image and determine:
1. Is the screen showing content DIRECTLY related to their focus task?
2. Are there any obvious distractions visible (social media, entertainment, unrelated content)?
3. Is the visible content helping them accomplish their stated goal?

IMPORTANT:
- Focus.AI monitoring application itself is NOT the focus task
- Terminal/code editors are only relevant if studying programming/development
- Be strict: if content doesn't match the task, mark as unfocused

Respond in this exact format:
FOCUSED: yes/no
REASONING: Brief explanation (one sentence)

Example response:
FOCUSED: no
REASONING: Screen shows terminal and monitoring app, not AI study materials.";
    }

    private VisionAnalysis ParseResponse(string response)
    {
        var analysis = new VisionAnalysis
        {
            Timestamp = DateTime.Now
        };

        try
        {
            // Parse the structured response
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("FOCUSED:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = trimmed.Substring(8).Trim().ToLower();
                    analysis.IsFocused = value == "yes" || value == "true";
                }
                else if (trimmed.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
                {
                    analysis.Reasoning = trimmed.Substring(10).Trim();
                }
            }

            analysis.ScreenContext = analysis.IsFocused ? "On task" : "Distracted";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to parse VLLM response: {ex.Message}");
            Console.WriteLine($"[DEBUG] Raw response: {response}");
        }

        return analysis;
    }

    private VisionAnalysis CreateFallbackAnalysis()
    {
        return new VisionAnalysis
        {
            IsFocused = true,
            ScreenContext = "Unable to analyze (Ollama unavailable)",
            Reasoning = "Ollama service is not available. Using fallback analysis.",
            Timestamp = DateTime.Now
        };
    }
}
