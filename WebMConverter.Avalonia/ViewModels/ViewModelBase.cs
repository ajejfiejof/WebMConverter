using CommunityToolkit.Mvvm.ComponentModel;
using WebMConverter.Avalonia.Services;

namespace WebMConverter.Avalonia.ViewModels;

/// <summary>Base class for all view models. Keeps the MVVM plumbing in one place.</summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Localized string lookup for XAML: <c>{Binding L[SomeKey]}</c>.
    /// Subscribing to PropertyChanged with an empty name lets every binding
    /// refresh when the user switches language.
    /// </summary>
    public LocalizationService L => LocalizationService.Instance;
}