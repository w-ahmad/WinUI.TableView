using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation.Collections;

namespace WinUI.TableView;

/// <summary>
/// Represents a collection of <see cref="TableViewColumn"/> objects used in a <see cref="WinUI.TableView.TableView"/>.
/// </summary>
/// <remarks>This collection provides functionality for managing columns in a <see cref="WinUI.TableView.TableView"/>, including adding,
/// removing,  and tracking changes to column properties. It supports notifications for collection changes and column 
/// property changes, enabling dynamic updates to the <see cref="WinUI.TableView.TableView"/>.</remarks>
public partial class TableViewColumnsCollection : DependencyObjectCollection, ITableViewColumnsCollection
{
    // The items, kept in step with the collection: reading them back from it costs a call per item.
    private readonly List<TableViewColumn> _items = [];
    private bool _movingColumn;
    private ReadOnlyCollection<TableViewColumn>? _visibleColumns;
    private double[]? _visibleColumnOffsets;
    private double[]? _estimatedColumnOffsets;
    private Dictionary<TableViewColumn, int>? _visibleColumnIndexes;

    /// <inheritdoc/>
    public event EventHandler<TableViewColumnPropertyChangedEventArgs>? ColumnPropertyChanged;
    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// The constructor for the <see cref="TableViewColumnsCollection"/> class.
    /// </summary>
    /// <param name="tableView">
    /// The <see cref="WinUI.TableView.TableView"/> that owns this collection.
    /// </param>
    public TableViewColumnsCollection(TableView tableView)
    {
        TableView = tableView ?? throw new ArgumentNullException(nameof(tableView));
        VectorChanged += OnVectorChanged;
    }

