// ScreenCapture.cs
using System.Drawing;
using System.Drawing.Imaging;
using CredentialsExtractor.Configuration;
using CredentialsExtractor.Logging;

namespace CredentialsExtractor.Core
{
    public class ScreenCapture : IScreenCapture
    {
        private readonly IAppConfig _config;
        private readonly ILogger _logger;
        private readonly object _captureLock = new object();

        public ScreenCapture(IAppConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string CaptureScreen()
        {
            string screenshotPath = string.Empty;

            lock (_captureLock) // Ensure thread safety for GDI+ operations
            {
                try
                {
                    // Validate screen capture dimensions
                    int width = _config.ScreenshotWidth;
                    int height = _config.ScreenshotHeight;
                    int startY = _config.ScreenshotStartY;

                    // Create a unique filename
                    screenshotPath = Path.Combine(
                        _config.ScreenshotDirectory,
                        $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");

                    // Create the screenshot bitmap outside of using block to allow for fallback
                    Bitmap screenshotBitmap = null;

                    try
                    {
                        // First attempt with configured dimensions
                        screenshotBitmap = new Bitmap(width, height);
                        using (Graphics g = Graphics.FromImage(screenshotBitmap))
                        {
                            // Improve performance with these settings
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

                            // Capture screen area
                            g.CopyFromScreen(0, startY, 0, 0, screenshotBitmap.Size);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Error during screen capture: {ex.Message}");

                        // Dispose the failed bitmap if it was created
                        if (screenshotBitmap != null)
                        {
                            screenshotBitmap.Dispose();
                            screenshotBitmap = null;
                        }

                        // Attempt with more conservative dimensions
                        if (width > 800 && height > 600)
                        {
                            try
                            {
                                screenshotBitmap = new Bitmap(800, 600);
                                using (Graphics g = Graphics.FromImage(screenshotBitmap))
                                {
                                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

                                    g.CopyFromScreen(0, startY, 0, 0, screenshotBitmap.Size);
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                _logger.Log($"Fallback screen capture failed: {fallbackEx.Message}");

                                // Dispose the fallback bitmap if it was created
                                if (screenshotBitmap != null)
                                {
                                    screenshotBitmap.Dispose();
                                }

                                return string.Empty;
                            }
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }

                    // If we got this far, we have a valid screenshot
                    if (screenshotBitmap != null)
                    {
                        try
                        {
                            // Create image encoder based on configured preferences
                            var encoderFactory = new ImageEncoderFactory();
                            IImageEncoder encoder;

                            try
                            {
                                encoder = encoderFactory.CreateEncoder(ImageFormat.Jpeg, 80);
                                screenshotPath = Path.ChangeExtension(screenshotPath, encoder.FileExtension);
                            }
                            catch
                            {
                                // Fall back to PNG if JPEG encoder not available
                                encoder = encoderFactory.CreateEncoder(ImageFormat.Png);
                                screenshotPath = Path.ChangeExtension(screenshotPath, encoder.FileExtension);
                            }

                            // Create directory if it doesn't exist
                            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));

                            // Save the screenshot
                            using (FileStream fs = new FileStream(screenshotPath, FileMode.Create, FileAccess.Write))
                            {
                                encoder.SaveImage(fs, screenshotBitmap);
                            }

                            // Clean up the bitmap
                            screenshotBitmap.Dispose();

                            // Verify the file was created successfully
                            if (!File.Exists(screenshotPath) || new FileInfo(screenshotPath).Length == 0)
                            {
                                _logger.Log("Screenshot file was not created or is empty");
                                return string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"Error saving screenshot: {ex.Message}");

                            // Clean up bitmap on error
                            screenshotBitmap.Dispose();

                            return string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"Screenshot capture error: {ex.Message}");
                    screenshotPath = string.Empty;
                }
            }
            return screenshotPath;
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }
    }
}