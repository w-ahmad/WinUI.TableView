using Microsoft.UI.Xaml.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

partial class CollectionView
{
    /// <summary>
    /// Gets or sets the source collection.
    /// </summary>
    public IEnumerable Source
    {
        get => _source;
        set
        {
            if (_source == value) return;

            if (_source is not null) DetachPropertyChangedHandlers(_source);

            _source = value;
            AttachPropertyChangedHandlers(_source);

            _collectionChangedListener?.Detach();

            if (_source is INotifyCollectionChanged sourceNcc)
            {
                _collectionChangedListener = new(this, sourceNcc, OnSourceCollectionChanged);
            }

            HandleSourceChanged();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether this CollectionView can filter its items.
    /// </summary>
    public bool CanFilter => FilterDescriptions.Count > 0;

    /// <summary>
    /// Gets the collection of filter descriptions.
    /// </summary>
    public IList<FilterDescription> FilterDescriptions => _filterDescriptions;

    /// <summary>
    /// Gets the collection of sort descriptions.
    /// </summary>
    public IList<SortDescription> SortDescriptions => _sortDescriptions;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to get or set.</param>
    /// <returns>The item at the specified index.</returns>
    public object? this[int index]
    {
        get => _view[index];
        set => _view[index] = value;
    }

    /// <summary>
    /// Gets the collection groups.
    /// </summary>
    public IObservableVector<object?>? CollectionGroups { get; } = null;

    /// <summary>
    /// Gets or sets the current item in the view.
    /// </summary>
    public object? CurrentItem
    {
        get => CurrentPosition > -1 && CurrentPosition < _view.Count ? _view[CurrentPosition] : null!;
        set => MoveCurrentTo(value);
    }

    /// <summary>
    /// Gets the current position of the item in the view.
    /// </summary>
    public int CurrentPosition { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are more items to load.
    /// </summary>
    public bool HasMoreItems => (_source as ISupportIncrementalLoading)?.HasMoreItems ?? false;

    /// <summary>
    /// Gets a value indicating whether the current item is after the last item in the view.
    /// </summary>
    public bool IsCurrentAfterLast => CurrentPosition >= _view.Count;

    /// <summary>
    /// Gets a value indicating whether the current item is before the first item in the view.
    /// </summary>
    public bool IsCurrentBeforeFirst => CurrentPosition < 0;

    /// <summary>
    /// Gets the number of items in the view.
    /// </summary>
    public int Count => _view.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => _source == null || _source.IsReadOnly();

    /// <summary>
    /// Gets or sets a value indicating whether live shaping is enabled.
    /// </summary>
    public bool AllowLiveShaping
    {
        get => _allowLiveShaping;
        set
        {
            if (_allowLiveShaping == value) return;

            _allowLiveShaping = value;
            if (_allowLiveShaping)
                AttachPropertyChangedHandlers(_source);
            else
                DetachPropertyChangedHandlers(_source);
        }
    }
}
