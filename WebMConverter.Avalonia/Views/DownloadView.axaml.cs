using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WebMConverter.Avalonia.ViewModels;

namespace WebMConverter.Avalonia.Views;

public partial class DownloadView : Window
{
    public DownloadView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is DownloadViewModel vm && vm.IsRunning)
            vm.Cancel();
        base.OnClosing(e);
    }
}
