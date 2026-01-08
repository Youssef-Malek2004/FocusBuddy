using Focus.AI.Config;
using Focus.AI.Models;
using Focus.AI.Services;

namespace Focus.AI;

class Program
{
    private static OcrResult? _latestOcrResult;
    private static readonly object _ocrLock = new();

    static async Task Main(string[] args)
    {
        // Display welcome banner
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║         Focus.AI - Stay on track!      ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
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
        Console.WriteLine("Monitoring will start in 3 seconds...\n");
        await Task.Delay(3000);

        // Initialize configuration
        var config = new MonitoringConfig
        {
            OcrIntervalSeconds = 3,
            VllmIntervalSeconds = 5,
            EnableTerminalAlerts = true,
            EnableSystemNotifications = false, // Disabled for skeleton
            EnableLogging = true
        };

        // Initialize services
        var services = InitializeServices(config);

        Console.WriteLine("[INFO] Focus.AI is now monitoring...");
        Console.WriteLine($"[INFO] OCR checks every {config.OcrIntervalSeconds}s");
        Console.WriteLine($"[INFO] VLLM analysis every {config.VllmIntervalSeconds}s");
        Console.WriteLine("[INFO] Press Ctrl+C to stop\n");

        // Setup graceful shutdown
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n\n[INFO] Shutting down Focus.AI gracefully...");
            Environment.Exit(0);
        };

        // Start monitoring loops
        var ocrTask = RunOcrLoopAsync(services, config);
        var vllmTask = RunVllmLoopAsync(services, config, focusTask);

        await Task.WhenAll(ocrTask, vllmTask);
    }

    static (ScreenCaptureService, OcrService, VisionLlmService, FocusAnalyzer, AlertService) InitializeServices(MonitoringConfig config)
    {
        var screenCapture = new ScreenCaptureService();
        var ocr = new OcrService();
        var vllm = new VisionLlmService();
        var analyzer = new FocusAnalyzer();
        var alert = new AlertService(config);

        return (screenCapture, ocr, vllm, analyzer, alert);
    }

    static async Task RunOcrLoopAsync(
        (ScreenCaptureService screenCapture, OcrService ocr, VisionLlmService vllm, FocusAnalyzer analyzer, AlertService alert) services,
        MonitoringConfig config)
    {
        while (true)
        {
            try
            {
                // Capture screen
                var screenshot = await services.screenCapture.CaptureScreenAsync();

                // Run OCR analysis
                var ocrResult = await services.ocr.AnalyzeScreenAsync(screenshot);

                // Store latest result for VLLM loop
                lock (_ocrLock)
                {
                    _latestOcrResult = ocrResult;
                }

                // Wait for next interval
                await Task.Delay(config.OcrIntervalSeconds * 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] OCR loop error: {ex.Message}");
                await Task.Delay(5000); // Wait before retrying
            }
        }
    }

    static async Task RunVllmLoopAsync(
        (ScreenCaptureService screenCapture, OcrService ocr, VisionLlmService vllm, FocusAnalyzer analyzer, AlertService alert) services,
        MonitoringConfig config,
        FocusTask focusTask)
    {
        // Initial delay to let OCR loop run first
        await Task.Delay(5000);

        while (true)
        {
            try
            {
                // Capture screen at lower resolution for faster VLLM processing
                // 1920px width is plenty for the model to understand context
                var screenshot = await services.screenCapture.CaptureScreenAsync(maxWidth: 1920);

                // Run VLLM analysis
                var vllmResult = await services.vllm.AnalyzeAsync(screenshot, focusTask);

                // Get latest OCR result
                OcrResult? ocrResult;
                lock (_ocrLock)
                {
                    ocrResult = _latestOcrResult;
                }

                // Determine focus status
                var focusStatus = services.analyzer.DetermineFocus(ocrResult, vllmResult, focusTask);

                // Send alerts
                await services.alert.NotifyAsync(focusStatus);

                // Wait for next interval
                await Task.Delay(config.VllmIntervalSeconds * 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] VLLM loop error: {ex.Message}");
                await Task.Delay(10000); // Wait before retrying
            }
        }
    }
}
