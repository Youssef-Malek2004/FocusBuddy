using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Focus.AI.Services;

public class WindowInfoService
{
    public async Task<(string appName, string windowTitle)> GetActiveWindowInfoAsync()
    {
        try
        {
            // Get active application name
            var appName = await GetActiveAppNameAsync();

            // Get active window title
            var windowTitle = await GetActiveWindowTitleAsync();

            return (appName, windowTitle);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get window info: {ex.Message}");
            return ("Unknown", "Unknown");
        }
    }

    private async Task<string> GetActiveAppNameAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = "-e \"tell application \\\"System Events\\\" to get name of first application process whose frontmost is true\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"[ERROR] AppleScript error (app name): {error}");
                }

                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get app name: {ex.Message}");
        }

        return "Unknown";
    }

    private async Task<string> GetActiveWindowTitleAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = "-e \"tell application \\\"System Events\\\" to get name of window 1 of first application process whose frontmost is true\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // If there's an error or empty output, return empty string (not "Unknown")
                if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(output))
                {
                    return string.Empty;
                }

                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get window title: {ex.Message}");
        }

        return string.Empty;
    }

    public List<string> ExtractUrls(string text)
    {
        var urls = new List<string>();

        // Regex pattern for URLs
        var urlPattern = @"(https?://[^\s]+)|(www\.[^\s]+)|([a-zA-Z0-9-]+\.(com|net|org|edu|gov|io|ai|co)[^\s]*)";
        var matches = Regex.Matches(text, urlPattern);

        foreach (Match match in matches)
        {
            urls.Add(match.Value);
        }

        return urls;
    }
}
