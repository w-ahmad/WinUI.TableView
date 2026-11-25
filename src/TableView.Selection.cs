using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using WinUI.TableView.Selection;

namespace WinUI.TableView;

/// <summary>
/// Row selection for <see cref="TableView"/>.
/// </summary>
/// <remarks>
/// Row selection is stored as index ranges in a <see cref="TableViewSelectionModel"/> and projected on demand,
/// replacing what <c>ListViewBase</c> used to supply. Selecting a span of a million rows is one range, and no
/// row has to be realized for it: only the rows that happen to be on screen get their visuals updated.
/// </remarks>
public partial class TableView
{
    private readonly TableViewSelectionModel _rowSelection = new();
    private TableViewSelectedItemsCollection? _selectedItems;
    private bool _isSyncingSelectionProperties;
    private const int SelectionChangeSnapshotLimit = 1024;

    /// <summary>
    /// Identifies the <see cref="SelectedItem"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(TableView), new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Identifies the <see cref="SelectedIndex"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(TableView), new PropertyMetadata(-1, OnSelectedIndexChanged));

    /// <summary>
    /// Occurs when the row selection changes.
    /// </summary>
    /// <remarks>
    /// The added and removed items are copies when the change covers up to 1,024 items, which every click and
    /// keyboard change does. A larger change, such as selecting all of a long list, reports them as projections
    /// over the changed index ranges instead, read through the view when they are enumerated: copy them before
    /// changing the source if a handler needs them afterwards.
    /// </remarks>
    public event SelectionChangedEventHandler? SelectionChanged;

    /// <summary>
    /// Gets or sets the first selected item, or <see langword="null"/> when nothing is selected.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of the first selected item, or -1 when nothing is selected.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Gets the selected items.
    /// </summary>
    /// <remarks>
    /// A live projection over the selected index ranges, not a materialised list, so reading
    /// <see cref="ICollection{T}.Count"/> is O(1) even when everything is selected. Adding to and removing from
    /// the collection changes the selection, as it did when this came from <c>ListViewBase</c>.
    /// </remarks>
    public IList<object> SelectedItems => _selectedItems ??= new TableViewSelectedItemsCollection(
        _rowSelection,
        ItemAt,
        item => _collectionView.IndexOf(item),
        (index, isSelected) => SetItemSelected(index, isSelected),
        DeselectAllItems);

    /// <summary>
    /// Gets the selected rows as index ranges.
    /// </summary>
    public IReadOnlyList<ItemIndexRange> SelectedRanges => [.. _rowSelection.GetRanges()];

    /// <summary>
    /// Selects the rows in the given range.
    /// </summary>
    /// <param name="itemIndexRange">The range of item indexes to select.</param>
    public void SelectRange(ItemIndexRange itemIndexRange)
    {
        if (SelectionMode is ListViewSelectionMode.None)
        {
            return;
        }

        // Limited to the items there are: a selected index without an item reads as a null item.
        var first = Math.Max(0, itemIndexRange.FirstIndex);
        var last = Math.Min(itemIndexRange.LastIndex, _collectionView.Count - 1);

        if (first > last)
        {
            return;
        }

        if (SelectionMode is ListViewSelectionMode.Single)
        {
            SelectedIndex = first;
            return;
        }

        ChangeSelection(model => model.Select(first, last));
    }

    /// <summary>
    /// Deselects the rows in the given range.
    /// </summary>
    /// <param name="itemIndexRange">The range of item indexes to deselect.</param>
    public void DeselectRange(ItemIndexRange itemIndexRange)
    {
        ChangeSelection(model => model.Deselect(itemIndexRange.FirstIndex, itemIndexRange.LastIndex));
    }

    /// <summary>
    /// Determines whether the row at the given item index is selected.
    /// </summary>
    /// <param name="itemIndex">The item index to test.</param>
    public bool IsRowSelected(int itemIndex) => _rowSelection.Contains(itemIndex);

    /// <summary>
    /// Selects or deselects a single row.
    /// </summary>
    private void SetItemSelected(int itemIndex, bool isSelected)
    {
        if (isSelected && SelectionMode is ListViewSelectionMode.None)
        {
            return;
        }

        if (isSelected && SelectionMode is ListViewSelectionMode.Single)
        {
            SelectedIndex = itemIndex;
            return;
        }

        ChangeSelection(model => isSelected
            ? model.Select(itemIndex, itemIndex)
            : model.Deselect(itemIndex, itemIndex));
    }

