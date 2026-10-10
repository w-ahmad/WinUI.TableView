using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    private readonly List<TableViewGroupHeaderRow> _groupHeaderRows = [];
    private bool _isLoadingMoreItems;

    /// <summary>
    /// Gets the items produced by the current sorting and filtering.
    /// </summary>
    /// <remarks>
    /// Positions in this collection are what <see cref="TableViewCellSlot.Row"/>, <see cref="SelectedRanges"/>,
    /// the clipboard and automation all mean by "row".
    /// </remarks>
    public IList<object> Items => _collectionView;

    /// <summary>
    /// Gets the currently realized row containers.
    /// </summary>
    internal IReadOnlyList<TableViewRow> Rows => _rows;

    /// <summary>
    /// Gets the number of visual rows.
    /// </summary>
    internal int VisualRowCount => _visualRows?.Count ?? 0;

    /// <summary>
    /// Gets the currently realized group header rows.
    /// </summary>
    internal IReadOnlyList<TableViewGroupHeaderRow> GroupHeaderRows => _groupHeaderRows;

    /// <summary>
    /// Returns the realized rows and group headers in the order they are displayed.
    /// </summary>
    internal List<UIElement> GetRealizedRowElements()
    {
        var elements = new List<(int VisualIndex, UIElement Element)>(_rows.Count + _groupHeaderRows.Count);

        foreach (var row in _rows)
        {
            elements.Add((row.VisualIndex, row));
        }

        foreach (var header in _groupHeaderRows)
        {
            elements.Add((_rowsRepeater?.GetElementIndex(header) ?? -1, header));
        }

        elements.RemoveAll(x => x.VisualIndex < 0);
        elements.Sort((x, y) => x.VisualIndex.CompareTo(y.VisualIndex));

        return elements.ConvertAll(x => x.Element);
    }

    /// <summary>
    /// Gets a value indicating whether a group header should currently be pinned to the top of the viewport.
    /// </summary>
    internal bool AreStickyGroupHeadersActive => AreStickyGroupHeadersEnabled && _visualRows?.HasGroups is true;

    /// <summary>
    /// Gets the top of the viewport in the row host's coordinates, which start below the template's padding.
    /// </summary>
    internal double RowsViewportTop => (_scrollViewer?.VerticalOffset ?? 0d) - Padding.Top;

    /// <summary>
    /// Returns the visual index of the group header a visual row belongs to, or -1 when it belongs to none.
    /// </summary>
    internal int FindGroupHeaderAtOrBefore(int visualIndex) => _visualRows?.FindHeaderAtOrBefore(visualIndex) ?? -1;

    /// <summary>
    /// Returns the visual index of the next group header after a visual row, or -1 when there is none.
    /// </summary>
    internal int FindGroupHeaderAfter(int visualIndex) => _visualRows?.FindHeaderAfter(visualIndex) ?? -1;

    /// <summary>
    /// Returns the header template for a group, from the <see cref="GroupStyle"/> entry for its nesting level, or
    /// from <see cref="DefaultGroupStyle"/> when <see cref="GroupStyle"/> is empty.
    /// </summary>
    internal DataTemplate? GetGroupHeaderTemplate(TableViewGroupInfo groupInfo, DependencyObject container)
    {
        var style = _groupStyle.Count > 0
            ? _groupStyle[Math.Clamp(groupInfo.Level, 0, _groupStyle.Count - 1)]
            : DefaultGroupStyle;

        return style?.HeaderTemplateSelector?.SelectTemplate(groupInfo, container) ?? style?.HeaderTemplate;
    }

    /// <summary>
    /// Re-resolves the header template of every realized group header, after <see cref="GroupStyle"/> or
    /// <see cref="DefaultGroupStyle"/> changed.
    /// </summary>
    private void RefreshGroupHeaderTemplates()
    {
        foreach (var header in _groupHeaderRows)
        {
            if (header.GroupInfo is { } groupInfo)
            {
                header.ContentTemplate = GetGroupHeaderTemplate(groupInfo, header);
            }
        }
    }

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
    /// Converts an item index to the visual row index showing it.
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
    /// Converts a visual row index to the item index it shows.
    /// </summary>
    internal int GetItemIndexFromVisualIndex(int visualIndex)
    {
        return _visualRows?.GetItemIndex(visualIndex) ?? visualIndex;
    }

    /// <summary>
    /// Creates a row, wired to this table.
    /// </summary>
    internal TableViewRow CreateRow()
    {
        return new TableViewRow { TableView = this };
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
        _visualRows = new TableViewVisualRows(_collectionView);

        // Half a viewport of rows is kept realized above and below the visible ones. Touch and touchpad panning
        // keep moving the content while the UI thread is busy, during a garbage collection for instance, and the
        // rows realized ahead are what stays on screen meanwhile. The repeater's default of a whole viewport each
        // side would double the live rows, and every live row adds to what each garbage collection has to walk.
        repeater.HorizontalCacheLength = 0d;
        repeater.VerticalCacheLength = 1d;
        repeater.Layout = _rowsLayout;
        repeater.ItemTemplate = _rowElementFactory.Shim;
        repeater.ItemsSource = _visualRows;

        repeater.ElementPrepared += OnRowElementPrepared;
        repeater.ElementClearing += OnRowElementClearing;
        repeater.ElementIndexChanged += OnRowElementIndexChanged;
    }

    /// <summary>
    /// Takes the layout off the repeater while the table is unloaded.
    /// </summary>
    /// <remarks>
    /// An unloaded table can be collected, and the repeater can call into its layout after that; the layout is a .NET
    /// object that went with the table, so the call fails inside the interop layer. Clearing the layout here makes the
    /// repeater uninitialize it and recycle its rows while everything is still alive, and leaves it nothing to call
    /// later. <see cref="ResumeRowsHost"/> puts it back when the table is loaded again.
    /// </remarks>
    private void SuspendRowsHost()
    {
        if (_rowsRepeater is not null)
        {
            _rowsRepeater.Layout = null;
        }
    }

    /// <summary>
    /// Gives the repeater its layout back when the table is loaded again.
    /// </summary>
    private void ResumeRowsHost()
    {
        if (_rowsRepeater is not null && _rowsLayout is not null && _rowsRepeater.Layout is null)
        {
            _rowsRepeater.Layout = _rowsLayout;
        }
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
        _groupHeaderRows.Clear();
    }

    /// <summary>
    /// Applies the table's state to a row as it becomes visible. Everything a row needs to look right is pushed
    /// here rather than pulled per row from the whole table, which is what keeps realization proportional to the
    /// viewport.
    /// </summary>
    private void OnRowElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is TableViewRow row)
        {
            AttachRow(row, args.Index);
        }
        else if (args.Element is TableViewGroupHeaderRow header)
        {
            AttachGroupHeader(header);
        }
    }

    /// <summary>
    /// Releases a row as it leaves the realized range.
    /// </summary>
    private void OnRowElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is TableViewGroupHeaderRow header)
        {
            _groupHeaderRows.Remove(header);
            _rowsLayout?.OnElementCleared(header);

            return;
        }

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
        if (args.Element is TableViewRow row)
        {
            AttachRow(row, args.NewIndex);
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
        row.RowPresenter?.SetGroupIndent();
        row.ApplyCellsSelectionState();

        if (CurrentCellSlot.HasValue)
        {
            row.ApplyCurrentCellState(CurrentCellSlot.Value);
        }
    }

    /// <summary>
    /// Brings a group header's table-driven appearance up to date as it becomes visible.
    /// </summary>
    private void AttachGroupHeader(TableViewGroupHeaderRow header)
    {
        header.TableView = this;

        if (!_groupHeaderRows.Contains(header))
        {
            _groupHeaderRows.Add(header);
        }

        header.EnsureGridLines();
        header.UpdateExpandCollapseButtonVisibility();
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
                // The visual index has to be read before the projection is rebuilt, while it still describes
                // where the removed row was.
                var removedVisualIndex = _visualRows?.GetVisualIndex(index) ?? index;
                _rowSelection.OnItemsRemoved(index, 1);
                _visualRows?.OnItemRemoved(removedVisualIndex, (args as VectorChangedEventArgs)?.Item);
                break;

            default:
                // A reset re-orders everything, so index-based selection no longer means anything. It was already
                // cleared, with notifications, by OnCollectionViewResetting while the old indexes still held; this
                // only guards against a reset that did not announce itself first.
                if (_rowSelection.Clear())
                {
                    SyncSelectionProperties();
                }

                _visualRows?.Reset();
                break;
        }

        OnItemsChangedForSelection();
    }

    /// <summary>
    /// Clears the selection just before the item view rebuilds itself, while the selected indexes still refer to
    /// the items they were selected on.
    /// </summary>
    /// <remarks>
    /// A rebuild (sorting, regrouping, expanding or collapsing a group, replacing the source) re-orders every
    /// index, so an index-based selection cannot survive it. Clearing it here rather than after the fact is what
    /// lets <see cref="SelectionChanged"/> report the right removed items and keeps <see cref="SelectedItem"/> and
    /// <see cref="CurrentCellSlot"/> from pointing at whatever happens to land on the old index.
    /// </remarks>
    private void OnCollectionViewResetting(object? sender, EventArgs e)
    {
        DeselectAllBeforeViewReset();
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
    /// Called by the layout after each measure pass with the last visual row it realized and the visual row count.
    /// </summary>
    internal void OnRowsRealized(int lastRealizedIndex, int rowCount)
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
