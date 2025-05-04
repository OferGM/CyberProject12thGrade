namespace CredentialsExtractor.Core
{
    public interface IApplicationIdentifier
    {
        ApplicationInfo GetCurrentApplicationInfo();
    }

    // Data structure to hold application information
    public class ApplicationInfo
    {
        public string ApplicationName { get; set; } = "Unknown";
        public string WindowTitle { get; set; } = "Unknown";
        public string URL { get; set; } = "Unknown";
        public string ProcessName { get; set; } = "Unknown";
    }
}