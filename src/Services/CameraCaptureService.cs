namespace Focus.AI.Services;

public class CameraCaptureService
{
    public async Task<byte[]> CaptureCameraFrameAsync()
    {
        // TODO: Implement camera capture using AVFoundation
        // For now, return empty byte array as stub
        await Task.CompletedTask;

        Console.WriteLine("[DEBUG] Camera frame captured (stub)");
        return Array.Empty<byte>();
    }
}
