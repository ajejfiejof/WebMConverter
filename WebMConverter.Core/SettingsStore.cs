using System;
using System.IO;
using System.Text.Json;

namespace WebMConverter.Core;

/// <summary>
/// Persistent user preferences (JSON in the user profile).
/// Replaces the WinForms Properties.Settings store.
/// </summary>
public sealed class AppSettings
{
    public string Language { get; set; } = "en";
    public string EncodingMode { get; set; } = nameof(Core.EncodingMode.Variable);
    public bool AudioEnabled { get; set; } = true;
    public string RememberedFolderIn { get; set; } = string.Empty;
    public string RememberedFolderOut { get; set; } = string.Empty;
}

public static class SettingsStore
{
    public static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WebMConverter");
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (settings is not null)
                    return settings;
            }
        }
        catch (Exception)
        {
            // Corrupt settings must never block startup.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (dir is not null)
                Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Settings are best effort.
        }
    }
}
