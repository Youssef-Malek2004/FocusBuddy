# Focus.AI

A C# console application that helps you stay focused by monitoring your screen and camera using a hybrid AI architecture.

## Architecture

**Hybrid Monitoring System:**
- **Fast OCR Loop** (every 10s): Apple Vision Framework for text/window detection
- **Deep Analysis Loop** (every 30s): Qwen3-VL-8B for screen context + camera analysis

**Total RAM Usage:** ~6-7GB (fits comfortably in 18GB Mac)

## Features

- Terminal-based focus monitoring
- Real-time distraction detection
- Session logging
- Graceful shutdown (Ctrl+C)
- Configurable monitoring intervals

## Prerequisites

### 1. Install .NET SDK 8+
```bash
# Check if already installed
dotnet --version

# If not installed, download from:
# https://dotnet.microsoft.com/download
```

### 2. Install Ollama
```bash
brew install ollama
```

### 3. Pull Qwen3-VL-8B Model
```bash
ollama pull qwen3-vl:8b
```

This will download ~6.1GB. Verify installation:
```bash
ollama list
```

### 4. Grant macOS Permissions
Focus.AI requires the following permissions (will be prompted on first run):
- **Screen Recording**: System Preferences → Privacy & Security → Screen Recording
- **Camera**: System Preferences → Privacy & Security → Camera
- **Accessibility**: System Preferences → Privacy & Security → Accessibility

## Project Structure

```
Focus.AI/
├── Focus.AI.sln                      # Solution file
├── Focus.AI/
│   ├── Focus.AI.csproj               # Project file
│   ├── Program.cs                    # Entry point, main monitoring loops
│   ├── Config/
│   │   └── MonitoringConfig.cs       # Configuration settings
│   ├── Services/
│   │   ├── ScreenCaptureService.cs   # Screen capture (stub)
│   │   ├── CameraCaptureService.cs   # Camera capture (stub)
│   │   ├── OcrService.cs             # Apple Vision Framework OCR (stub)
│   │   ├── VisionLlmService.cs       # Ollama/Qwen3-VL client (stub)
│   │   ├── FocusAnalyzer.cs          # Focus determination logic
│   │   └── AlertService.cs           # Notifications & logging
│   ├── Models/
│   │   ├── FocusTask.cs              # User's focus task
│   │   ├── OcrResult.cs              # OCR output
│   │   ├── VisionAnalysis.cs         # VLLM analysis result
│   │   └── FocusStatus.cs            # Focus state (Focused/Distracted/Away)
│   └── Helpers/
│       └── OllamaClient.cs           # HTTP client for Ollama API
└── logs/
    └── focus-session-{date}.log      # Session logs
```

## Build & Run

### Build the Project
```bash
dotnet build Focus.AI.sln
```

### Run Focus.AI
```bash
dotnet run --project Focus.AI/Focus.AI.csproj
```

You'll be prompted:
```
What do you want to focus on? _
```

Enter your task (e.g., "Writing my research paper") and Focus.AI will start monitoring!

## Current Status: Skeleton Implementation

This is the **skeleton version** of Focus.AI. The following components have **stub implementations**:

- ✅ **Complete**: Project structure, models, configuration
- ✅ **Complete**: Main application loop and monitoring logic
- ✅ **Complete**: Focus analyzer with distraction detection rules
- ✅ **Complete**: Alert service with terminal notifications and logging
- ⏳ **Stub**: Screen capture (needs macOS Core Graphics integration)
- ⏳ **Stub**: Camera capture (needs AVFoundation integration)
- ⏳ **Stub**: OCR service (needs Apple Vision Framework integration)
- ⏳ **Stub**: VLLM service (needs full Ollama API integration)

### What Works Now

You can run the application and it will:
1. Ask for your focus task
2. Display monitoring status
3. Run both loops (OCR + VLLM) with stub data
4. Show focus status alerts in terminal
5. Log session data to `logs/focus-session-{date}.log`

### What Needs Implementation

The stub services need to be replaced with actual implementations:

1. **ScreenCaptureService**: Use `CGWindowListCreateImage()` via P/Invoke
2. **CameraCaptureService**: Use AVFoundation via Xamarin.Mac or P/Invoke
3. **OcrService**: Use Apple Vision Framework (`VNRecognizeTextRequest`)
4. **VisionLlmService**: Complete Ollama API integration with image encoding

## Configuration

Edit `MonitoringConfig` in `Program.cs` to adjust settings:

```csharp
var config = new MonitoringConfig
{
    OcrIntervalSeconds = 10,        // Fast loop interval
    VllmIntervalSeconds = 30,       // Deep analysis interval
    EnableCamera = true,            // Use camera for presence detection
    EnableTerminalAlerts = true,    // Show alerts in terminal
    EnableSystemNotifications = false, // macOS notifications (TODO)
    EnableLogging = true,           // Log to file
    LogDirectory = "logs"           // Log directory
};
```

## Distraction Detection

Focus.AI detects distractions from these apps/sites by default:
- YouTube, Netflix, Twitter, Facebook, Instagram
- TikTok, Reddit, Discord, Slack, Messages

Add more in `Services/FocusAnalyzer.cs`:
```csharp
private readonly HashSet<string> _distractionApps = new()
{
    "YouTube", "Netflix", // ... add more
};
```

## Alert Examples

```
[FOCUSED] ✓ Working on: Writing my research paper
[DISTRACTED] ⚠ Distraction app detected: YouTube
[AWAY] ⏸ User not present at desk
```

## Session Logs

Logs are saved to `logs/focus-session-{date}.log`:

```
[2026-01-07 14:30:15] Focused - Working on: Writing my research paper (Confidence: 85%)
[2026-01-07 14:32:30] Distracted - Distraction app detected: YouTube (Confidence: 80%)
[2026-01-07 14:35:45] Focused - Working on: Writing my research paper (Confidence: 85%)
```

## Next Steps (Post-Skeleton)

1. Implement screen capture using macOS Core Graphics
2. Implement camera capture using AVFoundation
3. Implement Apple Vision Framework OCR
4. Complete Ollama API integration with Qwen3-VL-8B
5. Add macOS system notifications
6. Add focus statistics dashboard
7. Add Pomodoro timer integration
8. Add custom distraction lists

## Technical Stack

- **Language**: C# (.NET 9)
- **Vision LLM**: Qwen3-VL-8B (via Ollama)
- **OCR**: Apple Vision Framework (native macOS)
- **Screen Capture**: macOS Core Graphics (CGWindowListCreateImage)
- **Camera**: AVFoundation
- **Platform**: macOS 12+

## Why This Architecture?

- **Hybrid approach**: Fast OCR (every 10s) + Deep VLLM (every 30s) = efficient + accurate
- **Apple Vision Framework**: Native, optimized for M-series, zero-cost
- **Qwen3-VL-8B**: Best-in-class benchmarks (97% DocVQA), GUI understanding, only 6.1GB RAM
- **Resource-efficient**: Total ~6-7GB RAM usage, plenty of headroom on 18GB Mac

## License

MIT

## Contributing

This is a skeleton implementation. Contributions welcome for:
- Native macOS integrations (screen, camera, OCR)
- Enhanced distraction detection
- UI improvements
- Additional alert methods
