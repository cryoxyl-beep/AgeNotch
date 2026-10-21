using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GrumphySwarm.Diagnostics;

/// <summary>
/// Simple, thread-safe file logger to record diagnostic logs to %LocalAppData%\GrumphySwarm\app.log.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileLoggerProvider(string? logFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "GrumphySwarm");
            Directory.CreateDirectory(folder);
            _logFilePath = Path.Combine(folder, "app.log");
        }
        else
        {
            _logFilePath = logFilePath;
        }
    }

    public string LogFilePath => _logFilePath;

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    internal void WriteLog(string category, LogLevel level, string message, Exception? exception)
    {
        lock (_lock)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{category}] {message}");
                if (exception != null)
                {
                    sb.AppendLine();
                    sb.Append($"  Exception: {exception.GetType().FullName}: {exception.Message}");
                    sb.AppendLine();
                    sb.Append($"  StackTrace: {exception.StackTrace}");
                }
                sb.AppendLine();

                using (var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(sb.ToString());
                }
            }
            catch
            {
                // Never crash the application from logging failures
            }
        }
    }

    public void Dispose()
    {
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly FileLoggerProvider _provider;

        public FileLogger(string category, FileLoggerProvider provider)
        {
            _category = category;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            _provider.WriteLog(_category, logLevel, message, exception);
        }
    }
}
