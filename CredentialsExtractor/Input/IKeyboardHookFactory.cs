//IKeyboardHookFactory.cs
namespace CredentialsExtractor.Input
{
    public interface IKeyboardHookFactory
    {
        IKeyboardHook CreateKeyboardHook();
    }
}