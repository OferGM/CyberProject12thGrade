using System;
using System.Diagnostics;
using System.Threading;
using CredentialsExtractor.Configuration;
using CredentialsExtractor.Logging;

namespace CredentialsExtractor.Core
{
    public class ExternalProcessLoginDetector : ILoginPageDetector
    {
        private readonly IAppConfig _config;
        private readonly ILogger _logger;
        private readonly object _detectionLock = new object();
        private Stopwatch _processingTimeWatch = new Stopwatch();
        private int _consecutiveFailures = 0;
        private int _adaptiveDelayMs = 0;

        public ExternalProcessLoginDetector(IAppConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public bool IsLoginPage(string screenshotPath)
        {
            lock (_detectionLock) // Ensure only one detection process runs at a time
            {
                try
                {
                    _logger.Log($"Starting login detection for: {screenshotPath}");
                    _logger.Log($"Using detector at: {_config.LoginDetectorPath}");
                    // Track processing time for adaptive scaling
                    _processingTimeWatch.Restart();

                    // Apply adaptive delay if I've had consecutive failures
                    if (_adaptiveDelayMs > 0)
                    {
                        Thread.Sleep(_adaptiveDelayMs);
                    }

                    // Call the external detector process
                    using (Process process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = _config.LoginDetectorPath,
                            Arguments = $"1 \"{screenshotPath}\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        process.Start();

                        // Add timeout to prevent hanging
                        if (!process.WaitForExit(5000)) // 5 second timeout
                        {
                            _logger.Log("LoginDetector process timed out, killing process");
                            try { process.Kill(); } catch { }
                            _consecutiveFailures++;
                            AdjustAdaptiveDelay();
                            return false;
                        }

                        // Reset failure counter on success
                        _consecutiveFailures = 0;
                        _adaptiveDelayMs = 0;

                        _processingTimeWatch.Stop();
                        string output = process.StandardOutput.ReadToEnd();
                        _logger.Log($"Detector output: {output}");

                        // Check exit code
                        _logger.Log($"Detector exit code: {process.ExitCode}");
                        return output.Trim().EndsWith("true", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"Login detection error: {ex.Message}");

                    _consecutiveFailures++;
                    AdjustAdaptiveDelay();
                    return false;
                }
                finally
                {
                    _processingTimeWatch.Stop();
                }
            }
        }

        // Adjust delay based on consecutive failures to prevent CPU thrashing
        private void AdjustAdaptiveDelay()
        {
            if (_consecutiveFailures > 5)
            {
                // Exponential backoff with maximum of 5 seconds
                _adaptiveDelayMs = Math.Min(5000, 100 * (int)Math.Pow(2, _consecutiveFailures - 5));
                _logger.Log($"Adding adaptive delay of {_adaptiveDelayMs}ms after {_consecutiveFailures} consecutive failures");
            }
        }
    }
}