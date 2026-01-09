using Focus.AI.Config;
using Focus.AI.Models;
using Focus.AI.Services;

namespace Focus.AI;

class Program
{
    private static OcrResult? _latestOcrResult;
    private static RLMAnalysis? _latestRlmAnalysis;
    private static readonly object _dataLock = new();

    static async Task Main(string[] args)
    {
        // Display welcome banner
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════╗");
        Console.WriteLine("║   FocusBuddy - AI-Powered Focus Monitoring (RLM)   ║");
        Console.WriteLine("╚════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        // Get focus task from user
        Console.Write("What do you want to focus on? ");
        string? userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput))
        {
            Console.WriteLine("No focus task provided. Exiting...");
            return;
        }

        var focusTask = new FocusTask(userInput);

        Console.WriteLine($"\nGreat! I'll help you stay focused on: {focusTask.Task}");
        Console.WriteLine("\n📊 Two-Stage Monitoring Architecture:");
        Console.WriteLine("   Stage 1 (Quick): OCR + RLM Reasoning (qwen3:0.6b) every 5s");
        Console.WriteLine("   Stage 2 (Deep):  Vision Analysis (qwen3-vl:4b) every 15s");
        Console.WriteLine("\nMonitoring will start in 3 seconds...\n");
        await Task.Delay(3000);

        // Initialize configuration
        var config = new MonitoringConfig
        {
            QuickCheckIntervalSeconds = 5,
            DeepAnalysisIntervalSeconds = 15,
            EnableTerminalAlerts = true,
            EnableSystemNotifications = false,
            EnableLogging = true
        };

        // Initialize services
        var services = InitializeServices(config);

        Console.WriteLine("[INFO] FocusBuddy is now monitoring...");
        Console.WriteLine($"[INFO] Quick checks (OCR + RLM) every {config.QuickCheckIntervalSeconds}s");
        Console.WriteLine($"[INFO] Deep analysis (Vision) every {config.DeepAnalysisIntervalSeconds}s");
        Console.WriteLine("[INFO] Press Ctrl+C to stop\n");

        // Setup graceful shutdown
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n\n[INFO] Shutting down FocusBuddy gracefully...");
            Environment.Exit(0);
        };

        // Start two-stage monitoring loops
        var quickCheckTask = RunQuickCheckLoopAsync(services, config, focusTask);
        var deepAnalysisTask = RunDeepAnalysisLoopAsync(services, config, focusTask);

        await Task.WhenAll(quickCheckTask, deepAnalysisTask);
    }

    static (ScreenCaptureService, OcrService, RecursiveLanguageModelService, VisionLlmService, FocusAnalyzer, AlertService) InitializeServices(MonitoringConfig config)
    {
        var screenCapture = new ScreenCaptureService();
        var ocr = new OcrService();
        var rlm = new RecursiveLanguageModelService();
        var vllm = new VisionLlmService();
        var analyzer = new FocusAnalyzer();
        var alert = new AlertService(config);

        return (screenCapture, ocr, rlm, vllm, analyzer, alert);
    }

    /// <summary>
    /// Quick check loop: OCR + RLM reasoning every 5 seconds
    /// </summary>
    static async Task RunQuickCheckLoopAsync(
        (ScreenCaptureService screenCapture, OcrService ocr, RecursiveLanguageModelService rlm, VisionLlmService vllm, FocusAnalyzer analyzer, AlertService alert) services,
        MonitoringConfig config,
        FocusTask focusTask)
    {
        while (true)
        {
            try
            {
                Console.WriteLine("\n[QUICK CHECK] Starting...");

                // Capture screen
                var screenshot = await services.screenCapture.CaptureScreenAsync();

                // Run OCR analysis
                var ocrResult = await services.ocr.AnalyzeScreenAsync(screenshot);

                // Store latest OCR result
                lock (_dataLock)
                {
                    _latestOcrResult = ocrResult;
                }

                // Run RLM analysis with recursive decomposition
                var rlmAnalysis = await services.rlm.AnalyzeAsync(ocrResult, focusTask);

                // Store latest RLM result
                lock (_dataLock)
                {
                    _latestRlmAnalysis = rlmAnalysis;
                }

                // Determine quick focus status
                var focusStatus = services.analyzer.DetermineQuickFocus(rlmAnalysis, focusTask);

                // Display quick check result
                Console.ForegroundColor = focusStatus.State == FocusState.Focused
                    ? ConsoleColor.Green
                    : ConsoleColor.Red;
                Console.WriteLine($"[QUICK] {focusStatus.State} ({focusStatus.Confidence:P0}) - {focusStatus.Reason}");
                Console.ResetColor();

                // Send alert if distracted
                if (focusStatus.State == FocusState.Distracted)
                {
                    await services.alert.NotifyAsync(focusStatus);
                }

                // Wait for next interval
                await Task.Delay(config.QuickCheckIntervalSeconds * 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Quick check error: {ex.Message}");
                await Task.Delay(5000); // Wait before retrying
            }
        }
    }

    /// <summary>
    /// Deep analysis loop: Vision model + context update every 15 seconds
    /// </summary>
    static async Task RunDeepAnalysisLoopAsync(
        (ScreenCaptureService screenCapture, OcrService ocr, RecursiveLanguageModelService rlm, VisionLlmService vllm, FocusAnalyzer analyzer, AlertService alert) services,
        MonitoringConfig config,
        FocusTask focusTask)
    {
        // Initial delay to let quick check run first
        await Task.Delay(7000);

        while (true)
        {
            try
            {
                Console.WriteLine("\n[DEEP ANALYSIS] Starting...");

                // Capture screen at optimal resolution for vision model
                var screenshot = await services.screenCapture.CaptureScreenAsync(maxWidth: 1920);

                // Run vision model analysis
                var visionAnalysis = await services.vllm.AnalyzeAsync(screenshot, focusTask);

                // Update RLM context with deep analysis results
                services.rlm.UpdateContext(visionAnalysis);

                // Get latest RLM analysis
                RLMAnalysis? rlmAnalysis;
                lock (_dataLock)
                {
                    rlmAnalysis = _latestRlmAnalysis;
                }

                // If we have RLM data, combine both analyses
                if (rlmAnalysis != null)
                {
                    var combinedStatus = services.analyzer.DetermineDeepFocus(rlmAnalysis, visionAnalysis, focusTask);

                    Console.ForegroundColor = combinedStatus.State == FocusState.Focused
                        ? ConsoleColor.Green
                        : ConsoleColor.Yellow;
                    Console.WriteLine($"[DEEP] {combinedStatus.State} ({combinedStatus.Confidence:P0}) - Vision: {visionAnalysis.Reasoning}");
                    Console.ResetColor();

                    // Send deep analysis alert
                    await services.alert.NotifyAsync(combinedStatus);
                }
                else
                {
                    Console.WriteLine("[DEEP] Waiting for RLM data...");
                }

                // Wait for next interval
                await Task.Delay(config.DeepAnalysisIntervalSeconds * 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Deep analysis error: {ex.Message}");
                await Task.Delay(10000); // Wait before retrying
            }
        }
    }
}
