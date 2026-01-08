using Focus.AI.Models;

namespace Focus.AI.Services;

public class FocusAnalyzer
{
    private readonly HashSet<string> _distractionApps = new()
    {
        "YouTube", "Netflix", "Twitter", "Facebook", "Instagram",
        "TikTok", "Reddit", "Discord", "Slack", "Messages"
    };

    private readonly HashSet<string> _distractionDomains = new()
    {
        "youtube.com", "netflix.com", "twitter.com", "facebook.com",
        "instagram.com", "tiktok.com", "reddit.com"
    };

    public FocusStatus DetermineFocus(OcrResult? ocr, VisionAnalysis vision, FocusTask task)
    {
        // Check OCR for distraction indicators
        bool hasDistractionApp = ocr != null && CheckDistractionApp(ocr.ActiveApp);
        bool hasDistractionUrl = ocr != null && CheckDistractionUrls(ocr.VisibleUrls);

        // Combine with VLLM analysis
        if (hasDistractionApp || hasDistractionUrl || !vision.IsFocused)
        {
            string reason = hasDistractionApp
                ? $"Distraction app detected: {ocr?.ActiveApp}"
                : hasDistractionUrl
                ? "Distraction website detected"
                : vision.Reasoning;

            return new FocusStatus
            {
                State = FocusState.Distracted,
                Reason = reason,
                Confidence = 0.8,
                Timestamp = DateTime.Now
            };
        }

        return new FocusStatus
        {
            State = FocusState.Focused,
            Reason = $"Working on: {task.Task}",
            Confidence = 0.85,
            Timestamp = DateTime.Now
        };
    }

    private bool CheckDistractionApp(string appName)
    {
        return _distractionApps.Any(app =>
            appName.Contains(app, StringComparison.OrdinalIgnoreCase));
    }

    private bool CheckDistractionUrls(List<string> urls)
    {
        return urls.Any(url =>
            _distractionDomains.Any(domain =>
                url.Contains(domain, StringComparison.OrdinalIgnoreCase)));
    }
}