    /// <summary>
    /// Replaces the selection with a single row, or clears it when <paramref name="itemIndex"/> is negative.
    /// </summary>
    private void SelectSingleItem(int itemIndex)
    {
        if (itemIndex >= 0 && (SelectionMode is ListViewSelectionMode.None || SelectionUnit is TableViewSelectionUnit.Cell))
        {
            return;
        }

        ChangeSelection(model => itemIndex < 0 || itemIndex >= _collectionView.Count ? model.Clear() : model.SelectOnly(itemIndex));
    }

    /// <summary>
    /// Makes a row the whole selection and the selection anchor.
    /// </summary>
    internal void SelectOnlyRow(int index)
    {
        if (!CanSelectRow(index))
        {
            return;
        }

        SelectionStartRowIndex = CurrentRowIndex = index;
        SelectSingleItem(index);
    }

    /// <summary>
    /// Adds a row to the selection without touching the others, and makes it the selection anchor.
    /// </summary>
    internal void AddRowToSelection(int index)
    {
        if (SelectionMode is ListViewSelectionMode.Single)
        {
            SelectOnlyRow(index);
        }
        else if (CanSelectRow(index))
        {
            SelectionStartRowIndex = CurrentRowIndex = index;
            SelectRange(new ItemIndexRange(index, 1));
        }
    }

    /// <summary>
    /// Deselects the row a press landed on when the press toggles it: any press in Multiple mode, a press with
    /// Ctrl in Extended mode.
    /// </summary>
    /// <returns>True when the row was selected and the press deselected it.</returns>
    internal bool DeselectRowOnPress(int? index, bool ctrlKey)
    {
        var toggles = SelectionMode is ListViewSelectionMode.Multiple
            || (ctrlKey && SelectionMode is ListViewSelectionMode.Extended);

        if (!toggles || index is not { } row || !_rowSelection.Contains(row))
        {
            return false;
        }

        DeselectRange(new ItemIndexRange(row, 1));
        CurrentRowIndex = row;

        return true;
    }

    /// <summary>
    /// Applies a row's <see cref="TableViewRow.IsSelected"/>, set from outside, to the selection.
    /// </summary>
    /// <returns>Whether the row is selected afterwards.</returns>
    internal bool SetRowSelectedFromRow(int index, bool isSelected)
    {
        if (!isSelected || CanSelectRow(index))
        {
            SetItemSelected(index, isSelected);
        }

        return _rowSelection.Contains(index);
    }

    private bool CanSelectRow(int index)
    {
        return SelectionMode is not ListViewSelectionMode.None
            && SelectionUnit is not TableViewSelectionUnit.Cell
            && index >= 0
            && index < _collectionView.Count;
    }

