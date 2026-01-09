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

    /// <summary>
    /// Quick focus check using only RLM analysis (called every 5s)
    /// </summary>
    public FocusStatus DetermineQuickFocus(RLMAnalysis rlmAnalysis, FocusTask task)
    {
        return new FocusStatus
        {
            State = rlmAnalysis.IsFocused ? FocusState.Focused : FocusState.Distracted,
            Reason = rlmAnalysis.Reasoning,
            Confidence = rlmAnalysis.Confidence,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Deep focus check combining RLM and VLLM (called every 15s)
    /// </summary>
    public FocusStatus DetermineDeepFocus(RLMAnalysis rlmAnalysis, VisionAnalysis visionAnalysis, FocusTask task)
    {
        // Weight vision analysis more heavily for deep checks
        bool isFocused = (rlmAnalysis.IsFocused && visionAnalysis.IsFocused) ||
                        (rlmAnalysis.Confidence > 0.7 && visionAnalysis.IsFocused);

        // Combine reasoning from both sources
        string combinedReason = $"RLM: {rlmAnalysis.Reasoning}\nVision: {visionAnalysis.Reasoning}";

        // Average confidence
        double combinedConfidence = (rlmAnalysis.Confidence + (visionAnalysis.IsFocused ? 0.85 : 0.15)) / 2.0;

        return new FocusStatus
        {
            State = isFocused ? FocusState.Focused : FocusState.Distracted,
            Reason = combinedReason,
            Confidence = combinedConfidence,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
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
