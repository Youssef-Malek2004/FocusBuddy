namespace Focus.AI.Models;

public class OcrResult
{
    public string ActiveApp { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public List<string> VisibleUrls { get; set; } = new();
    public string ExtractedText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
