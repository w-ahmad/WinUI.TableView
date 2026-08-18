using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using WinUI.TableView.Helpers;
using WinUI.TableView.Layout;

namespace WinUI.TableView;

/// <summary>
/// Row hosting for <see cref="TableView"/>: the <see cref="ItemsRepeater"/>, the virtualizing layout, and the
/// mapping between item indexes and visual row indexes.
/// </summary>
public partial class TableView
{
    private ItemsRepeater? _rowsRepeater;
    private TableViewRowsLayout? _rowsLayout;
    private TableViewRowElementFactory? _rowElementFactory;
    private TableViewVisualRows? _visualRows;
    private bool _isLoadingMoreItems;

    /// <summary>
    /// Gets the items produced by the current sorting, filtering and grouping.
    /// </summary>
    /// <remarks>
    /// Positions in this collection are <em>item indexes</em>: what <see cref="TableViewCellSlot.Row"/>,
    /// <see cref="SelectedRanges"/>, the clipboard and automation all mean by "row". They are unaffected by group
    /// headers and by groups being collapsed.
    /// </remarks>
    public IList<object> Items => _collectionView;

    /// <summary>
    /// Gets the currently realized row containers.
    /// </summary>
    internal IReadOnlyList<TableViewRow> Rows => _rows;

    /// <summary>
    /// Gets the number of visual rows, counting group headers and excluding collapsed groups' contents.
    /// </summary>
    internal int VisualRowCount => _visualRows?.Count ?? 0;

    /// <summary>
    /// Returns the realized row for an item index, or <see langword="null"/> when it is not realized.
    /// </summary>
    /// <param name="index">The item index.</param>
    public DependencyObject? ContainerFromIndex(int index)
    {
        var visualIndex = GetVisualIndexFromItemIndex(index);

        return visualIndex < 0 ? null : _rowsRepeater?.TryGetElement(visualIndex);
    }

    /// <summary>
    /// Returns the realized row for an item, or <see langword="null"/> when it is not realized.
    /// </summary>
    /// <param name="item">The item to find the row for.</param>
    public DependencyObject? ContainerFromItem(object? item)
    {
        return ContainerFromIndex(_collectionView.IndexOf(item));
    }

    /// <summary>
    /// Returns the item index for a realized row, or -1 when the element is not a realized row.
    /// </summary>
    /// <param name="container">The row element.</param>
    public int IndexFromContainer(DependencyObject container)
    {
        if (container is not UIElement element || _rowsRepeater is null)
        {
            return -1;
        }

        var visualIndex = _rowsRepeater.GetElementIndex(element);

        return visualIndex < 0 ? -1 : GetItemIndexFromVisualIndex(visualIndex);
    }

    /// <summary>
    /// Returns the item for a realized row, or <see langword="null"/>.
    /// </summary>
    /// <param name="container">The row element.</param>
    public object? ItemFromContainer(DependencyObject container)
    {
        var index = IndexFromContainer(container);

        return index < 0 ? null : _collectionView[index];
    }

    /// <summary>
    /// Converts an item index to the visual row index showing it, or -1 when it is inside a collapsed group.
    /// </summary>
    internal int GetVisualIndexFromItemIndex(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _collectionView.Count)
        {
            return -1;
        }