    /// <summary>
    /// Handles changes to the underlying vector of <see cref="DependencyObject"/> items.
    /// </summary>
    private void OnVectorChanged(IObservableVector<DependencyObject> sender, IVectorChangedEventArgs args)
    {
        if (_movingColumn) return; // Skip processing if it's a move action

        var index = (int)args.Index;
        var isInsert = args.CollectionChange is CollectionChange.ItemInserted;
        TableViewColumn[] removed = isInsert ? [] : [.. _items];

        if (isInsert && index <= _items.Count && index < Count)
        {
            _items.Insert(index, (TableViewColumn)sender[index]);
        }
        else if (args.CollectionChange is CollectionChange.ItemRemoved && index < _items.Count)
        {
            _items.RemoveAt(index);
        }
        else
        {
            CopyItems();
        }

        InvalidateVisibleColumns();
        UpdateFrozenColumns();

        switch (args.CollectionChange)
        {
            case CollectionChange.ItemInserted:
                if (index < _items.Count)
                {
                    var column = _items[index];
                    column.SetOwningCollection(this);
                    column.SetOwningTableView(((ITableViewColumnsCollection)this).TableView!);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, column, (int)args.Index));
                }
                break;
            case CollectionChange.ItemRemoved:
                if (index < removed.Length)
                {
                    var column = removed[index];
                    // A column added twice is still owned while one of them remains.
                    if (!_items.Contains(column))
                    {
                        column.SetOwningCollection(null!);
                        column.SetOwningTableView(null!);
                    }
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, column, (int)args.Index));
                }
                break;
            case CollectionChange.Reset:
                foreach (var item in removed)
                {
                    item.SetOwningCollection(null!);
                    item.SetOwningTableView(null!);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                break;
        }
    }

    private void CopyItems()
    {
        _items.Clear();
        _items.AddRange(this.OfType<TableViewColumn>());
    }

    internal void UpdateFrozenColumns()
    {
        var frozenColumnCount = TableView?.FrozenColumnCount ?? 0;
        // A hidden column (index -1) counts as frozen, and a column added twice goes by its first place.
        foreach (var column in _items)
        {
            column.IsFrozen = GetVisibleColumnIndex(column) < frozenColumnCount;
        }
    }

    /// <summary>
    /// Drops the cached <see cref="VisibleColumns"/> projection so the next read rebuilds it.
    /// </summary>
    internal void InvalidateVisibleColumns()
    {
        _visibleColumns = null;
        _visibleColumnIndexes = null;
        InvalidateColumnOffsets();
    }

    private void InvalidateColumnOffsets()
    {
        _visibleColumnOffsets = null;
        _estimatedColumnOffsets = null;
        LayoutVersion++;
    }

    /// <summary>
    /// Gets a number that changes whenever the visible columns, their order or their widths change.
    /// </summary>
    internal int LayoutVersion { get; private set; }

    /// <summary>
    /// Finds the visible columns, from a given index on, that overlap a span measured from that column's left edge.
    /// </summary>
    /// <remarks>
    /// A column that has not been sized yet counts with the width it asks for, so that a table whose columns are
    /// all unsized does not report every one of them as inside the span.
    /// </remarks>
    internal (int First, int Last) FindVisibleColumns(int from, double start, double end)
    {
        var count = VisibleColumns.Count;

        if (from >= count)
        {
            return (count, count - 1);
        }

        if (_estimatedColumnOffsets is null)
        {
            var columns = VisibleColumns;
            var offsets = new double[count + 1];

            for (var i = 0; i < count; i++)
            {
                var width = columns[i].ActualWidth;

                if (!(width > 0d))
                {
                    width = columns[i].Width is { IsAbsolute: true, Value: > 0d and var asked } ? asked : UnsizedColumnWidth;
                }

                offsets[i + 1] = offsets[i] + width;
            }

            _estimatedColumnOffsets = offsets;
        }

        var estimated = _estimatedColumnOffsets;
        var origin = estimated[from];

        // The first column whose right edge is past the start.
        var low = from;
        var high = count;

        while (low < high)
        {
            var middle = (low + high) / 2;

            if (estimated[middle + 1] - origin > start)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        var first = Math.Min(low, count - 1);

        // The last column whose left edge is before the end.
        low = first;
        high = count;

        while (low < high)
        {
            var middle = (low + high) / 2;

            if (estimated[middle] - origin >= end)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        var last = Math.Max(low - 1, first);

        return (first, last);
    }

    private const double UnsizedColumnWidth = 100d;

    /// <summary>
    /// Gets the index of a column among the visible columns, or -1 when it is not one of them.
    /// </summary>
    internal int GetVisibleColumnIndex(TableViewColumn column)
    {
        if (_visibleColumnIndexes is null)
        {
            var columns = VisibleColumns;
            var indexes = new Dictionary<TableViewColumn, int>(columns.Count);

            for (var i = 0; i < columns.Count; i++)
            {
                // A column added twice goes by its first place.
                indexes.TryAdd(columns[i], i);
            }

            _visibleColumnIndexes = indexes;
        }

        return _visibleColumnIndexes.TryGetValue(column, out var index) ? index : -1;
    }

    /// <summary>
    /// Gets the distance from the first visible column's left edge to the left edge of the visible column at an
    /// index, or to the right edge of the last one when the index is the number of visible columns.
    /// </summary>
    internal double GetVisibleColumnOffset(int index)
    {
        if (_visibleColumnOffsets is null)
        {
            var columns = VisibleColumns;
            var offsets = new double[columns.Count + 1];

            for (var i = 0; i < columns.Count; i++)
            {
                // A column that has not been sized yet takes no room.
                offsets[i + 1] = offsets[i] + (columns[i].ActualWidth is > 0d and var width ? width : 0d);
            }

            _visibleColumnOffsets = offsets;
        }

        return _visibleColumnOffsets[Math.Clamp(index, 0, _visibleColumnOffsets.Length - 1)];
    }

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    internal void HandleColumnPropertyChanged(TableViewColumn column, string propertyName)
    {
        if (propertyName is nameof(TableViewColumn.Visibility) or nameof(TableViewColumn.Order))
        {
            InvalidateVisibleColumns();
        }
        else if (propertyName is nameof(TableViewColumn.ActualWidth) or nameof(TableViewColumn.Width))
        {
            InvalidateColumnOffsets();
        }

        if (!_movingColumn && ColumnPropertyChanged is { } handler && _items.IndexOf(column) is >= 0 and var index)
        {
            handler.Invoke(this, new TableViewColumnPropertyChangedEventArgs(column, propertyName, index));
        }
    }

    /// <inheritdoc/>
    public TableView? TableView { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// The projection is cached and rebuilt only when the collection changes or a column's
    /// <see cref="TableViewColumn.Visibility"/> or <see cref="TableViewColumn.Order"/> changes. It is read on
    /// hot paths (hit testing, cell insertion, clipboard) that used to allocate and sort a new list per access,
    /// sometimes once per loop iteration.
    /// </remarks>
    public IList<TableViewColumn> VisibleColumns => _visibleColumns ??= new ReadOnlyCollection<TableViewColumn>(
        [.. _items
               .Where(x => x.Visibility == Visibility.Visible)
               .OrderBy(x => x.Order ?? 0)]);

    TableViewColumn IList<TableViewColumn>.this[int index]
    {
        get => (TableViewColumn)base[index];
        set => base[index] = value;
    }

    int ICollection<TableViewColumn>.Count => Count;

    bool ICollection<TableViewColumn>.IsReadOnly => IsReadOnly;

    void ICollection<TableViewColumn>.Add(TableViewColumn item)
    {
        Add(item);
    }

    void ICollection<TableViewColumn>.Clear()
    {
        Clear();
    }

    bool ICollection<TableViewColumn>.Contains(TableViewColumn item)
    {
        return Contains(item);
    }

    void ICollection<TableViewColumn>.CopyTo(TableViewColumn[] array, int arrayIndex)
    {
        CopyTo(array, arrayIndex);
    }

    IEnumerator<TableViewColumn> IEnumerable<TableViewColumn>.GetEnumerator()
    {
        foreach (var item in this)
        {
            yield return (TableViewColumn)item;
        }
    }

    int IList<TableViewColumn>.IndexOf(TableViewColumn item)
    {
        return IndexOf(item);
    }

    void IList<TableViewColumn>.Insert(int index, TableViewColumn item)
    {
        Insert(index, item);
    }

    bool ICollection<TableViewColumn>.Remove(TableViewColumn item)
    {
        var index = IndexOf(item);

        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    void IList<TableViewColumn>.RemoveAt(int index)
    {
        RemoveAt(index);
    }

    /// <inheritdoc/>
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(oldIndex), "Old index is out of range.");
        }
        if (newIndex < 0 || newIndex >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex), "New index is out of range.");
        }
        if (oldIndex == newIndex)
        {
            return; // No need to move if the indices are the same
        }

        var column = (TableViewColumn)this[oldIndex];
        var columnBefore = (TableViewColumn)this[newIndex];

        _movingColumn = true;

        try
        {
            // Assign the same order value as the target column, ensuring correct order for the VisibleColumns collection.
            column.Order = columnBefore.Order;

            RemoveAt(oldIndex);
            Insert(newIndex, column);

            // OnVectorChanged is suppressed while _movingColumn is set, so its bookkeeping is done here.
            CopyItems();
            InvalidateVisibleColumns();
            UpdateFrozenColumns();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, column, newIndex, oldIndex));
        }
        finally
        {
            _movingColumn = false;
        }
    }
}