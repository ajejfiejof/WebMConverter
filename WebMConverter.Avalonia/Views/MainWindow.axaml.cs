using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WebMConverter.Avalonia.ViewModels;

namespace WebMConverter.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);

        // The DataContext is assigned after construction, so build the
        // language menu once it arrives.
        DataContextChanged += (_, _) => PopulateLanguageMenu();

        var tabs = this.FindControl<TabControl>("MainTabs");
        if (tabs is not null)
            tabs.SelectionChanged += OnTabSelectionChanged;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.RefreshActiveFilters();
    }

    private async void OnBrowseInputClick(object? sender, RoutedEventArgs e)
    {
        // URL in the box turns Browse into Download, like the legacy app.
        if (ViewModel.InputFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (ViewModel.DownloadFromUrlCommand.CanExecute(ViewModel.InputFile))
                await ViewModel.DownloadFromUrlCommand.ExecuteAsync(ViewModel.InputFile);
            else if (DataContext is MainViewModel vm)
                vm.StatusMessage = "yt-dlp not found — install it or put it next to the app.";
            return;
        }

        string? file = await FilePickerHelper.PickOpenAsync(this, "Select input video", ViewModel.RememberedFolderIn);
        if (file is null)
            return;

        ViewModel.InputFile = file;
        ViewModel.RememberFolders(Path.GetDirectoryName(file), null);
        if (string.IsNullOrWhiteSpace(ViewModel.OutputFile))
            ViewModel.OutputFile = Path.ChangeExtension(file, ".webm");
    }

    private async void OnBrowseOutputClick(object? sender, RoutedEventArgs e)
    {
        string suggested = string.IsNullOrWhiteSpace(ViewModel.InputFile)
            ? "output.webm"
            : Path.GetFileName(Path.ChangeExtension(ViewModel.InputFile, ".webm"));

        string? file = await FilePickerHelper.PickSaveAsync(this, "Select output file", suggested, ViewModel.RememberedFolderOut);
        if (file is null)
            return;

        ViewModel.OutputFile = file;
        ViewModel.RememberFolders(null, Path.GetDirectoryName(file));
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e) => OnBrowseInputClick(sender, e);

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private void OnLanguageClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string code })
            ViewModel.SetLanguageCommand.Execute(code);
    }

    /// <summary>
    /// Populates the language menu with explicit MenuItems. Built in code
    /// rather than ItemsSource: Avalonia's MenuItem does not generate item
    /// containers for a templated ItemsControl, which renders an empty popup.
    /// </summary>
    private void PopulateLanguageMenu()
    {
        var menu = this.FindControl<MenuItem>("LanguageMenu");
        if (menu is null)
            return;

        menu.Items.Clear();
        foreach (Services.LanguageOption option in ViewModel.Languages)
        {
            var item = new MenuItem
            {
                Header = option.Name,
                Tag = option.Code,
            };
            AutomationProperties.SetName(item, option.Name);
            item.Click += OnLanguageClick;
            menu.Items.Add(item);
        }
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e) =>
        ViewModel.StatusMessage = Lang.Resources.Get("About");

    /// <summary>Enter in the input box loads the file, or starts a URL download.</summary>
    private async void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !string.IsNullOrWhiteSpace(ViewModel.InputFile))
            return;

        e.Handled = true;
        if (ViewModel.InputFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (ViewModel.DownloadFromUrlCommand.CanExecute(ViewModel.InputFile))
                await ViewModel.DownloadFromUrlCommand.ExecuteAsync(ViewModel.InputFile);
            else
                ViewModel.StatusMessage = "yt-dlp not found — install it or put it next to the app.";
        }
        else
        {
            ViewModel.LoadInputFile(ViewModel.InputFile);
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.Items.OfType<global::Avalonia.Platform.Storage.IStorageFile>().FirstOrDefault();
        if (file is null)
            return;

        ViewModel.InputFile = file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(ViewModel.OutputFile))
            ViewModel.OutputFile = Path.ChangeExtension(file.Path.LocalPath, ".webm");

        e.Handled = true;
    }
}
