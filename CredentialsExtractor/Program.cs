// Program.cs
using CredentialsExtractor.Configuration;
using CredentialsExtractor.Core;
using CredentialsExtractor.Logging;
using CredentialsExtractor.DependencyInjection;

namespace LoginDetectorMonitor
{
    class Program
    {
        private static ILoginMonitor _monitor;
        private static readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            try
            {
                // Initialize configuration
                IAppConfig config = AppConfigFactory.LoadConfig();

                // Setup logging
                ILoggerFactory loggerFactory = new FileLoggerFactory(config.LogFilePath);
                ILogger logger = loggerFactory.CreateLogger();
                logger.Log("Application started");

                Console.WriteLine($"Starting Login Detector Monitor...");
                Console.WriteLine($"Log file: {config.LogFilePath}");
                Console.WriteLine($"Monitoring duration: {config.MaxCaptureDuration} minutes");

                // Create service provider with all dependencies
                ServiceProvider serviceProvider = new ServiceProvider(config, logger);

                // Create and start the login monitor
                _monitor = serviceProvider.GetService<ILoginMonitor>();
                _monitor.Start();

                // Wait for exit signal
                _exitEvent.WaitOne();

                // Cleanup
                _monitor.Stop();
                logger.Log("Application stopped");

                Console.WriteLine("Login Detector Monitor stopped successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}