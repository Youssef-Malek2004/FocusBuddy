using Focus.AI.Services.Native;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.InteropServices;

namespace Focus.AI.Services;

public class ScreenCaptureService
{
    public async Task<byte[]> CaptureScreenAsync(int? maxWidth = null)
    {
        return await Task.Run(() =>
        {
            IntPtr cgImage = IntPtr.Zero;
            IntPtr dataProvider = IntPtr.Zero;
            IntPtr cfData = IntPtr.Zero;

            try
            {
                // Get main display ID
                uint displayID = CoreGraphics.CGMainDisplayID();

                // Capture the display
                cgImage = CoreGraphics.CGDisplayCreateImage(displayID);
                if (cgImage == IntPtr.Zero)
                {
                    throw new Exception("Failed to capture screen");
                }

                // Get image dimensions
                var width = (int)CoreGraphics.CGImageGetWidth(cgImage);
                var height = (int)CoreGraphics.CGImageGetHeight(cgImage);
                var bytesPerRow = (int)CoreGraphics.CGImageGetBytesPerRow(cgImage);

                // Get pixel data
                dataProvider = CoreGraphics.CGImageGetDataProvider(cgImage);
                cfData = CoreGraphics.CGDataProviderCopyData(dataProvider);
                IntPtr dataPtr = CoreGraphics.CFDataGetBytePtr(cfData);
                long dataLength = CoreGraphics.CFDataGetLength(cfData);

                // Copy pixel data to managed array
                byte[] pixelData = new byte[dataLength];
                Marshal.Copy(dataPtr, pixelData, 0, (int)dataLength);

                // Convert BGRA to RGBA (macOS uses BGRA format)
                for (int i = 0; i < pixelData.Length; i += 4)
                {
                    byte b = pixelData[i];
                    byte r = pixelData[i + 2];
                    pixelData[i] = r;
                    pixelData[i + 2] = b;
                }

                // Create ImageSharp image from pixel data
                using var image = Image.LoadPixelData<Rgba32>(pixelData, width, height);

                // Resize if maxWidth specified (for faster VLLM processing)
                if (maxWidth.HasValue && width > maxWidth.Value)
                {
                    int newHeight = (int)((float)height / width * maxWidth.Value);
                    image.Mutate(x => x.Resize(maxWidth.Value, newHeight));
                    Console.WriteLine($"[DEBUG] Screen captured and resized: {width}x{height} → {maxWidth}x{newHeight}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Screen captured: {width}x{height}");
                }

                // Convert to PNG byte array
                using var ms = new MemoryStream();
                image.Save(ms, new PngEncoder());

                Console.WriteLine($"[DEBUG] Image size: {ms.Length} bytes");
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Screen capture failed: {ex.Message}");
                return Array.Empty<byte>();
            }
            finally
            {
                // Clean up native resources
                if (cfData != IntPtr.Zero)
                    CoreGraphics.CFRelease(cfData);
                if (cgImage != IntPtr.Zero)
                    CoreGraphics.CFRelease(cgImage);
            }
        });
    }
}
