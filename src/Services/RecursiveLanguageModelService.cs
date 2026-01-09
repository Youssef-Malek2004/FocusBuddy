using Focus.AI.Models;
using Focus.AI.Helpers;
using System.Text;

namespace Focus.AI.Services;

public class RecursiveLanguageModelService
{
    private readonly OllamaClient _ollamaClient;
    private const string Model = "qwen3:0.6b";
    private Dictionary<string, string> _contextMemory = new();
    private DateTime _lastDeepAnalysis = DateTime.MinValue;

    public RecursiveLanguageModelService()
    {
        _ollamaClient = new OllamaClient("http://localhost:11434");
    }

    /// <summary>
    /// Update context memory from deep VLLM analysis
    /// </summary>
    public void UpdateContext(VisionAnalysis deepAnalysis)
    {
        _contextMemory["last_deep_context"] = deepAnalysis.ScreenContext;
        _contextMemory["last_deep_reasoning"] = deepAnalysis.Reasoning;
        _contextMemory["last_deep_focused"] = deepAnalysis.IsFocused.ToString();
        _lastDeepAnalysis = deepAnalysis.Timestamp;
    }

    /// <summary>
    /// Recursive Language Model analysis - decomposes OCR into chunks and recursively analyzes
    /// </summary>
    public async Task<RLMAnalysis> AnalyzeAsync(OcrResult ocrResult, FocusTask task)
    {
        try
        {
            if (!await _ollamaClient.IsAvailableAsync())
            {
                Console.WriteLine("[WARN] Ollama not running for RLM analysis");
                return CreateFallbackAnalysis();
            }

            var chunkAnalyses = new List<ChunkAnalysis>();

            // Decompose OCR result into analyzable chunks
            var chunks = DecomposeIntoChunks(ocrResult);

            Console.WriteLine($"[RLM] Analyzing {chunks.Count} chunks in parallel...");

            // Analyze chunks in parallel for speed
            var analysisTaskList = chunks.Select(chunk => AnalyzeChunkAsync(chunk, task)).ToList();
            var analyzedChunks = await Task.WhenAll(analysisTaskList);
            chunkAnalyses.AddRange(analyzedChunks);

            // Aggregate chunk analyses into final determination
            var finalAnalysis = await AggregateAnalysesAsync(chunkAnalyses, task);

            Console.WriteLine($"[RLM] Final: Focused={finalAnalysis.IsFocused}, Confidence={finalAnalysis.Confidence:F2}");

            return finalAnalysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] RLM analysis failed: {ex.Message}");
            return CreateFallbackAnalysis();
        }
    }

    /// <summary>
    /// Decompose OCR result into meaningful chunks for recursive analysis
    /// Optimized for speed - max 3 chunks
    /// </summary>
    private List<ChunkAnalysis> DecomposeIntoChunks(OcrResult ocr)
    {
        var chunks = new List<ChunkAnalysis>();

        // Chunk 1: Application context (app + window + URLs combined)
        var contextParts = new List<string>();
        if (!string.IsNullOrEmpty(ocr.ActiveApp))
            contextParts.Add($"App: {ocr.ActiveApp}");
        if (!string.IsNullOrEmpty(ocr.WindowTitle))
            contextParts.Add($"Window: {ocr.WindowTitle}");
        if (ocr.VisibleUrls.Any())
            contextParts.Add($"URLs: {string.Join(", ", ocr.VisibleUrls.Take(3))}");

        if (contextParts.Any())
        {
            chunks.Add(new ChunkAnalysis
            {
                ChunkType = "context",
                ChunkContent = string.Join(" | ", contextParts)
            });
        }

        // Chunk 2-3: Screen text (max 2 chunks, larger size for efficiency)
        if (!string.IsNullOrEmpty(ocr.ExtractedText))
        {
            // Split into max 2 chunks of 600 chars each
            var textChunks = SplitTextIntoChunks(ocr.ExtractedText, maxChunkSize: 600);
            foreach (var (textChunk, index) in textChunks.Take(2).Select((c, i) => (c, i)))
            {
                chunks.Add(new ChunkAnalysis
                {
                    ChunkType = "content",
                    ChunkContent = textChunk
                });
            }
        }

        return chunks;
    }

    /// <summary>
    /// Split long text into smaller chunks for recursive analysis
    /// </summary>
    private List<string> SplitTextIntoChunks(string text, int maxChunkSize)
    {
        var chunks = new List<string>();

        if (text.Length <= maxChunkSize)
        {
            chunks.Add(text);
            return chunks;
        }

        // Split by sentences/lines to avoid cutting mid-sentence
        var sentences = text.Split(new[] { '.', '\n', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > maxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }
            currentChunk.Append(sentence).Append(". ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    /// <summary>
    /// Recursively analyze a single chunk
    /// </summary>
    private async Task<ChunkAnalysis> AnalyzeChunkAsync(ChunkAnalysis chunk, FocusTask task)
    {
        // Build context-aware prompt
        var prompt = BuildChunkPrompt(chunk, task);

        // Call small reasoning model
        var response = await _ollamaClient.GenerateAsync(Model, prompt);

        if (string.IsNullOrEmpty(response))
        {
            chunk.IsRelevant = false;
            chunk.Reasoning = "No response from model";
            return chunk;
        }

        // Parse response
        ParseChunkResponse(response, chunk);

        return chunk;
    }

    /// <summary>
    /// Build prompt for analyzing a single chunk
    /// </summary>
    private string BuildChunkPrompt(ChunkAnalysis chunk, FocusTask task)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Focus Goal: \"{task.Task}\"");
        sb.AppendLine();
        sb.AppendLine($"Screen Info ({chunk.ChunkType}): {chunk.ChunkContent}");
        sb.AppendLine();

        // Add context from previous deep analysis if available
        if (_contextMemory.ContainsKey("last_deep_context"))
        {
            var timeSinceDeep = (DateTime.Now - _lastDeepAnalysis).TotalSeconds;
            if (timeSinceDeep < 30) // Only use recent context
            {
                sb.AppendLine($"Visual Analysis: {_contextMemory["last_deep_context"]}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("Question: Does this screen information show the user IS working on their focus goal?");
        sb.AppendLine();
        sb.AppendLine("Important:");
        sb.AppendLine("- Be strict: content must DIRECTLY relate to the goal");
        sb.AppendLine("- Monitoring/development tools are NOT the focus task itself");
        sb.AppendLine("- Code/terminals are only relevant if the goal is programming");
        sb.AppendLine();
        sb.AppendLine("Format:");
        sb.AppendLine("RELEVANT: yes/no");
        sb.AppendLine("REASONING: Brief explanation.");

        return sb.ToString();
    }

    /// <summary>
    /// Parse the model's response for a chunk analysis
    /// </summary>
    private void ParseChunkResponse(string response, ChunkAnalysis chunk)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("RELEVANT:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed.Substring(9).Trim().ToLower();
                chunk.IsRelevant = value == "yes" || value == "true";
            }
            else if (trimmed.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
            {
                chunk.Reasoning = trimmed.Substring(10).Trim();
            }
        }
    }

    /// <summary>
    /// Aggregate all chunk analyses into final determination
    /// </summary>
    private Task<RLMAnalysis> AggregateAnalysesAsync(List<ChunkAnalysis> chunks, FocusTask task)
    {
        // Count relevant vs irrelevant chunks
        int relevantCount = chunks.Count(c => c.IsRelevant);
        int totalCount = chunks.Count;

        // Calculate base confidence
        double confidence = totalCount > 0 ? (double)relevantCount / totalCount : 0.5;

        // Start with chunk-based determination
        bool isFocused = relevantCount >= Math.Ceiling(totalCount / 2.0); // Majority vote

        // If we have recent deep visual analysis, heavily weight it
        if (_contextMemory.ContainsKey("last_deep_focused"))
        {
            var deepWasFocused = _contextMemory["last_deep_focused"] == "True";
            var timeSinceDeep = (DateTime.Now - _lastDeepAnalysis).TotalSeconds;

            if (timeSinceDeep < 20)
            {
                // Vision analysis is more reliable - weight it 70%
                // If vision says distracted, we're likely distracted
                if (!deepWasFocused)
                {
                    isFocused = false;
                    confidence = Math.Min(confidence, 0.4); // Lower confidence when disagreeing with vision
                }
                // If vision says focused and RLM agrees, boost confidence
                else if (isFocused)
                {
                    confidence = Math.Max(confidence, 0.7);
                }
            }
        }

        // Build concise reasoning
        var reasoning = new StringBuilder();

        // Primary determination
        if (totalCount > 0)
        {
            reasoning.Append($"{relevantCount}/{totalCount} chunks relevant. ");
        }

        // Add most important insight
        var mainInsight = chunks
            .Where(c => !string.IsNullOrEmpty(c.Reasoning))
            .OrderByDescending(c => c.IsRelevant)
            .FirstOrDefault();

        if (mainInsight != null)
        {
            reasoning.Append(mainInsight.Reasoning);
        }

        return Task.FromResult(new RLMAnalysis
        {
            IsFocused = isFocused,
            Reasoning = reasoning.ToString().Trim(),
            Confidence = confidence,
            ChunkAnalyses = chunks.Select(c => $"{c.ChunkType}: {(c.IsRelevant ? "✓" : "✗")} {c.Reasoning}").ToList(),
            ContextMemory = new Dictionary<string, string>(_contextMemory),
            Timestamp = DateTime.Now
        });
    }

    private RLMAnalysis CreateFallbackAnalysis()
    {
        return new RLMAnalysis
        {
            IsFocused = true,
            Reasoning = "RLM service unavailable, assuming focused",
            Confidence = 0.3,
            Timestamp = DateTime.Now
        };
    }
}
