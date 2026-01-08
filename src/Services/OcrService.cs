using Focus.AI.Models;
using System.Diagnostics;

namespace Focus.AI.Services;

public class OcrService
{
    private readonly WindowInfoService _windowInfo;
    private readonly string _swiftOcrPath;

    public OcrService()
    {
        _windowInfo = new WindowInfoService();

        // Path to Swift OCR helper
        _swiftOcrPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..",
            "Helpers", "VisionOCR.swift"
        );
    }

    public async Task<OcrResult> AnalyzeScreenAsync(byte[] screenshot)
    {
        try
        {
            // Get active window and app information
            var (appName, windowTitle) = await _windowInfo.GetActiveWindowInfoAsync();

            // Perform actual OCR using Apple Vision Framework via Swift
            string extractedText = await PerformVisionOcrAsync(screenshot);

            // Summarize OCR text to avoid overwhelming the VLLM
            string summarizedText = SummarizeOcrText(extractedText, appName);

            // Extract URLs from extracted text
            var urls = _windowInfo.ExtractUrls(extractedText);

            var result = new OcrResult
            {
                ActiveApp = appName,
                WindowTitle = windowTitle,
                VisibleUrls = urls,
                ExtractedText = summarizedText,
                Timestamp = DateTime.Now
            };

            // Log summary without printing text (too verbose)
            Console.WriteLine($"[OCR] {result.ActiveApp} - Extracted {extractedText.Length} chars");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OCR analysis failed: {ex.Message}");

            return new OcrResult
            {
                ActiveApp = "Error",
                WindowTitle = "Error",
                VisibleUrls = new List<string>(),
                ExtractedText = "",
                Timestamp = DateTime.Now
            };
        }
    }

    private async Task<string> PerformVisionOcrAsync(byte[] screenshot)
    {
        try
        {
            // Save screenshot to temp file
            string tempFile = Path.Combine(Path.GetTempPath(), $"focusai_ocr_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(tempFile, screenshot);

            try
            {
                // Call Swift helper
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/swift",
                    Arguments = $"\"{_swiftOcrPath}\" \"{tempFile}\"",
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

                    if (!string.IsNullOrEmpty(error) && error.Contains("ERROR"))
                    {
                        Console.WriteLine($"[WARN] Vision OCR error: {error}");
                        return string.Empty;
                    }

                    return output.Trim();
                }
            }
            finally
            {
                // Cleanup temp file
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Vision OCR failed: {ex.Message}");
        }

        return string.Empty;
    }

    private string SummarizeOcrText(string fullText, string appName)
    {
        if (string.IsNullOrEmpty(fullText))
            return string.Empty;

        // Limit total characters to avoid overwhelming VLLM
        const int maxChars = 1500;

        if (fullText.Length <= maxChars)
            return fullText;

        // Take first 1000 chars and last 500 chars to capture context
        // This gives both what's at the top of screen and current focus area
        string beginning = fullText.Substring(0, 1000);
        string ending = fullText.Length > 500
            ? fullText.Substring(fullText.Length - 500)
            : "";

        return $"{beginning}\n...[truncated]...\n{ending}";
    }
}
