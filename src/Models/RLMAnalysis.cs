namespace Focus.AI.Models;

public class RLMAnalysis
{
    public bool IsFocused { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> ChunkAnalyses { get; set; } = new();
    public Dictionary<string, string> ContextMemory { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class ChunkAnalysis
{
    public string ChunkType { get; set; } = string.Empty; // "app", "url", "content"
    public string ChunkContent { get; set; } = string.Empty;
    public bool IsRelevant { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}
