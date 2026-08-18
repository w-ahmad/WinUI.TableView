using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WinUI.TableView;

/// <summary>
/// The sequence of rows the row host displays.
/// </summary>
/// <remarks>
/// This is the boundary between the table's item view and the row host: it adapts <see cref="CollectionView"/> to
/// the shape <c>ItemsSourceView</c> recognises (a non-generic <see cref="IList"/> plus plain
/// <see cref="INotifyCollectionChanged"/>, rather than the WinRT vector-changed shape <see cref="ICollectionView"/>
/// exposes) on both Windows App SDK and Uno.
/// <para>
/// Today every visual index is also an item index — what <see cref="TableViewCellSlot.Row"/>,
/// <see cref="TableView.SelectedRanges"/>, the clipboard and automation all mean by "row". The distinction exists
/// so a future feature that displays rows the item view does not (such as group header rows) has a single seam to
/// extend rather than a page of call sites to update.
/// </para>
/// </remarks>
internal sealed class TableViewVisualRows : IList, INotifyCollectionChanged
{
    private readonly CollectionView _collectionView;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewVisualRows"/> class.
    /// </summary>
    /// <param name="collectionView">The item view to project.</param>
    public TableViewVisualRows(CollectionView collectionView)
    {
        _collectionView = collectionView;
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public int Count => _collectionView.Count;

    /// <inheritdoc/>
    public object? this[int index]
    {
        get => _collectionView[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns the item index the given visual index maps to.
    /// </summary>
    public int GetItemIndex(int visualIndex) => visualIndex;

    /// <summary>
    /// Returns the visual index the given item index maps to.
    /// </summary>
    public int GetVisualIndex(int itemIndex) => itemIndex;

    /// <summary>
    /// Reprojects after the item view changed shape in a way that is not a single insert or remove, and reports a
    /// reset.
    /// </summary>
    public void Reset()
    {
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Reports that an item was inserted at the given item index.
    /// </summary>
    public void OnItemInserted(int itemIndex)
    {
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add, this[itemIndex], itemIndex));
    }

    /// <summary>
    /// Reports that an item was removed. <paramref name="visualIndex"/> is the visual index the row occupied,
    /// captured before the removal.
    /// </summary>
    public void OnItemRemoved(int visualIndex, object? item)
    {
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove, item, visualIndex));
    }

    // Mutation and the rest of IList are not supported: this is a projection.

    /// <inheritdoc/>
    public bool IsFixedSize => false;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public bool IsSynchronized => false;

    /// <inheritdoc/>
    public object SyncRoot => this;

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc/>
    public bool Contains(object? value) => _collectionView.IndexOf(value) >= 0;

    /// <inheritdoc/>
    public int IndexOf(object? value) => _collectionView.IndexOf(value);

    /// <inheritdoc/>
    public void CopyTo(Array array, int index)
    {
        for (var i = 0; i < Count; i++)
        {
            array.SetValue(this[i], index + i);
        }
    }

    /// <inheritdoc/>
    public int Add(object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Clear() => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Insert(int index, object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Remove(object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void RemoveAt(int index) => throw new NotSupportedException();
}
