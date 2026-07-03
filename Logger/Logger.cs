using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Boxroom_Studio
{
    public static class Logger
    {
        private static readonly object _lock = new();

        private static readonly string LogDirectory =
            Path.Combine(AppContext.BaseDirectory, "logs");

        public static void Info(string message)
            => Write("INFO", message);

        public static void Warning(string message)
            => Write("WARN", message);

        public static void Error(string message)
            => Write("ERROR", message);

        public static void Error(Exception ex)
            => Write("ERROR", ex.ToString());

        private static void Write(string level, string message)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(LogDirectory);

                string file = Path.Combine(
                    LogDirectory,
                    $"{DateTime.Now:yyyy-MM-dd}.log");

                File.AppendAllText(
                    file,
                    $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
            }

            Debug.WriteLine($"[{level}] {message}");
        }
    }
}