    /// <summary>
    /// Reports a selected item that left the view, after the model has dropped it.
    /// </summary>
    private void OnSelectedItemRemoved(object? item)
    {
        SyncSelectionProperties();
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(item is null ? [] : [item], []));
    }

    /// <summary>
    /// Applies a change to the selection model and, when it changed anything, updates the affected rows' visuals
    /// and raises <see cref="SelectionChanged"/>.
    /// </summary>
    /// <param name="change">Changes the model and reports whether it did.</param>
    /// <param name="snapshotRemovedItems">
    /// Copies the removed items into the event instead of projecting them lazily. Needed when the item view is
    /// about to be rebuilt, after which the removed indexes would resolve to whatever items land on them.
    /// </param>
    private void ChangeSelection(Func<TableViewSelectionModel, bool> change, bool snapshotRemovedItems = false)
    {
        // Snapshotted items cannot wait for the end of a batch: the view is about to be rebuilt.
        var deferEvent = _selectionBatchDepth > 0 && !snapshotRemovedItems;

        if (deferEvent)
        {
            _rowSelectionBeforeBatch ??= _rowSelection.Clone();
        }
        else
        {
            RaisePendingSelectionChanged();
        }

        var before = _rowSelection.Clone();

        if (!change(_rowSelection) || !TryGetSelectionDiff(before, out var added, out var removed))
        {
            return;
        }

        SyncSelectionProperties();
        ApplySelectionToRealizedRows(added, removed);
        OnRowSelectionChanged(added, removed);

        if (!deferEvent)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(
                ProjectItems(removed, snapshotRemovedItems),
                ProjectItems(added)));
        }
    }

    /// <summary>
    /// Computes what the selection gained and lost since <paramref name="before"/>, which is consumed.
    /// </summary>
    private bool TryGetSelectionDiff(TableViewSelectionModel before, out TableViewSelectionModel added, out TableViewSelectionModel removed)
    {
        // The two directions of the diff, computed from ranges rather than from item lists.
        added = _rowSelection.Clone();
        removed = before;

        foreach (var range in before.GetRanges())
        {
            added.Deselect(range.FirstIndex, range.LastIndex);
        }

        foreach (var range in _rowSelection.GetRanges())
        {
            removed.Deselect(range.FirstIndex, range.LastIndex);
        }

        return added.Count > 0 || removed.Count > 0;
    }

    /// <summary>
    /// Starts a batch: the row and cell selection changes made until <see cref="EndSelectionBatch"/> are
    /// reported by one <see cref="SelectionChanged"/> and one <see cref="CellSelectionChanged"/>.
    /// </summary>
    private void BeginSelectionBatch()
    {
        _selectionBatchDepth++;
    }

    /// <summary>
    /// Ends a batch and raises the events for what it changed overall.
    /// </summary>
    private void EndSelectionBatch()
    {
        if (--_selectionBatchDepth > 0)
        {
            return;
        }

        RaisePendingSelectionChanged();

        if (_isCellSelectionChangePending)
        {
            _isCellSelectionChangePending = false;
            OnCellSelectionChanged();
        }
    }

    private void RaisePendingSelectionChanged()
    {
        if (_rowSelectionBeforeBatch is not { } before)
        {
            return;
        }

        _rowSelectionBeforeBatch = null;

        if (TryGetSelectionDiff(before, out var added, out var removed))
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(ProjectItems(removed), ProjectItems(added)));
        }
    }

    /// <summary>
    /// Deselects everything just before the item view is rebuilt, reporting the removed items as they were.
    /// </summary>
    private void DeselectAllBeforeViewReset()
    {
        ChangeSelection(static model => model.Clear(), snapshotRemovedItems: true);
        DeselectAllCells();

        // The anchors are indexes into the view being rebuilt.
        SelectionStartRowIndex = null;
        SelectionStartCellSlot = null;
        CurrentRowIndex = null;
    }

    /// <summary>
    /// Projects a selection span onto the items it covers. A small span is copied, so a handler can change the
    /// source while reading it; a large one stays a read-only projection that materialises nothing.
    /// </summary>
    private IList<object> ProjectItems(TableViewSelectionModel model, bool snapshot = false)
    {
        var items = new TableViewSelectedItemsCollection(model, ItemAt, item => _collectionView.IndexOf(item));

        return snapshot || model.Count <= SelectionChangeSnapshotLimit
            ? new System.Collections.ObjectModel.ReadOnlyCollection<object>([.. items])
            : items;
    }

    /// <summary>
    /// Returns the item at an item index, or <see langword="null"/> when the index is out of range.
    /// </summary>
    private object? ItemAt(int index)
    {
        return index >= 0 && index < _collectionView.Count ? _collectionView[index] : null;
    }

    /// <summary>
    /// Pushes the new selection state onto the rows that are realized. Rows that are not realized pick it up when
    /// they are prepared, which is why selecting a million rows costs nothing per row.
    /// </summary>
    private void ApplySelectionToRealizedRows(TableViewSelectionModel added, TableViewSelectionModel removed)
    {
        foreach (var row in _rows)
        {
            var index = row.Index;

            if (index >= 0 && (added.Contains(index) || removed.Contains(index)))
            {
                row.SetSelectionState(_rowSelection.Contains(index), animate: true);
                row.NotifySelectionChanged(isOnlySelection: _rowSelection.Count is 1);
            }
        }
    }

    /// <summary>
    /// Keeps <see cref="SelectedIndex"/> and <see cref="SelectedItem"/> in step with the model.
    /// </summary>
    private void SyncSelectionProperties()
    {
        _isSyncingSelectionProperties = true;

        try
        {
            var index = _rowSelection.FirstIndex;

            SetValue(SelectedIndexProperty, index);
            SetValue(SelectedItemProperty, ItemAt(index));
        }
        finally
        {
            _isSyncingSelectionProperties = false;
        }
    }

    /// <summary>
    /// Keeps the selection valid when the item view changes shape.
    /// </summary>
    private void OnItemsChangedForSelection()
    {
        if (_rowSelection.TrimTo(_collectionView.Count))
        {
            SyncSelectionProperties();
        }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TableView tableView || tableView._isSyncingSelectionProperties)
        {
            return;
        }

        tableView.SelectSingleItem(tableView._collectionView.IndexOf(e.NewValue));

        if (!Equals(tableView.ItemAt(tableView._rowSelection.FirstIndex), tableView.SelectedItem))
        {
            tableView.SyncSelectionProperties();
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TableView tableView || tableView._isSyncingSelectionProperties)
        {
            return;
        }

        tableView.SelectSingleItem(e.NewValue is int index ? index : -1);

        // An index without an item selects nothing, and the property says so.
        if (tableView._rowSelection.FirstIndex != tableView.SelectedIndex)
        {
            tableView.SyncSelectionProperties();
        }
    }
}
