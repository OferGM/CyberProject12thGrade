namespace CredentialsExtractor.Core
{
    using CredentialsExtractor.Logging;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public class Win32ApplicationIdentifier : IApplicationIdentifier
    {
        private readonly ILogger _logger;

        public Win32ApplicationIdentifier(ILogger logger)
        {
            _logger = logger;
        }

        public ApplicationInfo GetCurrentApplicationInfo()
        {
            var appInfo = new ApplicationInfo();

            try
            {
                // Get active window handle
                IntPtr activeWindowHandle = GetForegroundWindow();

                if (activeWindowHandle != IntPtr.Zero)
                {
                    // Get window title
                    int length = GetWindowTextLength(activeWindowHandle);
                    if (length > 0)
                    {
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(activeWindowHandle, sb, sb.Capacity);
                        appInfo.WindowTitle = sb.ToString();

                        // Extract application name from window title
                        appInfo.ApplicationName = ExtractApplicationNameFromTitle(appInfo.WindowTitle);
                    }

                    // Get process name
                    uint processId;
                    GetWindowThreadProcessId(activeWindowHandle, out processId);

                    try
                    {
                        using (Process process = Process.GetProcessById((int)processId))
                        {
                            appInfo.ProcessName = process.ProcessName;

                            // For browsers, try to get URL
                            if (IsBrowserProcess(process.ProcessName))
                            {
                                appInfo.URL = TryGetBrowserURL(process.ProcessName, appInfo.WindowTitle);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Error accessing process: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error getting application info: {ex.Message}");
            }

            return appInfo;
        }

        private string ExtractApplicationNameFromTitle(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return "Unknown";

            // Common patterns in window titles
            string[] commonSeparators = new[] { " - ", " | ", ": " };

            foreach (var separator in commonSeparators)
            {
                if (windowTitle.Contains(separator))
                {
                    // Try to find the most meaningful part
                    string[] parts = windowTitle.Split(new[] { separator }, StringSplitOptions.None);

                    // Look for parts that aren't generic terms like "Login", "Sign in", etc.
                    foreach (var part in parts)
                    {
                        if (!IsGenericLoginTerm(part) && part.Length > 3)
                        {
                            return part.Trim();
                        }
                    }

                    // If no good candidate, return first part
                    return parts[0].Trim();
                }
            }

            // If no separators, return as is
            return windowTitle;
        }

        private bool IsGenericLoginTerm(string text)
        {
            string[] genericTerms = new[]
            {
                "login", "sign in", "signin", "log in", "welcome",
                "authentication", "account", "password", "username"
            };

            text = text.ToLower().Trim();
            return genericTerms.Any(term => text.Contains(term));
        }

        private bool IsBrowserProcess(string processName)
        {
            string[] browsers = new[]
            {
                "chrome", "firefox", "msedge", "iexplore", "opera", "brave", "safari"
            };

            return browsers.Any(b => processName.ToLower().Contains(b));
        }

        private string TryGetBrowserURL(string browserName, string windowTitle)
        {
            // For browsers, window titles often have format: "Page Title - Browser Name"
            string title = windowTitle;
            string lowerTitle = title.ToLower();

            if (lowerTitle.EndsWith("chrome") || lowerTitle.EndsWith("edge") ||
                lowerTitle.EndsWith("firefox") || lowerTitle.EndsWith("opera") ||
                lowerTitle.EndsWith("safari") || lowerTitle.EndsWith("internet explorer"))
            {
                int lastDash = title.LastIndexOf(" - ");
                if (lastDash > 0)
                {
                    title = title.Substring(0, lastDash).Trim();
                }
            }

            // Extract domain patterns from title if they exist
            var domainPattern = new Regex(@"(?:https?://)?(?:www\.)?([a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)");
            var match = domainPattern.Match(title);
            if (match.Success)
            {
                return match.Value;
            }

            return "Unknown URL";
        }

        // Win32 API declarations needed for window detection
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}