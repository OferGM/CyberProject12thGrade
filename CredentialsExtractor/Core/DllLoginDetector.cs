using System;
using System.IO;
using System.Collections.Generic;
using CredentialsExtractor.Configuration;
using CredentialsExtractor.Logging;
using CredentialsExtractor.Native;

namespace CredentialsExtractor.Core
{
    public class DllLoginDetector : ILoginPageDetector, IDisposable
    {
        private readonly IAppConfig _config;
        private readonly ILogger _logger;
        private readonly double _confidenceThreshold;
        private int _consecutiveFailures = 0;
        private int _adaptiveDelayMs = 0;
        private readonly object _detectionLock = new object();
        private bool _isDisposed = false;

        public DllLoginDetector(IAppConfig config, ILogger logger, double confidenceThreshold = 0.6)
        {
            _config = config;
            _logger = logger;
            _confidenceThreshold = confidenceThreshold;

            try
            {
                // Check if DLL exists
                if (!File.Exists(_config.LoginDetectorDllPath))
                {
                    throw new FileNotFoundException($"LoginDetector DLL not found at: {_config.LoginDetectorDllPath}");
                }

                // Copy DLL to application directory if it's not already there
                string localDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoginDetectorDLL.dll");
                if (!File.Exists(localDllPath) ||
                    File.GetLastWriteTime(localDllPath) < File.GetLastWriteTime(_config.LoginDetectorDllPath))
                {
                    _logger.Log($"Copying DLL from {_config.LoginDetectorDllPath} to {localDllPath}");
                    File.Copy(_config.LoginDetectorDllPath, localDllPath, true);
                }

                // Also copy OpenCV DLL if it's available
                string opencvDllSource = Path.Combine(Path.GetDirectoryName(_config.LoginDetectorDllPath), "opencv_world490.dll");
                string opencvDllDest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "opencv_world490.dll");

                if (File.Exists(opencvDllSource) &&
                   (!File.Exists(opencvDllDest) ||
                    File.GetLastWriteTime(opencvDllDest) < File.GetLastWriteTime(opencvDllSource)))
                {
                    _logger.Log($"Copying OpenCV DLL from {opencvDllSource} to {opencvDllDest}");
                    File.Copy(opencvDllSource, opencvDllDest, true);
                }

                // Initialize the native library
                _logger.Log($"Initializing LoginDetector DLL from {localDllPath}");
                if (LoginDetectorNative.Initialize(localDllPath))
                {
                    _logger.Log("LoginDetector DLL initialized successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to initialize LoginDetector DLL: {ex.Message}");
                throw;
            }
        }

        public bool IsLoginPage(string screenshotPath)
        {
            lock (_detectionLock) // Ensure only one detection process runs at a time
            {
                try
                {
                    _logger.Log($"Starting login detection for: {screenshotPath}");

                    // Apply adaptive delay if I've had consecutive failures
                    if (_adaptiveDelayMs > 0)
                    {
                        System.Threading.Thread.Sleep(_adaptiveDelayMs);
                    }

                    // Call the DLL function to detect login page
                    IntPtr resultPtr = LoginDetectorNative.DetectLoginPage(screenshotPath, _confidenceThreshold);

                    if (resultPtr == IntPtr.Zero)
                    {
                        _logger.Log("DLL detection failed: Null result returned");
                        _consecutiveFailures++;
                        AdjustAdaptiveDelay();
                        return false;
                    }

                    try
                    {
                        // Extract the detection result
                        var result = LoginDetectorNative.ExtractDetectionResult(resultPtr);

                        if (result == null)
                        {
                            _logger.Log("Failed to extract detection result");
                            _consecutiveFailures++;
                            AdjustAdaptiveDelay();
                            return false;
                        }

                        // Extract fields and errors if needed
                        if (result.Value.FieldCount > 0 && result.Value.Fields != IntPtr.Zero)
                        {
                            var fields = LoginDetectorNative.ExtractFields(result.Value.Fields, result.Value.FieldCount);
                            _logger.Log($"Detected {fields.Count} form fields");

                            // Log field details for debugging
                            foreach (var field in fields)
                            {
                                _logger.Log($"Field: {field.Type}, Content: {field.Content}");
                            }
                        }

                        if (result.Value.ErrorCount > 0 && result.Value.Errors != IntPtr.Zero)
                        {
                            var errors = LoginDetectorNative.ExtractErrors(result.Value.Errors, result.Value.ErrorCount);
                            foreach (var error in errors)
                            {
                                _logger.Log($"Detection error: {error}");
                            }
                        }

                        _logger.Log($"Login detection complete. Is login page: {result.Value.IsLoginPage}, Confidence: {result.Value.Confidence:F2}, Time: {result.Value.ExecutionTimeMs}ms");

                        // Reset failure counter on success
                        _consecutiveFailures = 0;
                        _adaptiveDelayMs = 0;

                        return result.Value.IsLoginPage;
                    }
                    finally
                    {
                        // Always free the detection result to avoid memory leaks
                        LoginDetectorNative.FreeDetectionResult(resultPtr);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"Login detection error: {ex.Message}");

                    _consecutiveFailures++;
                    AdjustAdaptiveDelay();
                    return false;
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
                    // Clean up managed resources
                    try
                    {
                        LoginDetectorNative.Cleanup();
                        _logger.Log("LoginDetector DLL resources released");
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Error cleaning up LoginDetector DLL: {ex.Message}");
                    }
                }

                _isDisposed = true;
            }
        }

        ~DllLoginDetector()
        {
            Dispose(false);
        }
    }
}