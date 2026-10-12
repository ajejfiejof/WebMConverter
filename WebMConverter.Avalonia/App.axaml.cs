using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WebMConverter.Avalonia.ViewModels;
using WebMConverter.Avalonia.Views;
using WebMConverter.Core;

namespace WebMConverter.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        // Same as the legacy startup: detect helpers without blocking the UI.
        System.Threading.Tasks.Task.Run(VideoDownload.CheckEnabled);
        System.Threading.Tasks.Task.Run(ShareXClient.CheckEnabled);

        base.OnFrameworkInitializationCompleted();
    }
}
