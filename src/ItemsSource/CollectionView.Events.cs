using Microsoft.UI.Xaml.Data;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;

namespace WinUI.TableView;

partial class CollectionView
{
    /// <summary>
    /// Currently selected item changing event
    /// </summary>
    /// <param name="e">event args</param>
    private void OnCurrentChanging(CurrentChangingEventArgs e)
    {
        if (_deferCounter > 0)
        {
            return;
        }

        CurrentChanging?.Invoke(this, e);
        OnPropertyChanging(nameof(CurrentItem));
    }

    /// <summary>
    /// Currently selected item changed event
    /// </summary>
    private void OnCurrentChanged()
    {
        if (_deferCounter > 0)
        {
            return;
        }

        CurrentChanged?.Invoke(this, null!);

        // ReSharper disable once ExplicitCallerInfoArgument
        OnPropertyChanged(nameof(CurrentItem));
    }

    /// <summary>
    /// Vector changed event
    /// </summary>
    /// <param name="e">event args</param>
    private void OnVectorChanged(IVectorChangedEventArgs e)
    {
        if (_deferCounter > 0)
        {
            return;
        }

        VectorChanged?.Invoke(this, e);

        // ReSharper disable once ExplicitCallerInfoArgument
        OnPropertyChanged(nameof(Count));
    }

    /// <summary>
    /// Raises <see cref="ViewResetting"/>. Not suppressed by a pending defer: the view is rebuilt either way, and a
    /// listener holding indexes into it needs to know before they stop meaning anything.
    /// </summary>
    private void OnViewResetting()
    {
        ViewResetting?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Property changed event invoker
    /// </summary>
    /// <param name="propertyName">name of the property that changed</param>
    private void OnPropertyChanging([CallerMemberName] string propertyName = null!)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    /// <summary>
    /// Property changed event invoker
    /// </summary>
    /// <param name="propertyName">name of the property that changed</param>
    private void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Occurs when the current item has changed.
    /// </summary>
    public event EventHandler<object>? CurrentChanged;

    /// <summary>
    /// Occurs when the current item is changing.
    /// </summary>
    public event CurrentChangingEventHandler? CurrentChanging;

    /// <summary>
    /// Occurs when the vector has changed.
    /// </summary>
    public event VectorChangedEventHandler<object>? VectorChanged;

    /// <summary>
    /// Occurs when a property value is changing.
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when an item's property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? ItemPropertyChanged;

    /// <summary>
    /// Occurs just before the view is rebuilt from scratch (the change that is then reported as a reset), while
    /// every index into it still refers to the item it did before.
    /// </summary>
    internal event EventHandler? ViewResetting;
}
