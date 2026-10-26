using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BinObjCleaner.ViewModels;

/// <summary>
/// Basisklasse für ViewModels mit Benachrichtigung bei Änderungen.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Benachrichtigt alle Bindings neu - wird nach einem Sprachwechsel verwendet.
    /// </summary>
    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
