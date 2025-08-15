using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace FireflySR.Proxy.Common
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static string _consoleTitle = "Logger";

        public static void Init(string level = "INFO", string? consoleTitle = null)
        {
            if (!string.IsNullOrWhiteSpace(consoleTitle))
            {
                _consoleTitle = consoleTitle;
                Console.Title = _consoleTitle;
            }

            string baseName = "Proxy";
            RotateLogs(baseName);

            string logFilePath = $"{baseName}.log";
            string levelStr = level.Trim().ToUpperInvariant();
            LogEventLevel minimumLevel = ParseLogLevel(levelStr);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .Enrich.WithProperty("Tag", "")
                .WriteTo.File(
                    path: logFilePath,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Tag}] {Message:lj}{NewLine}",
                    restrictedToMinimumLevel: minimumLevel,
                    rollingInterval: RollingInterval.Infinite,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromMilliseconds(200)
                )
                .CreateLogger();

            if (!IsRecognizedLogLevel(levelStr))
                Warning($"Unknown log level '{levelStr}', defaulting to INFO.");
        }

        public static void Close()
        {
            Log.CloseAndFlush();
        }

        private static void RotateLogs(string baseName)
        {
            try
            {
                string todayPath = $"{baseName}.log";
                if (!File.Exists(todayPath))
                    return;

                DateTime lastWrite = File.GetLastWriteTime(todayPath);
                if (lastWrite.Date == DateTime.Now.Date)
                    return;

                int i = 1;
                while (File.Exists($"{baseName}-{i}.log"))
                    i++;

                for (int j = i - 1; j >= 1; j--)
                    File.Move($"{baseName}-{j}.log", $"{baseName}-{j + 1}.log", overwrite: true);

                File.Move(todayPath, $"{baseName}-1.log", overwrite: true);
            }
            catch { }
        }

        private static LogEventLevel ParseLogLevel(string? level)
        {
            return level switch
            {
                "INFO" => LogEventLevel.Information,
                "HINT" => LogEventLevel.Information,
                "WARN" => LogEventLevel.Warning,
                "WARNING" => LogEventLevel.Warning,
                "FAIL" => LogEventLevel.Error,
                "ERROR" => LogEventLevel.Error,
                "FATAL" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        private static bool IsRecognizedLogLevel(string? level)
        {
            return level is "INFO" or "NOTICE" or "HINT" or "WARN" or "WARNING" or "FAIL" or "ERROR" or "FATAL";
        }

        private static void LogWithTag(string tag, string message, LogEventLevel level, ConsoleColor color)
        {
            if (!Log.IsEnabled(level)) return;

            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.Write($"{tag} ");
                Console.ResetColor();
                Console.WriteLine(message);

                Log.ForContext("Tag", tag).Write(level, "{Message}", message);
            }
        }

        public static void Info(string message) => LogWithTag("INFO", message, LogEventLevel.Information, ConsoleColor.Green);
        public static void Hint(string message) => LogWithTag("HINT", message, LogEventLevel.Information, ConsoleColor.Yellow);
        public static void Redirecting(string message) => LogWithTag("REDIRECTING", message, LogEventLevel.Information, ConsoleColor.Blue);
        public static void Blocked(string message) => LogWithTag("BLOCKED", message, LogEventLevel.Warning, ConsoleColor.Magenta);
        public static void Warning(string message) => LogWithTag("WARN", message, LogEventLevel.Warning, ConsoleColor.Yellow);
        public static void Fail(string message) => LogWithTag("FAIL", message, LogEventLevel.Error, ConsoleColor.Red);
        public static void Error(string message) => LogWithTag("ERROR", message, LogEventLevel.Error, ConsoleColor.Red);
        public static void Fatal(string message) => LogWithTag("FATAL", message, LogEventLevel.Fatal, ConsoleColor.Red);
    }
}