        return _visualRows?.GetVisualIndex(itemIndex) ?? itemIndex;
    }

    /// <summary>
    /// Converts a visual row index to the item index it shows, or -1 for a group header row.
    /// </summary>
    internal int GetItemIndexFromVisualIndex(int visualIndex)
    {
        return _visualRows?.GetItemIndex(visualIndex) ?? visualIndex;
    }

    /// <summary>
    /// Returns the group whose header sits at a visual row index, or <see langword="null"/> for a data row.
    /// </summary>
    internal TableViewGroup? GetGroupFromVisualIndex(int visualIndex)
    {
        return _visualRows?.GetGroup(visualIndex);
    }

    /// <summary>
    /// Creates a row, wired to this table.
    /// </summary>
    internal TableViewRow CreateRow()
    {
        var row = new TableViewRow { TableView = this };

        // Propagate the text settings the way the old container-generation path did.
        row.SetBinding(FontFamilyProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRow.TableView)}.{nameof(FontFamily)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        row.SetBinding(FontSizeProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRow.TableView)}.{nameof(FontSize)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        return row;
    }

    /// <summary>
    /// Connects the row host found in the control template.
    /// </summary>
    private void InitializeRowsHost(ItemsRepeater repeater)
    {
        if (ReferenceEquals(_rowsRepeater, repeater))
        {
            return;
        }

        DetachRowsHost();

        _rowsRepeater = repeater;
        _rowsLayout = new TableViewRowsLayout { TableView = this };
        _rowElementFactory = new TableViewRowElementFactory(this);
        _visualRows = new TableViewVisualRows(_collectionView, IsGroupCollapsed);

        // A small cache keeps the realized row count close to the viewport. The repeater's default of two
        // viewports either side would triple the number of live rows, and rows are not cheap elements.
        repeater.HorizontalCacheLength = 0d;
        repeater.VerticalCacheLength = 0.5d;
        repeater.Layout = _rowsLayout;
        repeater.ItemTemplate = _rowElementFactory;
        repeater.ItemsSource = _visualRows;

        repeater.ElementPrepared += OnRowElementPrepared;
        repeater.ElementClearing += OnRowElementClearing;
        repeater.ElementIndexChanged += OnRowElementIndexChanged;
    }

    /// <summary>
    /// Disconnects the current row host.
    /// </summary>
    private void DetachRowsHost()
    {
        if (_rowsRepeater is null)
        {
            return;
        }

        _rowsRepeater.ElementPrepared -= OnRowElementPrepared;
        _rowsRepeater.ElementClearing -= OnRowElementClearing;
        _rowsRepeater.ElementIndexChanged -= OnRowElementIndexChanged;
        _rowsRepeater.ItemsSource = null;
        _rowsRepeater.Layout = null;
        _rowsRepeater.ItemTemplate = null;
        _rowsRepeater = null;
        _rowsLayout = null;
        _rowElementFactory = null;
        _visualRows = null;
        _rows.Clear();
    }

    /// <summary>
    /// Reprojects the flattened row sequence, for when grouping or collapse state changes.
    /// </summary>
    private void RefreshVisualRows()
    {
        _visualRows?.Reset();
    }

    /// <summary>
    /// Applies the table's state to a row as it becomes visible. Everything a row needs to look right is pushed
    /// here rather than pulled per row from the whole table, which is what keeps realization proportional to the
    /// viewport.
    /// </summary>
    private void OnRowElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        switch (args.Element)
        {
            case TableViewRow row:
                AttachRow(row, args.Index);
                break;

            case TableViewGroupRow groupRow:
                groupRow.VisualIndex = args.Index;
                break;
        }
    }

    /// <summary>
    /// Releases a row as it leaves the realized range.
    /// </summary>
    private void OnRowElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is not TableViewRow row)
        {
            return;
        }

        // A row being recycled mid-edit would silently drop the edit and leave the table stuck in editing mode,
        // so commit it while the cell still exists.
        CommitEditIfEditing(row);

        _rows.Remove(row);
        row.VisualIndex = -1;
        row.Index = -1;
    }

    /// <summary>
    /// Re-indexes a realized row after an insertion or removal shifted it.
    /// </summary>
    private void OnRowElementIndexChanged(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
    {
        switch (args.Element)
        {
            case TableViewRow row:
                AttachRow(row, args.NewIndex);
                break;

            case TableViewGroupRow groupRow:
                groupRow.VisualIndex = args.NewIndex;
                break;
        }
    }

    /// <summary>
    /// Records a row's position and brings its selection, styling and current-cell visuals up to date.
    /// </summary>
    private void AttachRow(TableViewRow row, int visualIndex)
    {
        var itemIndex = GetItemIndexFromVisualIndex(visualIndex);

        row.TableView = this;
        row.VisualIndex = visualIndex;
        row.Index = itemIndex;
        row.IsSelected = itemIndex >= 0 && _rowSelection.Contains(itemIndex);

        if (!_rows.Contains(row))
        {
            _rows.Add(row);
        }

        row.EnsureCellsStyle(default, row.Content);
        row.EnsureAlternateColors();
        row.RowPresenter?.ApplyDetailsPaneState(row.Content);
        row.ApplyCellsSelectionState();

        if (CurrentCellSlot.HasValue)
        {
            row.ApplyCurrentCellState(CurrentCellSlot.Value);
        }
    }

    /// <summary>
    /// Commits an in-flight edit hosted by the given row.
    /// </summary>
    private void CommitEditIfEditing(TableViewRow row)
    {
        if (!IsEditing || CurrentCellSlot is not { } slot || slot.Row != row.Index)
        {
            return;
        }

        if (GetCellFromSlot(slot) is { } cell && EndCellEditing(TableViewEditAction.Commit, cell))
        {
            SetIsEditing(false);
        }
    }

    /// <summary>
    /// Keeps the projection, the selection and the layout in step with the item view.
    /// </summary>
    private void OnCollectionViewVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        var index = (int)args.Index;

        switch (args.CollectionChange)
        {
            case CollectionChange.ItemInserted:
                _rowSelection.OnItemsInserted(index, 1);
                _visualRows?.OnItemInserted(index);
                break;

            case CollectionChange.ItemRemoved:
                // The visual index has to be read before the projection is rebuilt, while the run list still
                // describes where the removed row was.
                var removedVisualIndex = _visualRows?.GetVisualIndex(index) ?? index;
                _rowSelection.OnItemsRemoved(index, 1);
                _visualRows?.OnItemRemoved(removedVisualIndex, (args as VectorChangedEventArgs)?.Item);
                break;

            default:
                // A reset re-orders everything, so index-based selection no longer means anything.
                _rowSelection.Clear();
                _visualRows?.Reset();
                break;
        }

        OnItemsChangedForSelection();
    }

    /// <summary>
    /// Gets the header row, once the template has been applied.
    /// </summary>
    internal TableViewHeaderRow? HeaderRow => _headerRow;

    /// <summary>
    /// Gets the height of the rows area that is scrolled out of view.
    /// </summary>
    internal double ScrollableHeight => _scrollViewer?.ScrollableHeight ?? 0d;

    /// <summary>
    /// Gets the width of the cells area that is scrolled out of view.
    /// </summary>
    internal double ScrollableWidth => _scrollViewer?.ScrollableWidth ?? 0d;

    /// <summary>
    /// Gets the height of the visible rows area.
    /// </summary>
    internal double ViewportHeight => _scrollViewer?.ViewportHeight ?? 0d;

    /// <summary>
    /// Gets the width of the visible cells area.
    /// </summary>
    internal double ViewportWidth => _scrollViewer?.ViewportWidth ?? 0d;

    /// <summary>
    /// Scrolls by the given deltas.
    /// </summary>
    internal void ScrollBy(double horizontalDelta, double verticalDelta)
    {
        ScrollTo(HorizontalOffset + horizontalDelta, (_scrollViewer?.VerticalOffset ?? 0d) + verticalDelta);
    }

    /// <summary>
    /// Scrolls to the given offsets.
    /// </summary>
    internal void ScrollTo(double horizontalOffset, double verticalOffset)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        SetValue(HorizontalOffsetProperty, Math.Clamp(horizontalOffset, 0d, _scrollViewer.ScrollableWidth));
        _scrollViewer.ChangeView(null, Math.Clamp(verticalOffset, 0d, _scrollViewer.ScrollableHeight), null, true);
    }

    /// <summary>
    /// Realizes the row for an item index and returns it, or <see langword="null"/> when the item cannot be shown.
    /// </summary>
    /// <remarks>
    /// Used by automation to reach a row that is off screen, which the Grid and ItemContainer patterns require.
    /// </remarks>
    internal TableViewRow? RealizeRow(int itemIndex)
    {
        var visualIndex = GetVisualIndexFromItemIndex(itemIndex);

        return visualIndex < 0 ? null : _rowsRepeater?.GetOrCreateElement(visualIndex) as TableViewRow;
    }

    /// <summary>
    /// Called by the layout after each measure pass with the range of visual rows it realized.
    /// </summary>
    internal void OnRowsRealized(int firstRealizedIndex, int lastRealizedIndex, int rowCount)
    {
        RequestMoreItemsIfNeeded(lastRealizedIndex, rowCount);
    }

    /// <summary>
    /// Asks the items source for more items when the realized range approaches the end of what is loaded.
    /// </summary>
    /// <remarks>
    /// Driven by how close the viewport is to the end, never by the layout asking for the extent, so scrolling a
    /// long way does not pull in the whole source. <see cref="DataFetchSize"/> is read as a multiple of the rows a
    /// viewport holds, matching how it behaved on a virtualizing panel.
    /// </remarks>
    private async void RequestMoreItemsIfNeeded(int lastRealizedIndex, int rowCount)
    {
        if (_isLoadingMoreItems
            || IncrementalLoadingTrigger is IncrementalLoadingTrigger.None
            || !_collectionView.HasMoreItems)
        {
            return;
        }

        var rowsPerViewport = Math.Max(1, CalculateAvailablePageSize());

        // rowCount of 0 means an empty source that has more to give, which needs its first page unconditionally.
        if (rowCount > 0 && lastRealizedIndex >= 0)
        {
            var threshold = (int)Math.Round(IncrementalLoadingThreshold * rowsPerViewport);

            if (lastRealizedIndex < rowCount - 1 - threshold)
            {
                return;
            }
        }

        var count = (uint)Math.Max(1, (int)Math.Round(DataFetchSize * rowsPerViewport));

        _isLoadingMoreItems = true;

        try
        {
            if (_collectionView.LoadMoreItemsAsync(count) is { } operation)
            {
                await operation;
            }
        }
        catch (Exception ex)
        {
            // A failing source must not take the control down; the next viewport change retries.
            TableViewTrace.Write($"TableView: LoadMoreItemsAsync failed: {ex.Message}");
        }
        finally
        {
            _isLoadingMoreItems = false;
        }
    }
}
