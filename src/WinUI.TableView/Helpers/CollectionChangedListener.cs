using System;
using System.Collections.Specialized;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Listens for collection changed events using a weak reference to the source.
/// </summary>
/// <typeparam name="TSource">The type of the source object.</typeparam>
internal class CollectionChangedListener<TSource> where TSource : class
{
    private readonly WeakReference<TSource> _source;
    private readonly INotifyCollectionChanged _notifyCollection;
    private readonly Action<object?, NotifyCollectionChangedEventArgs>? _onEventAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionChangedListener{TSource}"/> class.
    /// </summary>
    /// <param name="source">The source object to listen for changes.</param>
    /// <param name="notifyCollection">The collection that notifies about changes.</param>
    /// <param name="onEventAction">The action to perform when the collection changes.</param>
    public CollectionChangedListener(TSource source,
                                     INotifyCollectionChanged notifyCollection,
                                     Action<object?, NotifyCollectionChangedEventArgs> onEventAction)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(notifyCollection);
        ArgumentNullException.ThrowIfNull(onEventAction);

        _source = new WeakReference<TSource>(source);
        _notifyCollection = notifyCollection;
        _onEventAction = onEventAction;
        _notifyCollection.CollectionChanged += OnCollectionChanged;
    }

    /// <summary>
    /// Handles the collection changed event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_source.TryGetTarget(out var target))
        {
            _onEventAction?.Invoke(sender, e); // Call registered action
        }
        else
        {
            Detach(); // Detach from event
        }
    }

    /// <summary>
    /// Detaches the listener from the collection changed event.
    /// </summary>
    public void Detach()
    {
        _notifyCollection.CollectionChanged -= OnCollectionChanged;
    }
}

