using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CredentialsExtractor.Configuration;
using CredentialsExtractor.Logging;
using System.Text;

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
                    // Create a unique filename
                    screenshotPath = Path.Combine(
                        _config.ScreenshotDirectory,
                        $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");

                    // Capture the screenshot with specified dimensions
                    using (Bitmap screenshot = new Bitmap(_config.ScreenshotWidth, _config.ScreenshotHeight))
                    {
                        using (Graphics g = Graphics.FromImage(screenshot))
                        {
                            // Improve performance with these settings
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

                            // Capture screen area
                            g.CopyFromScreen(0, _config.ScreenshotStartY, 0, 0, screenshot.Size);
                        }

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

                        // Save the screenshot
                        using (FileStream fs = new FileStream(screenshotPath, FileMode.Create, FileAccess.Write))
                        {
                            encoder.SaveImage(fs, screenshot);
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