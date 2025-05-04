// ILoggerFactory.cs
namespace CredentialsExtractor.Logging
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger();
    }
}