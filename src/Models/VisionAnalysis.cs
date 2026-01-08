namespace Focus.AI.Models;

public class VisionAnalysis
{
    public bool IsFocused { get; set; }
    public string ScreenContext { get; set; } = string.Empty;
    public bool UserPresent { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
