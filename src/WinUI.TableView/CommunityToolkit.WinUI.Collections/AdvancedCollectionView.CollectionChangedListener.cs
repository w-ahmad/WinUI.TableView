using System;
using System.Collections.Specialized;

namespace CommunityToolkit.WinUI.Collections;

public partial class AdvancedCollectionView
{
    private class CollectionChangedListener
    {
        private readonly WeakReference<AdvancedCollectionView> _collectionView;
        private readonly INotifyCollectionChanged _notifyCollection;
        private readonly Action<object?, NotifyCollectionChangedEventArgs>? _onEventAction;

        public CollectionChangedListener(AdvancedCollectionView collectionView,
                                         INotifyCollectionChanged notifyCollection,
                                         Action<object?, NotifyCollectionChangedEventArgs> onEventAction)
        {
            ArgumentNullException.ThrowIfNull(collectionView);
            ArgumentNullException.ThrowIfNull(notifyCollection);
            ArgumentNullException.ThrowIfNull(onEventAction);

            _collectionView = new WeakReference<AdvancedCollectionView>(collectionView);
            _notifyCollection = notifyCollection;
            _onEventAction = onEventAction;
            _notifyCollection.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_collectionView.TryGetTarget(out var target))
            {
                _onEventAction?.Invoke(sender, e); // Call registered action
            }
            else
            {
                Detach();// Detach from event
            }
        }

        public void Detach()
        {
            _notifyCollection.CollectionChanged -= OnCollectionChanged;
        }
    }
}