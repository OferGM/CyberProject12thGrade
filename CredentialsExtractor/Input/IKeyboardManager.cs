// IKeyboardManager.cs
namespace CredentialsExtractor.Input
{
    public interface IKeyboardManager : System.IDisposable
    {
        event EventHandler<KeystrokeEventArgs> KeystrokeDetected;
        bool Initialize();
        void EnableKeylogger();
        void DisableKeylogger();
        void StopKeylogger();
    }
}