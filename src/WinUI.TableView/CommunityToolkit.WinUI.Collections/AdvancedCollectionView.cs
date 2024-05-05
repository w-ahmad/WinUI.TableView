// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI.TableView.Extensions;

namespace CommunityToolkit.WinUI.Collections;

/// <summary>
/// A collection view implementation that supports filtering, sorting and incremental loading
/// </summary>
public partial class AdvancedCollectionView : IAdvancedCollectionView, INotifyPropertyChanged, ISupportIncrementalLoading, IComparer<object>
{
    private readonly List<object> _view = new();
    private readonly ObservableCollection<SortDescription> _sortDescriptions = new();
    private readonly Dictionary<string, (PropertyInfo, object?)[]> _sortProperties = new();
    private readonly bool _liveShapingEnabled;
    private readonly HashSet<string> _observedFilterProperties = new();
    private IList _source = new List<object>(0);
    private Predicate<object> _filter = default!;
    private int _deferCounter;
    private CollectionChangedListener? _collectionChangedListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedCollectionView"/> class.
    /// </summary>
    public AdvancedCollectionView() : this(new List<object>(0))
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedCollectionView"/> class.
    /// </summary>
    /// <param name="source">source IEnumerable</param>
    /// <param name="isLiveShaping">Denotes whether or not this ACV should re-filter/re-sort if a PropertyChanged is raised for an observed property.</param>
    public AdvancedCollectionView(IList source, bool isLiveShaping = false)
    {
        _liveShapingEnabled = isLiveShaping;
        _sortDescriptions.CollectionChanged += SortDescriptions_CollectionChanged;
        Source = source;
    }

