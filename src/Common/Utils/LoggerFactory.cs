using Serilog;
using Serilog.Events;

namespace WebAutomator.Common.Utils;

/// <summary>
///     Factory for creating and configuring Serilog loggers
/// </summary>
public static class LoggerFactory
{
    private static ILogger? _logger;

    /// <summary>
    ///     Creates or gets the configured logger instance
    /// </summary>
    /// <param name="logLevel">Minimum log level (default: Information)</param>
    /// <param name="logDirectory">Directory for log files (default: Logs)</param>
    /// <returns>Configured Serilog logger</returns>
    public static ILogger CreateLogger(LogEventLevel logLevel = LogEventLevel.Information, string logDirectory = "Logs")
    {
        if (_logger != null)
            return _logger;

        // Ensure log directory exists
        if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

        var logFile = Path.Combine(logDirectory, $"web-automator-{DateTime.Now:yyyyMMdd}.log");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFile,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        _logger.Information("Logger initialized - Console and File sinks configured");
        _logger.Information($"Log file: {logFile}");

        return _logger;
    }

    /// <summary>
    ///     Gets the current logger instance
    /// </summary>
    /// <returns>Current logger or creates a new one if not exists</returns>
    public static ILogger GetLogger()
    {
        return _logger ?? CreateLogger();
    }

    /// <summary>
    ///     Closes and flushes the logger
    /// </summary>
    public static void CloseLogger()
    {
        Log.CloseAndFlush();
    }
}