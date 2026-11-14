using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
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
    private TableViewColumn[]? _visibleColumns; // VisibleColumns, until a column is added/removed/hidden/reordered
    private int _deferDepth;
    private bool _frozenColumnsStale;

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
        InvalidateVisibleColumns(args.CollectionChange == CollectionChange.ItemInserted && args.Index < Count ? (int)args.Index : -1);

        if (_movingColumn) return; // Skip processing if it's a move action

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
        // Inside DeferColumnUpdates the frozen state is worked out once, when the batch ends.
        if (_deferDepth > 0)
        {
            _frozenColumnsStale = true;
            return;
        }

        _frozenColumnsStale = false;
        // VisibleColumns materialises a fresh sorted list on every read, so evaluating it inside
        // the loop (once per column, then IndexOf over the result) made this O(n^2 log n) per call.
        // It runs on every Add, which made building an n-column table O(n^3 log n): 175 columns
        // cost ~220 ms and 350 columns ~1.9 s. Resolve the visible order once and look each
        // column up in a set; semantics are unchanged (a non-visible column keeps IndexOf == -1,
        // i.e. it is still reported as frozen, exactly as before).
        var frozenColumnCount = TableView?.FrozenColumnCount ?? 0;
        var visibleColumns = VisibleColumnsSnapshot;
        var scrollableColumns = new HashSet<TableViewColumn>(visibleColumns.Skip(frozenColumnCount));

        foreach (var column in this.OfType<TableViewColumn>())
        {
            column.IsFrozen = !scrollableColumns.Contains(column);
        }
    }

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    internal void HandleColumnPropertyChanged(TableViewColumn column, string propertyName)
    {
        if (propertyName is nameof(TableViewColumn.Visibility) or nameof(TableViewColumn.Order))
            InvalidateVisibleColumns(-1);

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
    /// The visible columns in display order are worked out once and kept until a column is added,
    /// removed, hidden, shown or reordered. A read returns a READ-ONLY view of that snapshot: a caller may keep it (a later
    /// change makes a new snapshot, never edits this one) but not change it.
    /// Building a 178-column table used to sort every column on every <c>Add</c> (and again for each realised row): 1.19 s
    /// of a wide table's first layout on a Release build.
    /// </remarks>
    public IList<TableViewColumn> VisibleColumns => _visibleColumnsView is { } view && _visibleColumns is not null
        ? view
        : _visibleColumnsView = Array.AsReadOnly(VisibleColumnsSnapshot);

    private System.Collections.ObjectModel.ReadOnlyCollection<TableViewColumn>? _visibleColumnsView;

    /// <summary>The cached visible columns in display order (never handed out: <see cref="VisibleColumns"/> copies it).</summary>
    private TableViewColumn[] VisibleColumnsSnapshot => _visibleColumns ??= [.. this.OfType<TableViewColumn>()
                                                                                .Where(x => x.Visibility == Visibility.Visible)
                                                                                .OrderBy(x => x.Order ?? 0)];

    /// <summary>
    /// Forgets the cached visible columns - unless the change is a visible column appended at the END whose order sorts at
    /// or after every visible column's (how a table is built), which extends the cache instead: <c>OrderBy</c> is stable,
    /// so a recomputation would place it last too.
    /// </summary>
    private void InvalidateVisibleColumns(int insertedIndex)
    {
        if (_visibleColumns is { } cached && insertedIndex >= 0 && insertedIndex == Count - 1
            && this[insertedIndex] is TableViewColumn added)
        {
            if (added.Visibility != Visibility.Visible) return; // not in the list, and nothing before it moved
            if (cached.Length == 0 || (added.Order ?? 0) >= (cached[^1].Order ?? 0))
            {
                _visibleColumns = [.. cached, added];
                _visibleColumnsView = null;
                return;
            }
        }

        _visibleColumns = null;
        _visibleColumnsView = null;
    }

    /// <summary>
    /// Defers the per-change column bookkeeping (which columns are frozen) until the returned scope
    /// is disposed, so adding n columns does it once instead of n times. Scopes nest; the work runs when the outermost one
    /// ends. Collection-changed notifications are still raised per change.
    /// </summary>
    public IDisposable DeferColumnUpdates()
    {
        _deferDepth++;
        return new ColumnUpdateDeferral(this);
    }

    private void EndDeferral()
    {
        if (_deferDepth == 0) return;
        if (--_deferDepth == 0 && _frozenColumnsStale)
            UpdateFrozenColumns();
    }

    private sealed class ColumnUpdateDeferral(TableViewColumnsCollection owner) : IDisposable
    {
        private TableViewColumnsCollection? _owner = owner;

        public void Dispose()
        {
            _owner?.EndDeferral();
            _owner = null;
        }
    }

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

        UpdateFrozenColumns();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, column, newIndex, oldIndex));

        _movingColumn = false;
    }
}