using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WebMConverter.Avalonia.ViewModels;

namespace WebMConverter.Avalonia.Views;

public partial class ConvertProgressView : Window
{
    public ConvertProgressView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ConvertProgressViewModel vm && vm.IsRunning)
            vm.Cancel();
        base.OnClosing(e);
    }
}
