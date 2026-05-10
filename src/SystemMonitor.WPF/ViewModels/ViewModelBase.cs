using CommunityToolkit.Mvvm.ComponentModel;

namespace SystemMonitor.WPF.ViewModels;

/// <summary>
/// Base class for all ViewModels.
/// Inherits <see cref="ObservableObject"/> from CommunityToolkit.Mvvm which provides
/// INotifyPropertyChanged via source generation — no boilerplate required.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
