using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
        index => index >= 0 && index < _collectionView.Count ? _collectionView[index] : null,
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

        if (SelectionMode is ListViewSelectionMode.Single)
        {
            SelectedIndex = itemIndexRange.FirstIndex;
            return;
        }

        ChangeSelection(model => model.Select(itemIndexRange.FirstIndex, itemIndexRange.LastIndex));
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
        ChangeSelection(model => itemIndex < 0 ? model.Clear() : model.SelectOnly(itemIndex));
    }

    /// <summary>
    /// Applies a change to the selection model and, when it changed anything, updates the affected rows' visuals
    /// and raises <see cref="SelectionChanged"/>.
    /// </summary>
    private void ChangeSelection(System.Func<TableViewSelectionModel, bool> change)
    {
        var before = _rowSelection.Clone();

        if (!change(_rowSelection))
        {
            return;
        }

        // The two directions of the diff, computed from ranges rather than from item lists.
        var added = _rowSelection.Clone();
        var removed = before;

        foreach (var range in before.GetRanges())
        {
            added.Deselect(range.FirstIndex, range.LastIndex);
        }

        foreach (var range in _rowSelection.GetRanges())
        {
            removed.Deselect(range.FirstIndex, range.LastIndex);
        }

        if (added.Count is 0 && removed.Count is 0)
        {
            return;
        }

        SyncSelectionProperties();
        ApplySelectionToRealizedRows(added, removed);
        OnRowSelectionChanged(added, removed);

        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(
            ProjectItems(removed),
            ProjectItems(added)));
    }

    /// <summary>
    /// Projects a selection span onto the items it covers, without materialising them.
    /// </summary>
    private IList<object> ProjectItems(TableViewSelectionModel model)
    {
        return new TableViewSelectedItemsCollection(
            model,
            index => index >= 0 && index < _collectionView.Count ? _collectionView[index] : null,
            item => _collectionView.IndexOf(item),
            static (_, _) => { },
            static () => { });
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
                row.IsSelected = _rowSelection.Contains(index);
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
            SetValue(SelectedItemProperty, index >= 0 && index < _collectionView.Count ? _collectionView[index] : null);
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
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TableView tableView || tableView._isSyncingSelectionProperties)
        {
            return;
        }

        tableView.SelectSingleItem(e.NewValue is int index ? index : -1);
    }
}
