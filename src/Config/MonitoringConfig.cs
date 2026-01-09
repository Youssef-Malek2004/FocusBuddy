namespace Focus.AI.Config;

public class MonitoringConfig
{
    // Quick stage: OCR + RLM (fast reasoning model)
    public int QuickCheckIntervalSeconds { get; set; } = 5;

    // Deep stage: Screenshot + VLLM (vision model)
    public int DeepAnalysisIntervalSeconds { get; set; } = 15;

    public bool EnableCamera { get; set; } = true;
    public bool EnableTerminalAlerts { get; set; } = true;
    public bool EnableSystemNotifications { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string LogDirectory { get; set; } = "logs";
}
