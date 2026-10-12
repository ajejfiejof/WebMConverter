using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace WebMConverter.Avalonia.Views;

internal static class FilePickerHelper
{
    public static async Task<string?> PickOpenAsync(Window? owner, string title, string? initialDir = null)
    {
        if (owner is null)
            return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        };
        var start = await TryGetFolderAsync(owner, initialDir).ConfigureAwait(true);
        if (start is not null)
            options.SuggestedStartLocation = start;

        var files = await owner.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(true);
        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    public static async Task<string?> PickSaveAsync(Window? owner, string title, string suggestedName, string? initialDir = null)
    {
        if (owner is null)
            return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = [new FilePickerFileType("WebM video") { Patterns = ["*.webm"] }],
        };
        var start = await TryGetFolderAsync(owner, initialDir).ConfigureAwait(true);
        if (start is not null)
            options.SuggestedStartLocation = start;

        var file = await owner.StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(true);
        return file?.Path.LocalPath;
    }

    private static async Task<IStorageFolder?> TryGetFolderAsync(Window owner, string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return null;
        try
        {
            return await owner.StorageProvider.TryGetFolderFromPathAsync(new Uri(dir)).ConfigureAwait(true);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
