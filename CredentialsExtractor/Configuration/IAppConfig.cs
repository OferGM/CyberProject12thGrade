// IAppConfig.cs
namespace CredentialsExtractor.Configuration
{
    public interface IAppConfig
    {
        int ScreenshotStartY { get; }
        int ScreenshotWidth { get; }
        int ScreenshotHeight { get; }
        string LogFilePath { get; }
        string ScreenshotDirectory { get; }
    }
}