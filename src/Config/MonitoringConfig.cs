namespace Focus.AI.Config;

public class MonitoringConfig
{
    public int OcrIntervalSeconds { get; set; } = 10;
    public int VllmIntervalSeconds { get; set; } = 30;
    public bool EnableCamera { get; set; } = true;
    public bool EnableTerminalAlerts { get; set; } = true;
    public bool EnableSystemNotifications { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string LogDirectory { get; set; } = "logs";
}
