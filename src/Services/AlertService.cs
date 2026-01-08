using Focus.AI.Models;
using Focus.AI.Config;

namespace Focus.AI.Services;

public class AlertService
{
    private readonly MonitoringConfig _config;
    private readonly string _logFilePath;

    public AlertService(MonitoringConfig config)
    {
        _config = config;

        // Create logs directory if it doesn't exist
        if (!Directory.Exists(config.LogDirectory))
        {
            Directory.CreateDirectory(config.LogDirectory);
        }

        _logFilePath = Path.Combine(config.LogDirectory, $"focus-session-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public async Task NotifyAsync(FocusStatus status)
    {
        // Terminal notification
        if (_config.EnableTerminalAlerts)
        {
            ShowTerminalNotification(status);
        }

        // macOS system notification
        if (_config.EnableSystemNotifications)
        {
            // TODO: Implement macOS NSUserNotificationCenter
            Console.WriteLine("[DEBUG] System notification would be shown here");
        }

        // Log to file
        if (_config.EnableLogging)
        {
            await LogToFileAsync(status);
        }
    }

    private void ShowTerminalNotification(FocusStatus status)
    {
        var (icon, color) = status.State switch
        {
            FocusState.Focused => ("✓", ConsoleColor.Green),
            FocusState.Distracted => ("⚠", ConsoleColor.Yellow),
            FocusState.Away => ("⏸", ConsoleColor.Gray),
            _ => ("?", ConsoleColor.White)
        };

        Console.ForegroundColor = color;
        Console.WriteLine($"[{status.State.ToString().ToUpper()}] {icon} {status.Reason}");
        Console.ResetColor();
    }

    private async Task LogToFileAsync(FocusStatus status)
    {
        var logEntry = $"[{status.Timestamp:yyyy-MM-dd HH:mm:ss}] {status.State} - {status.Reason} (Confidence: {status.Confidence:P0})\n";
        await File.AppendAllTextAsync(_logFilePath, logEntry);
    }
}
