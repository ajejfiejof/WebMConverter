using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using WebMConverter.Avalonia.ViewModels;

namespace WebMConverter.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private async void OnBrowseInputClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select input video",
            AllowMultiple = false,
        }).ConfigureAwait(true);

        if (files.Count == 0)
            return;

        var file = files[0];

        ViewModel.InputFile = file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(ViewModel.OutputFile))
            ViewModel.OutputFile = Path.ChangeExtension(file.Path.LocalPath, ".webm");
    }

    private async void OnBrowseOutputClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select output file",
            SuggestedFileName = string.IsNullOrWhiteSpace(ViewModel.InputFile)
                ? "output.webm"
                : Path.GetFileName(Path.ChangeExtension(ViewModel.InputFile, ".webm")),
            FileTypeChoices = [new FilePickerFileType("WebM video") { Patterns = ["*.webm"] }],
        }).ConfigureAwait(true);

        if (file is not null)
            ViewModel.OutputFile = file.Path.LocalPath;
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e) => OnBrowseInputClick(sender, e);

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private void OnAboutClick(object? sender, RoutedEventArgs e) =>
        ViewModel.StatusMessage = "WebMConverter (Avalonia) - cross-platform port in progress.";

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.Items.OfType<IStorageFile>().FirstOrDefault();
        if (file is null)
            return;

        ViewModel.InputFile = file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(ViewModel.OutputFile))
            ViewModel.OutputFile = Path.ChangeExtension(file.Path.LocalPath, ".webm");

        e.Handled = true;
    }
}
