using System;
using Avalonia;

namespace WebMConverter.Avalonia;

internal static class Program
{
    // Cross-platform entry point. Keep WinForms and WPF APIs out of this
    // head so the app starts on Linux, macOS, and Windows alike.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
