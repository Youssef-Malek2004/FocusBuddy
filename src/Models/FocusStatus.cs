namespace Focus.AI.Models;

public enum FocusState
{
    Focused,
    Distracted,
    Away
}

public class FocusStatus
{
    public FocusState State { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; }
}
