using System;
using System.Diagnostics;
using System.IO;

namespace DriverHealthChecker.App;

internal static class AppLogger
{
    private static readonly object Sync = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DriverHealthChecker",
        "logs");

    private static readonly string LogFile = Path.Combine(LogDirectory, "app.log");

    public static void Info(string message)
    {
        Write("INFO", message, null);
    }

    public static void Error(string message, Exception? ex = null)
    {
        Write("ERROR", message, ex);
    }

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);

                using var writer = new StreamWriter(LogFile, append: true);
                writer.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [{level}] {message}");

                if (ex != null)
                {
                    writer.WriteLine(ex.ToString());
                }
            }
        }
        catch
        {
            Trace.WriteLine($"[DriverHealthChecker][{level}] {message}");
            if (ex != null)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
    }
}
