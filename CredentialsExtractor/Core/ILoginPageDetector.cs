// ILoginPageDetector.cs
namespace CredentialsExtractor.Core
{
    public interface ILoginPageDetector
    {
        bool IsLoginPage(string screenshotPath);
    }
}