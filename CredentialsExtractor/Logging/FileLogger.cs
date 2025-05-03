using System;
using System.IO;

namespace CredentialsExtractor.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
            try
            {
                File.WriteAllText(_logFilePath, $"Log started at {DateTime.Now}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger initialization error: {ex.Message}");
            }
        }

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            try
            {
                File.AppendAllText(_logFilePath, $"{DateTime.Now:HH:mm:ss.fff}: {message}\n");
            }
            catch { }
        }
    }

    public class FileLoggerFactory : ILoggerFactory
    {
        private readonly string _logFilePath;

        public FileLoggerFactory(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public ILogger CreateLogger()
        {
            return new FileLogger(_logFilePath);
        }
    }
}