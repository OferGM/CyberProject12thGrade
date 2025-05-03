namespace CredentialsExtractor.Core
{
    public interface ILoginMonitor : System.IDisposable
    {
        void Start();
        void Stop();
    }
}