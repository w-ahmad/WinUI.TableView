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
    private TableViewColumn[] _itemsCopy = []; // To keep a copy of the items to keep track of removed items
    private bool _movingColumn;
    private ReadOnlyCollection<TableViewColumn>? _visibleColumns;

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

        InvalidateVisibleColumns();
        UpdateFrozenColumns();

        var index = (int)args.Index;

        switch (args.CollectionChange)
        {
            case CollectionChange.ItemInserted:
                if (args.Index < Count)
                {
                    var column = (TableViewColumn)sender[index];
                    column.SetOwningCollection(this);
                    column.SetOwningTableView(((ITableViewColumnsCollection)this).TableView!);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, column, (int)args.Index));
                }
                break;
            case CollectionChange.ItemRemoved:
                if (args.Index < _itemsCopy.Length)
                {
                    var column = _itemsCopy[index];
                    column.SetOwningCollection(null!);
                    column.SetOwningTableView(null!);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, column, (int)args.Index));
                }
                break;
            case CollectionChange.Reset:
                foreach (var item in _itemsCopy)
                {
                    item.SetOwningCollection(null!);
                    item.SetOwningTableView(null!);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                break;
        }

        _itemsCopy = new TableViewColumn[Count];
        CopyTo(_itemsCopy, 0);
    }

    internal void UpdateFrozenColumns()
    {
        // Read VisibleColumns once: it used to be re-evaluated (allocating and sorting a new list)
        // for every column in this loop.
        var visibleColumns = VisibleColumns;
        var frozenColumnCount = TableView?.FrozenColumnCount ?? 0;

        foreach (var column in this.OfType<TableViewColumn>())
        {
            column.IsFrozen = visibleColumns.IndexOf(column) < frozenColumnCount;
        }
    }

    /// <summary>
    /// Drops the cached <see cref="VisibleColumns"/> projection so the next read rebuilds it.
    /// </summary>
    internal void InvalidateVisibleColumns()
    {
        _visibleColumns = null;
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

        if (Contains(column) && !_movingColumn)
        {
            var index = IndexOf(column);
            ColumnPropertyChanged?.Invoke(this, new TableViewColumnPropertyChangedEventArgs(column, propertyName, index));
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
        [.. this.OfType<TableViewColumn>()
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
        _movingColumn = true;

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

        // Assign the same order value as the target column, ensuring correct order for the VisibleColumns collection.
        column.Order = columnBefore.Order;

        RemoveAt(oldIndex);
        Insert(newIndex, column);

        InvalidateVisibleColumns(); // OnVectorChanged is suppressed while _movingColumn is set.
        UpdateFrozenColumns();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, column, newIndex, oldIndex));

        _movingColumn = false;
    }
}