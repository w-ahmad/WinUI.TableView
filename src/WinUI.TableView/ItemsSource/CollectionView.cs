using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// A collection view implementation that supports filtering, sorting, and incremental loading.
/// </summary>
internal partial class CollectionView : ICollectionView, ISupportIncrementalLoading, INotifyPropertyChanged, IComparer<object>
{
    private IList _source = default!;
    private bool _allowLiveShaping;
    private readonly List<object> _view = [];
    private readonly ObservableCollection<FilterDescription> _filterDescriptions = [];
    private readonly ObservableCollection<SortDescription> _sortDescriptions = [];
    private CollectionChangedListener<CollectionView>? _collectionChangedListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionView"/> class.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <param name="liveShapingEnabled">Indicates whether live shaping is enabled.</param>
    public CollectionView(IList? source = null, bool liveShapingEnabled = true)
    {
        _filterDescriptions.CollectionChanged += OnFilterDescriptionsCollectionChanged;
        _sortDescriptions.CollectionChanged += OnSortDescriptionsCollectionChanged;

        AllowLiveShaping = liveShapingEnabled;
        Source = source ?? new List<object>();
    }

    /// <summary>
    /// Handles changes to the filter descriptions collection.
    /// </summary>
    private void OnFilterDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HandleFilterChanged();
    }

    /// <summary>
    /// Handles changes to the sort descriptions collection.
    /// </summary>
    private void OnSortDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_deferCounter > 0)
        {
            return;
        }

        HandleSortChanged();
    }

    /// <summary>
    /// Attaches property changed handlers to the items in the collection.
    /// </summary>
    /// <param name="items">The items to attach handlers to.</param>
    private void AttachPropertyChangedHandlers(IEnumerable? items)
    {
        if (!AllowLiveShaping || items is null) return;

        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// Detaches property changed handlers from the items in the collection.
    /// </summary>
    /// <param name="items">The items to detach handlers from.</param>
    private void DetachPropertyChangedHandlers(IEnumerable? items)
    {
        if (items is null) return;

        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// Handles changes to the source collection.
    /// </summary>
    private void OnSourceCollectionChanged(object? arg1, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AttachPropertyChangedHandlers(e.NewItems);
                if (_deferCounter <= 0)
                {
                    if (e.NewItems?.Count == 1)
                    {
                        HandleItemAdded(e.NewStartingIndex, e.NewItems[0]);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                DetachPropertyChangedHandlers(e.OldItems);
                if (_deferCounter <= 0)
                {
                    if (e.OldItems?.Count == 1)
                    {
                        HandleItemRemoved(e.OldStartingIndex, e.OldItems[0]);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                if (_deferCounter <= 0)
                {
                    HandleSourceChanged();
                }

                break;
        }
    }

    /// <summary>
    /// Handles property changed events for items in the collection.
    /// </summary>
    private void OnItemPropertyChanged(object? item, PropertyChangedEventArgs e)
    {
        if (!AllowLiveShaping || item is null || string.IsNullOrEmpty(e.PropertyName))
        {
            return;
        }

        if (FilterDescriptions.Any(fd => string.IsNullOrEmpty(fd.PropertyName) || fd.PropertyName == e.PropertyName))
        {
            var filterResult = FilterDescriptions.All(x => x.Predicate(item));
            var viewIndex = _view.IndexOf(item);

            if (viewIndex != -1 && !filterResult)
            {
                RemoveFromView(viewIndex, item);
            }
            else if (viewIndex == -1 && filterResult)
            {
                var index = _source.IndexOf(item);
                HandleItemAdded(index, item);
            }
        }

        if (SortDescriptions.Any(sd => string.IsNullOrEmpty(sd.PropertyName) || sd.PropertyName == e.PropertyName))
        {
            var oldIndex = _view.IndexOf(item);

            // Check if item is in view:
            if (oldIndex < 0)
            {
                return;
            }

            _view.RemoveAt(oldIndex);
            var targetIndex = _view.BinarySearch(item, this);
            if (targetIndex < 0)
            {
                targetIndex = ~targetIndex;
            }

            // Only trigger expensive UI updates if the index really changed:
            if (targetIndex != oldIndex)
            {
                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemRemoved, oldIndex, item));

                _view.Insert(targetIndex, item);

                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted, targetIndex, item));
            }
            else
            {
                _view.Insert(targetIndex, item);
            }
        }
        else if (string.IsNullOrEmpty(e.PropertyName))
        {
            HandleSourceChanged();
        }
    }

    /// <summary>
    /// Handles changes to the source collection.
    /// </summary>
    private void HandleSourceChanged()
    {
        var currentItem = CurrentItem;
        _view.Clear();

        if (Source is not null)
        {
            if (FilterDescriptions.Any() || SortDescriptions.Any())
            {
                foreach (var item in Source)
                {
                    if (FilterDescriptions is not null && !FilterDescriptions.All(x => x.Predicate(item)))
                    {
                        continue;
                    }

                    var targetIndex = _view.BinarySearch(item, this);
                    if (targetIndex < 0)
                    {
                        targetIndex = ~targetIndex;
                    }

                    _view.Insert(targetIndex, item);
                }
            }
            else
            {
                _view.AddRange(_source.OfType<object>());
            }
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
        MoveCurrentTo(currentItem);
    }

    /// <summary>
    /// Handles changes to the filter descriptions.
    /// </summary>
    private void HandleFilterChanged()
    {
        if (FilterDescriptions.Any())
        {
            for (var index = 0; index < _view.Count; index++)
            {
                var item = _view.ElementAt(index);
                if (FilterDescriptions.All(x => x.Predicate(item)))
                {
                    continue;
                }

                RemoveFromView(index, item);
                index--;
            }
        }

        var viewHash = new HashSet<object>(_view);
        var viewIndex = 0;
        for (var index = 0; index < _source.Count; index++)
        {
            var item = _source[index]!;
            if (viewHash.Contains(item))
            {
                viewIndex++;
                continue;
            }

            if (HandleItemAdded(index, item, viewIndex))
            {
                viewIndex++;
            }
        }
    }

    /// <summary>
    /// Handles changes to the sort descriptions.
    /// </summary>
    private void HandleSortChanged()
    {
        if (SortDescriptions.Count > 0)
        {
            _view.Sort(this);
        }
        else
        {
            HandleSourceChanged();
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
    }

    /// <summary>
    /// Handles the addition of an item to the collection.
    /// </summary>
    private bool HandleItemAdded(int newStartingIndex, object? newItem, int? viewIndex = null)
    {
        if (!FilterDescriptions.All(x => x.Predicate(newItem)))
        {
            return false;
        }

        var newViewIndex = newStartingIndex;

        if (_sortDescriptions.Any())
        {
            newViewIndex = _view.BinarySearch(newItem!, this);
            if (newViewIndex < 0)
            {
                newViewIndex = ~newViewIndex;
            }
        }
        else if (FilterDescriptions.Any())
        {
            if (_source == null)
            {
                HandleSourceChanged();
                return false;
            }
            newViewIndex = viewIndex ?? _view.Take(newStartingIndex).Count();
        }

        _view.Insert(newViewIndex, newItem!);
        if (newViewIndex <= CurrentPosition)
        {
            CurrentPosition++;
        }

        var e = new VectorChangedEventArgs(CollectionChange.ItemInserted, newViewIndex, newItem);
        OnVectorChanged(e);

        return true;
    }

    /// <summary>
    /// Handles the removal of an item from the collection.
    /// </summary>
    private void HandleItemRemoved(int oldStartingIndex, object? oldItem)
    {
        if (FilterDescriptions != null && !FilterDescriptions.All(x => x.Predicate(oldItem)))
        {
            return;
        }

        if (oldStartingIndex < 0 || oldStartingIndex >= _view.Count || !Equals(_view[oldStartingIndex], oldItem))
        {
            oldStartingIndex = _view.IndexOf(oldItem!);
        }

        if (oldStartingIndex < 0)
        {
            return;
        }

        RemoveFromView(oldStartingIndex, oldItem);
    }

    /// <summary>
    /// Removes an item from the view.
    /// </summary>
    private void RemoveFromView(int itemIndex, object? item)
    {
        _view.RemoveAt(itemIndex);

        if (itemIndex <= CurrentPosition)
        {
            CurrentPosition--;
        }

        var e = new VectorChangedEventArgs(CollectionChange.ItemRemoved, itemIndex, item);
        OnVectorChanged(e);
    }

    /// <summary>
    /// Moves the current item to the specified index.
    /// </summary>
    private bool MoveCurrentToIndex(int i)
    {
        if (i < -1 || i >= _view.Count || i == CurrentPosition) return false;

        var e = new CurrentChangingEventArgs();
        OnCurrentChanging(e);

        if (e.Cancel)
        {
            return false;
        }

        CurrentPosition = i;
        OnCurrentChanged();

        return true;
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(object item)
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        _source.Add(item);
    }

    /// <summary>
    /// Clears the collection.
    /// </summary>
    public void Clear()
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        _source.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>true if the item is found in the collection; otherwise, false.</returns>
    public bool Contains(object item)
    {
        return _view.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(object[] array, int arrayIndex)
    {
        _view.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>The index of the item if found in the collection; otherwise, -1.</returns>
    public int IndexOf(object item)
    {
        return _view.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item to the collection at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, object item)
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        _source.Insert(index, item);
    }

    /// <summary>
    /// Moves the current item to the specified item.
    /// </summary>
    /// <param name="item">The item to move to.</param>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentTo(object item)
    {
        return item == CurrentItem || MoveCurrentToIndex(IndexOf(item));
    }

    /// <summary>
    /// Moves the current item to the first item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToFirst()
    {
        return MoveCurrentToIndex(0);
    }

    /// <summary>
    /// Moves the current item to the last item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToLast()
    {
        return MoveCurrentToIndex(_view.Count - 1);
    }

    /// <summary>
    /// Moves the current item to the next item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToNext()
    {
        return MoveCurrentToIndex(CurrentPosition + 1);
    }

    /// <summary>
    /// Moves the current item to the specified position.
    /// </summary>
    /// <param name="index">The zero-based index to move to.</param>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToPosition(int index)
    {
        return MoveCurrentToIndex(index);
    }

    /// <summary>
    /// Moves the current item to the previous item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToPrevious()
    {
        return MoveCurrentToIndex(CurrentPosition - 1);
    }

    /// <summary>
    /// Removes the first occurrence of a specific item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    public bool Remove(object item)
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        _source.Remove(item);

        return true;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        Remove(_view[index]);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<object> GetEnumerator()
    {
        return _view.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _view.GetEnumerator();
    }

    /// <summary>
    /// Loads more items asynchronously.
    /// </summary>
    /// <param name="count">The number of items to load.</param>
    /// <returns>An asynchronous operation that returns the result of the load operation.</returns>
    public IAsyncOperation<LoadMoreItemsResult>? LoadMoreItemsAsync(uint count)
    {
        return (_source as ISupportIncrementalLoading)?.LoadMoreItemsAsync(count);
    }

    /// <summary>
    /// Compares two objects based on the sort descriptions.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>An integer that indicates the relative order of the objects being compared.</returns>
    public int Compare(object? x, object? y)
    {
        foreach (var sortDescription in SortDescriptions)
        {
            var xValue = sortDescription.GetPropertyValue(x);
            var yValue = sortDescription.GetPropertyValue(y);
            var cmp = sortDescription.Compare(xValue, yValue);

            if (cmp != 0)
            {
                return sortDescription.Direction is SortDirection.Ascending ? +cmp : -cmp;
            }
        }

        return 0;
    }

    /// <summary>
    /// Manually refreshes the view.
    /// </summary>
    public void Refresh()
    {
        HandleSourceChanged();
    }

    /// <summary>
    /// Refreshes the filter applied to the view.
    /// </summary>
    public void RefreshFilter()
    {
        HandleFilterChanged();
    }

    /// <summary>
    /// Refreshes the sorting applied to the view.
    /// </summary>
    public void RefreshSorting()
    {
        HandleSortChanged();
    }
}