    /// <summary>
    /// Gets or sets the source
    /// </summary>
    public IList Source
    {
        get => _source;

        set
        {
            if (_source == value)
            {
                return;
            }

            if (_source != null)
            {
                DetachPropertyChangedHandler(_source);
            }

            _source = value;
            AttachPropertyChangedHandler(_source);

            _collectionChangedListener?.Detach();

            if (_source is INotifyCollectionChanged sourceNcc)
            {
                _collectionChangedListener = new(this, sourceNcc, SourceNcc_CollectionChanged);
            }

            HandleSourceChanged();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Manually refresh the view
    /// </summary>
    public void Refresh()
    {
        HandleSourceChanged();
    }

    /// <inheritdoc/>
    public void RefreshFilter()
    {
        HandleFilterChanged();
    }

    /// <inheritdoc/>
    public void RefreshSorting()
    {
        HandleSortChanged();
    }

    /// <inheritdoc />
    public IEnumerator<object> GetEnumerator()
    {
        return _view.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _view.GetEnumerator();
    }

    /// <inheritdoc />
    public void Add(object item)
    {
        if (IsReadOnly)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        _source.Add(item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (IsReadOnly)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        _source.Clear();
    }

    /// <inheritdoc />
    public bool Contains(object item)
    {
        return _view.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(object[] array, int arrayIndex)
    {
        _view.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(object item)
    {
        if (IsReadOnly)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        _source.Remove(item);
        return true;
    }

    /// <inheritdoc />
    public int Count => _view.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _source == null || _source.IsReadOnly;

    /// <inheritdoc />
    public int IndexOf(object item)
    {
        return _view.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, object item)
    {
        if (IsReadOnly)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        _source.Insert(index, item);
    }

    /// <summary>
    /// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
    public void RemoveAt(int index)
    {
        Remove(_view[index]);
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <returns>
    /// The element at the specified index.
    /// </returns>
    /// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
    public object this[int index]
    {
        get => _view[index];
        set => _view[index] = value;
    }

    /// <summary>
    /// Occurs when the vector changes.
    /// </summary>
    public event VectorChangedEventHandler<object>? VectorChanged;

    /// <summary>
    /// Move current index to item
    /// </summary>
    /// <param name="item">item</param>
    /// <returns>success of operation</returns>
    public bool MoveCurrentTo(object item)
    {
        return item == CurrentItem || MoveCurrentToIndex(IndexOf(item));
    }

    /// <summary>
    /// Moves selected item to position
    /// </summary>
    /// <param name="index">index</param>
    /// <returns>success of operation</returns>
    public bool MoveCurrentToPosition(int index)
    {
        return MoveCurrentToIndex(index);
    }

    /// <summary>
    /// Move current item to first item
    /// </summary>
    /// <returns>success of operation</returns>
    public bool MoveCurrentToFirst()
    {
        return MoveCurrentToIndex(0);
    }

    /// <summary>
    /// Move current item to last item
    /// </summary>
    /// <returns>success of operation</returns>
    public bool MoveCurrentToLast()
    {
        return MoveCurrentToIndex(_view.Count - 1);
    }

    /// <summary>
    /// Move current item to next item
    /// </summary>
    /// <returns>success of operation</returns>
    public bool MoveCurrentToNext()
    {
        return MoveCurrentToIndex(CurrentPosition + 1);
    }

    /// <summary>
    /// Move current item to previous item
    /// </summary>
    /// <returns>success of operation</returns>
    public bool MoveCurrentToPrevious()
    {
        return MoveCurrentToIndex(CurrentPosition - 1);
    }

    /// <summary>
    /// Load more items from the source
    /// </summary>
    /// <param name="count">number of items to load</param>
    /// <returns>Async operation of LoadMoreItemsResult</returns>
    /// <exception cref="NotImplementedException">Not implemented yet...</exception>
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        var sil = _source as ISupportIncrementalLoading;
        return sil?.LoadMoreItemsAsync(count)!;
    }

    /// <summary>
    /// Gets the groups in collection
    /// </summary>
    public IObservableVector<object>? CollectionGroups => null;

    /// <summary>
    /// Gets or sets the current item
    /// </summary>
    public object CurrentItem
    {
        get => CurrentPosition > -1 && CurrentPosition < _view.Count ? _view[CurrentPosition] : null!;
        set => MoveCurrentTo(value);
    }

    /// <summary>
    /// Gets the position of current item
    /// </summary>
    public int CurrentPosition { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the source has more items
    /// </summary>
    public bool HasMoreItems => (_source as ISupportIncrementalLoading)?.HasMoreItems ?? false;

    /// <summary>
    /// Gets a value indicating whether the current item is after the last visible item
    /// </summary>
    public bool IsCurrentAfterLast => CurrentPosition >= _view.Count;

    /// <summary>
    /// Gets a value indicating whether the current item is before the first visible item
    /// </summary>
    public bool IsCurrentBeforeFirst => CurrentPosition < 0;

    /// <summary>
    /// Current item changed event handler
    /// </summary>
    public event EventHandler<object>? CurrentChanged;

    /// <summary>
    /// Current item changing event handler
    /// </summary>
    public event CurrentChangingEventHandler? CurrentChanging;

    /// <summary>
    /// Gets a value indicating whether this CollectionView can filter its items
    /// </summary>
    public bool CanFilter => true;

    /// <summary>
    /// Gets or sets the predicate used to filter the visible items
    /// </summary>
    public Predicate<object> Filter
    {
        get => _filter;
        set
        {
            if (_filter == value)
            {
                return;
            }

            _filter = value;
            HandleFilterChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether this CollectionView can sort its items
    /// </summary>
    public bool CanSort => true;

    /// <summary>
    /// Gets SortDescriptions to sort the visible items
    /// </summary>
    public IList<SortDescription> SortDescriptions => _sortDescriptions;

    /*
    /// <summary>
    /// Gets a value indicating whether this CollectionView can group its items
    /// </summary>
    public bool CanGroup => false;

    /// <summary>
    /// Gets GroupDescriptions to group the visible items
    /// </summary>
    public IList<object> GroupDescriptions => null;
    */

    /// <summary>
    /// Gets the source collection
    /// </summary>
    public IEnumerable SourceCollection => _source;

    /// <summary>
    /// IComparer implementation
    /// </summary>
    /// <param name="x">Object A</param>
    /// <param name="y">Object B</param>
    /// <returns>Comparison value</returns>
    int IComparer<object>.Compare(object? x, object? y)
    {
        foreach (var sd in _sortDescriptions)
        {
            object? cx;
            object? cy;

            if (!string.IsNullOrEmpty(sd.PropertyName) &&
                _sortProperties.TryGetValue(sd.PropertyName, out var pis))
            {
                cx = x.GetValue(pis);
                cy = y.GetValue(pis);
            }
            else
            {
                var type = _source?.GetType() is { } listType && listType.IsGenericType ? listType.GetGenericArguments()[0] : x?.GetType();
                cx = x.GetValue(type, sd.PropertyName, out pis);
                cy = y.GetValue(pis);

                _sortProperties.Add(sd.PropertyName, pis);
            }

            var cmp = sd.Comparer.Compare(cx, cy);

            if (cmp != 0)
            {
                return sd.Direction == SortDirection.Ascending ? +cmp : -cmp;
            }
        }

        return 0;
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Property changed event invoker
    /// </summary>
    /// <param name="propertyName">name of the property that changed</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <inheritdoc/>
    public void ObserveFilterProperty(string propertyName)
    {
        _observedFilterProperties.Add(propertyName);
    }

    /// <inheritdoc/>
    public void ClearObservedFilterProperties()
    {
        _observedFilterProperties.Clear();
    }

    private void ItemOnPropertyChanged(object? item, PropertyChangedEventArgs e)
    {
        if (!_liveShapingEnabled || item is null || string.IsNullOrEmpty(e.PropertyName))
        {
            return;
        }

        var filterResult = _filter?.Invoke(item);

        if (filterResult.HasValue && _observedFilterProperties.Contains(e.PropertyName))
        {
            var viewIndex = _view.IndexOf(item);
            if (viewIndex != -1 && !filterResult.Value)
            {
                RemoveFromView(viewIndex, item);
            }
            else if (viewIndex == -1 && filterResult.Value)
            {
                var index = _source.IndexOf(item);
                HandleItemAdded(index, item);
            }
        }

        if ((filterResult ?? true) && SortDescriptions.Any(sd => sd.PropertyName == e.PropertyName))
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

    private void AttachPropertyChangedHandler(IEnumerable? items)
    {
        if (!_liveShapingEnabled || items == null)
        {
            return;
        }

        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged += ItemOnPropertyChanged;
        }
    }

    private void DetachPropertyChangedHandler(IEnumerable? items)
    {
        if (!_liveShapingEnabled || items == null)
        {
            return;
        }

        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
        }
    }

    private void HandleSortChanged()
    {
        _sortProperties.Clear();

        if (_sortDescriptions.Count > 0)
        {
            _view.Sort(this);
        }
        else
        {
            HandleSourceChanged();
        }

        _sortProperties.Clear();

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
    }

    private void HandleFilterChanged()
    {
        if (_filter != null)
        {
            for (var index = 0; index < _view.Count; index++)
            {
                var item = _view.ElementAt(index);
                if (_filter(item))
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

    private void HandleSourceChanged()
    {
        _sortProperties.Clear();
        var currentItem = CurrentItem;
        _view.Clear();

        if (Source is not null)
        {
            foreach (var item in Source)
            {
                if (_filter != null && !_filter(item))
                {
                    continue;
                }

                if (_sortDescriptions.Any())
                {
                    var targetIndex = _view.BinarySearch(item, this);
                    if (targetIndex < 0)
                    {
                        targetIndex = ~targetIndex;
                    }

                    _view.Insert(targetIndex, item);
                }
                else
                {
                    _view.Add(item);
                }
            }
        }

        _sortProperties.Clear();
        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
        MoveCurrentTo(currentItem);
    }

    private void SourceNcc_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AttachPropertyChangedHandler(e.NewItems);
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
                DetachPropertyChangedHandler(e.OldItems);
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

    private bool HandleItemAdded(int newStartingIndex, object? newItem, int? viewIndex = null)
    {
        if (_filter != null && !_filter(newItem!))
        {
            return false;
        }

        var newViewIndex = newStartingIndex;

        if (_sortDescriptions.Any())
        {
            _sortProperties.Clear();
            newViewIndex = _view.BinarySearch(newItem!, this);
            if (newViewIndex < 0)
            {
                newViewIndex = ~newViewIndex;
            }
        }
        else if (_filter is not null)
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

    private void HandleItemRemoved(int oldStartingIndex, object? oldItem)
    {
        if (_filter != null && !_filter(oldItem!))
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

    private void SortDescriptions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_deferCounter > 0)
        {
            return;
        }

        HandleSortChanged();
    }

    private bool MoveCurrentToIndex(int i)
    {
        if (i < -1 || i >= _view.Count)
        {
            return false;
        }

        if (i == CurrentPosition)
        {
            return false;
        }

        var e = new CurrentChangingEventArgs();
        OnCurrentChanging(e);
        if (e.Cancel)
        {
            return false;
        }

        CurrentPosition = i;
        OnCurrentChanged(null!);
        return true;
    }
}