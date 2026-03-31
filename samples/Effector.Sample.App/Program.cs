using Avalonia;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;

namespace Effector.Sample.App;

internal static class Program
{
    [System.STAThread]
    public static void Main(string[] args)
    {
        ConfigureExceptionDiagnostics();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void ConfigureExceptionDiagnostics()
    {
        var path = Environment.GetEnvironmentVariable("EFFECTOR_SAMPLE_EXCEPTION_LOG_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        AppDomain.CurrentDomain.FirstChanceException += (_, eventArgs) => AppendException(path!, "FirstChance", eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            AppendException(path!, "Unhandled", eventArgs.ExceptionObject as Exception ?? new Exception(eventArgs.ExceptionObject?.ToString()));
    }

    private static void AppendException(string path, string category, Exception exception)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line =
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) +
                " | " + category +
                " | " + exception.GetType().FullName +
                " | " + exception.Message +
                Environment.NewLine +
                exception +
                Environment.NewLine;
            File.AppendAllText(path, line);
        }
        catch
        {
        }
    }
}
