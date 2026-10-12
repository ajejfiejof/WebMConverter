using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using WebMConverter.Avalonia.Lang;
using WebMConverter.Avalonia.ViewModels;
using WebMConverter.Core;

namespace WebMConverter.Avalonia.Services;

/// <summary>
/// Resource lookup with runtime language switching. Bound as
/// <c>{Binding L[Tab_InputOutput]}</c> in XAML; setting
/// <see cref="CurrentCulture"/> refreshes every such binding live (empty
/// property name signals "all changed" to the binding engine).
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private string _currentCulture = "en";

    /// <summary>Culture code of the active UI language (not the data culture).</summary>
    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(value);
            }
            catch (CultureNotFoundException)
            {
                return;
            }

            _currentCulture = value;
            Resources.Culture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // The setter runs on the UI thread; pin it directly so the
            // ResourceManager resolves against this thread, not just the
            // default for threads that haven't captured a culture yet.
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            // Refresh indexer bindings ({Binding L[Key]}); Avalonia matches
            // this notification by the indexer name.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>Null indexer binds to anything; the key indexer is the XAML path.</summary>
    public string? this[string? key] =>
        key is null ? null : Resources.ResourceManager.GetString(key) ?? key;

    /// <summary>Languages offered in the menu. Names are self-describing per locale.</summary>
    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new();

    public LocalizationService()
    {
        AvailableLanguages.Add(new LanguageOption("en", "English"));
        AvailableLanguages.Add(new LanguageOption("zh-Hans", "简体中文"));
        AvailableLanguages.Add(new LanguageOption("zh-Hant", "繁體中文"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>One selectable UI language.</summary>
public sealed class LanguageOption(string code, string name) : ViewModelBase
{
    public string Code { get; } = code;

    /// <summary>Display name (English) — deliberately not localized so users can always find their language.</summary>
    public string Name { get; } = name;

    public override string ToString() => Name;
}