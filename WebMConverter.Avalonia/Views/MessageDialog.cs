using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace WebMConverter.Avalonia.Views;

/// <summary>Minimal OK dialog (Avalonia has no built-in MessageBox).</summary>
public static class MessageDialog
{
    public static async Task ShowAsync(Window owner, string title, string message)
    {
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 440,
            Margin = new global::Avalonia.Thickness(0, 0, 0, 12),
        };
        var ok = new Button
        {
            Content = "OK",
            MinWidth = 90,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var panel = new StackPanel
        {
            Margin = new global::Avalonia.Thickness(16),
        };
        panel.Children.Add(text);
        panel.Children.Add(ok);

        var dialog = new Window
        {
            Title = title,
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        ok.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner);
    }
}
