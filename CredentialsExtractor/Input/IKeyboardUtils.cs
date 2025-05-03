namespace CredentialsExtractor.Input
{
    public interface IKeyboardUtils
    {
        bool IsCapsLockOn();
        string GetKeyChar(int vkCode, bool shiftActive, bool capsLockOn);
    }
}