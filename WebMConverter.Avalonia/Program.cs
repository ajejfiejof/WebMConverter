using System;
using Avalonia;

namespace WebMConverter.Avalonia;

internal sealed class Program
{
    // Avalonia entry point. Do not call any WinForms/WPF APIs from here;
    // this head must stay runnable on Linux (net8.0, no Windows targeting).
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
