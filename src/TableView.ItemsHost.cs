using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation.Collections;
using WinUI.TableView.Helpers;
using WinUI.TableView.Layout;

namespace WinUI.TableView;

/// <summary>
/// Row hosting for <see cref="TableView"/>: the <see cref="ItemsRepeater"/> with the stock <see cref="StackLayout"/>,
/// and the mapping between item indexes and visual row indexes.
/// </summary>
public partial class TableView
{
    private ItemsRepeater? _rowsRepeater;
    private TableViewRowElementFactory? _rowElementFactory;
    private TableViewVisualRows? _visualRows;
    private readonly List<TableViewGroupHeaderRow> _groupHeaderRows = [];
    private bool _isLoadingMoreItems;
    private bool _isIncrementalLoadQueued;
    private TableViewRealizedRows? _realizedRows;
    private int _selectionModeVersion;
    private double _suspendedVerticalOffset;
    private (double Height, double MinHeight, double MaxHeight)? _rowHeights;

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
        return [.. RealizedRows.InOrder];
    }

    /// <summary>
    /// Gets the realized rows and group headers in visual order, and where each one is.
    /// </summary>
    internal TableViewRealizedRows RealizedRows => _realizedRows ??= new(GetRealizedEntries);

    private IEnumerable<(int VisualIndex, UIElement Element)> GetRealizedEntries()
    {
        foreach (var row in _rows)
        {
            yield return (row.VisualIndex, row);
        }

        foreach (var header in _groupHeaderRows)
        {
            yield return (header.VisualIndex, header);
        }
    }

    /// <summary>
    /// Gets a value indicating whether a group header should currently be pinned to the top of the viewport.
    /// </summary>
    internal bool AreStickyGroupHeadersActive => AreStickyGroupHeadersEnabled && _visualRows?.HasGroups is true;

    /// <summary>
    /// Gets the factory that creates and pools the row host's rows.
    /// </summary>
    internal TableViewRowElementFactory? RowElementFactory => _rowElementFactory;

    /// <summary>
    /// Gets the top of the viewport in the row host's coordinates, which start below the template's padding.
    /// </summary>
    internal double RowsViewportTop => (_scrollViewer?.VerticalOffset ?? 0d) - Padding.Top;

    /// <summary>
    /// Gets the width of the viewport inside the table's padding.
    /// </summary>
    internal double RowsViewportWidth => Math.Max(0d, (_scrollViewer?.ViewportWidth ?? 0d) - Padding.Left - Padding.Right);

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

        if (_stickyGroupHeader?.GroupInfo is { } stickyGroup)
        {
            _stickyGroupHeader.ContentTemplate = GetGroupHeaderTemplate(stickyGroup, _stickyGroupHeader);
        }
    }

    /// <summary>
    /// Returns the realized row for an item index, or <see langword="null"/> when it is not realized.
    /// </summary>
    /// <param name="index">The item index.</param>
    public DependencyObject? ContainerFromIndex(int index)
    {
        var visualIndex = GetVisualIndexFromItemIndex(index);

        return visualIndex < 0 ? null : RealizedRows.Get(visualIndex);
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
    /// Occurs when <see cref="RowHeight"/>, <see cref="RowMinHeight"/> or <see cref="RowMaxHeight"/> changes.
    /// </summary>
    internal event Action? RowHeightsChanged;

    /// <summary>
    /// Applies <see cref="RowHeight"/>, <see cref="RowMinHeight"/> and <see cref="RowMaxHeight"/> to a cell, a row
    /// header or another element that is as tall as a row's cells.
    /// </summary>
    internal void ApplyRowHeights(FrameworkElement element)
    {
        var (height, minHeight, maxHeight) = _rowHeights ??= (RowHeight, RowMinHeight, RowMaxHeight);

        element.Height = height;
        element.MinHeight = minHeight;
        element.MaxHeight = maxHeight;
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
        _rowElementFactory = new TableViewRowElementFactory(this);
        _visualRows = new TableViewVisualRows(_collectionView);

        // About a viewport of rows is kept realized above and below the visible ones, the repeater's default and
        // the platform's own table's choice. Touch and touchpad panning keep moving the content while the UI thread
        // is busy, during a garbage collection for instance, and the rows realized ahead are what stays on screen
        // meanwhile; a fling outruns half a viewport within a few frames. Scrolling realizes rows at the rate they
        // come into view whatever the cache, so the cost of the larger cache is only the rows it keeps alive.
        //
        // A browser creates the extra cached rows on its UI thread as the viewport starts moving. Even with AOT,
        // growing that pool causes first-scroll stalls. Half a viewport of cache in total limits this work while
        // keeping rows ahead of the viewport; steady scrolling still reuses rows at the same rate.
        repeater.HorizontalCacheLength = 0d;
#if WINDOWS
        // Windows shows the browser's first-scroll pattern too - frames of 70-140 ms while the pool grows as
        // scrolling starts - and adds one of its own: the reference-tracker walk of an induced gen2 GC suspends
        // the process for as long as there are live XAML objects to walk, and the realized rows are most of them.
        // One viewport of cache halves the rows both costs are paid on, while still keeping half a viewport of
        // realized rows ahead of a fling for the frames the UI thread is busy.
        repeater.VerticalCacheLength = 1d;
#else
        repeater.VerticalCacheLength = OperatingSystem.IsBrowser() ? 0.5d : 2d;
#endif

        // The platform's own virtualizing layout realizes from an anchor, estimates unrealized row heights and
        // recycles rows it leaves behind. On Uno its managed layout methods also need coverage in the AOT profile.
        repeater.Layout = new StackLayout();
        repeater.ItemTemplate = _rowElementFactory.Shim;
        repeater.ItemsSource = _visualRows;

        repeater.ElementPrepared += OnRowElementPrepared;
#if WINDOWS
        repeater.KeyDown += OnRowsHostKeyDown;
#endif

        ConsiderLoadingMoreItems(-1);
        repeater.ElementClearing += OnRowElementClearing;
        repeater.ElementIndexChanged += OnRowElementIndexChanged;
        repeater.LayoutUpdated += OnRowsRepeaterLayoutUpdated;

        UpdateRowsMinWidth();
    }

    /// <summary>
    /// Keeps the rows at least as wide as the row header and the visible columns.
    /// </summary>
    /// <remarks>
    /// The stack layout is as wide as the widest row it has realized, so without this floor the horizontal extent
    /// would depend on which rows happen to be realized, and a table without rows could not scroll its column headers.
    /// </remarks>
    private void UpdateRowsMinWidth()
    {
        if (_rowsRepeater is null)
        {
            return;
        }

        // The offset is NaN while the header row's corner is left to take the width it asks for.
        var width = double.IsNaN(CellsHorizontalOffset) ? 0d : CellsHorizontalOffset;

        foreach (var column in Columns.VisibleColumns)
        {
            if (column.ActualWidth is > 0d and var columnWidth)
            {
                width += columnWidth;
            }
        }

        if (_rowsRepeater.MinWidth != width)
        {
            _rowsRepeater.MinWidth = width;
        }
    }

    /// <summary>
    /// Takes the items off the repeater while the table is unloaded.
    /// </summary>
    /// <remarks>
    /// An unloaded table can be collected, and the repeater can call into the .NET objects it was given after that,
    /// such as the items source it unsubscribes from or the factory it hands its rows back to; the call then fails
    /// inside the interop layer. Clearing the items here recycles every row while everything is still alive, the way
    /// the platform's own table lets go of its rows. <see cref="ResumeRowsHost"/> gives them back when the table is
    /// loaded again.
    /// </remarks>
    private void SuspendRowsHost()
    {
        if (_rowsRepeater is { ItemsSource: not null })
        {
            // Taking the items away resets the scroll position; ResumeRowsHost puts it back.
            _suspendedVerticalOffset = _scrollViewer?.VerticalOffset ?? 0d;
            _rowsRepeater.ItemsSource = null;
        }

        // Clearing the items pooled every row; outside this callback, let them drop the items they showed.
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (!IsLoaded)
            {
                _rowElementFactory?.ReleasePooledContent();
            }
        });
    }

    /// <summary>
    /// Gives the repeater its items back when the table is loaded again.
    /// </summary>
    private void ResumeRowsHost()
    {
        if (_rowsRepeater is not null && _visualRows is not null && _rowsRepeater.ItemsSource is null)
        {
            _rowsRepeater.ItemsSource = _visualRows;

            if (_suspendedVerticalOffset > 0d && _scrollViewer is not null)
            {
                UpdateLayout(); // the extent is not known until the rows are laid out
                _scrollViewer.ChangeView(null, _suspendedVerticalOffset, null, true);
            }
        }

        _suspendedVerticalOffset = 0d;
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
#if WINDOWS
        _rowsRepeater.KeyDown -= OnRowsHostKeyDown;
#endif
        _rowsRepeater.LayoutUpdated -= OnRowsRepeaterLayoutUpdated;
        _rowsRepeater.ElementClearing -= OnRowElementClearing;
        _rowsRepeater.ElementIndexChanged -= OnRowElementIndexChanged;
        _rowsRepeater.ItemsSource = null;
        _rowsRepeater.Layout = null;
        _rowsRepeater.ItemTemplate = null;
        _rowsRepeater = null;
        _rowElementFactory = null;
        _visualRows = null;
        foreach (var row in _rows)
        {
            row.IsAttached = false;
        }

        _rows.Clear();
        _realizedRows?.Invalidate();
        _groupHeaderRows.Clear();
    }

    /// <summary>
    /// Applies the table's state to a row as it becomes visible. Everything a row needs to look right is pushed
    /// here rather than pulled per row from the whole table, which is what keeps realization proportional to the
    /// viewport.
    /// </summary>
    private void OnRowElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        _realizedRows?.Invalidate();

        if (args.Element is TableViewRow row)
        {
            AttachRow(row, args.Index);
            ConsiderLoadingMoreItems(args.Index);
        }
        else if (args.Element is TableViewGroupHeaderRow header)
        {
            header.VisualIndex = args.Index;
            AttachGroupHeader(header);
            ConsiderLoadingMoreItems(args.Index); // the last rows can be the headers of collapsed groups
        }
    }

    /// <summary>
    /// Moves the sticky group header after the rows have been laid out, since the group at the top can change without
    /// a scroll, such as when one is expanded or collapsed. Cells can have moved into or out of view sideways too.
    /// </summary>
    private void OnRowsRepeaterLayoutUpdated(object? sender, object e)
    {
        if (AreStickyGroupHeadersActive || _stickyGroupHeader?.Visibility is Visibility.Visible)
        {
            UpdateStickyGroupHeader();
        }

#if !WINDOWS
        UpdateRenderCulling(onlyIfStale: true);
#endif
    }

    /// <summary>
    /// Releases a row as it leaves the realized range.
    /// </summary>
    private void OnRowElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        _realizedRows?.Invalidate();

        if (args.Element is TableViewGroupHeaderRow header)
        {
            _groupHeaderRows.Remove(header);
            header.VisualIndex = -1;

            return;
        }

        if (args.Element is not TableViewRow row)
        {
            return;
        }

        // A row being recycled mid-edit would silently drop the edit and leave the table stuck in editing mode,
        // so commit it while the cell still exists.
        EndEditOfReleasedRow(row);

        _rows.Remove(row);
        row.IsAttached = false;
        row.VisualIndex = -1;
        row.Index = -1;
    }

    /// <summary>
    /// Re-indexes a realized row after an insertion or removal shifted it.
    /// </summary>
    private void OnRowElementIndexChanged(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
    {
        _realizedRows?.Invalidate();

        if (args.Element is TableViewRow row)
        {
            AttachRow(row, args.NewIndex);
        }
        else if (args.Element is TableViewGroupHeaderRow header)
        {
            header.VisualIndex = args.NewIndex;
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
        row.SetSelectionState(itemIndex >= 0 && _rowSelection.Contains(itemIndex), animate: false);
#if !WINDOWS
        InvalidateRenderCulling();
#endif

        if (!row.IsAttached)
        {
            row.IsAttached = true;
            _rows.Add(row);
        }

        // A new row has no template yet; its presenter applies the visuals once it has one.
        if (row.RowPresenter?.IsTemplateApplied is true)
        {
            ApplyRowVisuals(row);
        }
    }

    /// <summary>
    /// Brings a row's per-item visuals up to date: cell styles, alternate colors, details pane, group indent,
    /// template selectors and cell states.
    /// </summary>
    /// <remarks>
    /// Runs while the repeater prepares the row, inside a layout pass, so it never moves focus.
    /// </remarks>
    internal void ApplyRowVisuals(TableViewRow row)
    {
        row.EnsureCellsStyle(default, row.Content);
        row.EnsureAlternateColors();
        row.RowPresenter?.ApplyDetailsPaneState(row.Content);
        row.RowPresenter?.SetGroupIndent();
        row.RowPresenter?.SetRowNumber();
        row.RowPresenter?.SyncRowHeaderWidth();

        if (RowHeaderTemplateSelector is not null)
        {
            row.RowPresenter?.SetRowHeaderTemplate();
        }

        if (RowDetailsTemplateSelector is not null)
        {
            row.RowPresenter?.SetRowDetailsTemplate();
        }

        // A pooled row missed any change of selection mode, and still shows the pointer state it was released in.
        if (row.SelectionModeVersion != _selectionModeVersion)
        {
            ApplySelectionModeLayout(row);
        }

        row.RefreshVisualStates();
        row.ApplyCellStates();
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
    /// Ends the open edit because its row is about to be released: commits it, or cancels it when a handler
    /// refuses the commit, and leaves edit mode either way.
    /// </summary>
    /// <param name="row">The row being released, or <see langword="null"/> when every row is.</param>
    private void EndEditOfReleasedRow(TableViewRow? row = null)
    {
        if (!IsEditing || _editingCell is not { } cell || (row is not null && !ReferenceEquals(cell.Row, row)))
        {
            return;
        }

        // A commit can re-sort the item; the view must not change mid-layout.
        _collectionView.DeferItemChanges();

        try
        {
            if (!EndCellEditing(TableViewEditAction.Commit, cell))
            {
                cell.EndEditing(TableViewEditAction.Cancel);
            }
        }
        finally
        {
            SetIsEditing(false);
            DispatcherQueue?.TryEnqueue(_collectionView.ApplyDeferredItemChanges);
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

                // The selected rows may have moved down. A reattach settles the selection once, at its end.
                if (_rowSelection.Count > 0 && !_isReattachingItemsSource)
                {
                    SyncSelectionProperties();
                }

                break;

            case CollectionChange.ItemRemoved:
                // The visual index has to be read before the projection is rebuilt, while it still describes
                // where the removed row was.
                var removedVisualIndex = _visualRows?.GetVisualIndex(index) ?? index;
                var removedItem = (args as VectorChangedEventArgs)?.Item;
                var wasSelected = _rowSelection.OnItemsRemoved(index, 1);
                _visualRows?.OnItemRemoved(removedVisualIndex, removedItem);

                if (_isReattachingItemsSource)
                {
                    // The item may only have moved; SettleSelectionAfterReattach finds out.
                    if (wasSelected && removedItem is not null)
                    {
                        (_selectedItemsRemovedWhileReattaching ??= []).Add(removedItem);
                    }
                }
                else if (wasSelected)
                {
                    OnSelectedItemRemoved(removedItem);
                }
                else if (_rowSelection.Count > 0)
                {
                    SyncSelectionProperties();
                }

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

                // A group expanded or collapsed keeps its header object, so the sticky one is not shown anew.
                _stickyGroupHeader?.RefreshGroupVisuals();
                break;
        }

        OnItemsChangedForSelection();

        if (_collectionView.Count is 0)
        {
            // With no rows to measure, the header row's corner takes the width it asks for.
            _headerRow?.SetCellsHorizontalOffset();
            UpdateHorizontalScrollBarMargin();

            // An empty source realizes no rows to ask for its first page.
            ConsiderLoadingMoreItems(-1);
        }
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
        // Every row is about to show another item or none, so an open edit ends here, while its item is known.
        EndEditOfReleasedRow();

        // A reload that has to reset the view selects the items still in it again afterwards.
        if (_isReattachingItemsSource && _rowSelection.Count > 0)
        {
            _selectionBeforeReattachReset = [.. SelectedItems];
        }

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

        if (visualIndex < 0 || !IsRowsHostActive)
        {
            return null;
        }

        if (_rowsRepeater.GetOrCreateElement(visualIndex) is not TableViewRow row)
        {
            return null;
        }

        // A row created for this call, rather than taken from the pool, has no template and so no cells until it
        // is laid out, and the repeater holds it unlaid out where the layout estimates it to be.
        if (row.RowPresenter is null)
        {
            row.UpdateLayout();
        }

        return row;
    }

    /// <summary>
    /// Gets a value indicating whether the repeater has its items, which it does not while the table is unloaded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_rowsRepeater))]
    private bool IsRowsHostActive => _rowsRepeater is { ItemsSource: not null };

    /// <summary>
    /// Queues a request for more items when a realized row is close enough to the end of what is loaded.
    /// </summary>
    /// <param name="realizedIndex">The visual index of a row just realized, or -1 to only check an empty source.</param>
    /// <remarks>
    /// Driven by how close the realized rows are to the end, never by the extent, so scrolling a long way does not pull
    /// in the whole source. Called while the repeater prepares rows, inside a layout pass, so the request itself is
    /// made later: a source that adds items at once would otherwise change the collection in the middle of a measure.
    /// </remarks>
    private void ConsiderLoadingMoreItems(int realizedIndex)
    {
        // Nothing is shown, so nothing is close to the end, until the table is loaded with its row host.
        if (_isIncrementalLoadQueued
            || _isLoadingMoreItems
            || _rowsRepeater is null
            || !IsLoaded
            || IncrementalLoadingTrigger is IncrementalLoadingTrigger.None
            || !_collectionView.HasMoreItems)
        {
            return;
        }

        // A table showing nothing, not even a group header, needs its first page unconditionally.
        if (VisualRowCount > 0)
        {
            var threshold = (int)Math.Round(IncrementalLoadingThreshold * Math.Max(1, CalculateAvailablePageSize()));

            if (realizedIndex < VisualRowCount - 1 - threshold)
            {
                return;
            }
        }

        _isIncrementalLoadQueued = true;
        DispatcherQueue?.TryEnqueue(() =>
        {
            _isIncrementalLoadQueued = false;
            LoadMoreItems();
        });
    }

    /// <summary>
    /// Asks the items source for more items.
    /// </summary>
    /// <remarks>
    /// <see cref="DataFetchSize"/> is read as a multiple of the rows a viewport holds, matching how it behaved on a
    /// virtualizing panel.
    /// </remarks>
    private async void LoadMoreItems()
    {
        if (_isLoadingMoreItems
            || IncrementalLoadingTrigger is IncrementalLoadingTrigger.None
            || !_collectionView.HasMoreItems)
        {
            return;
        }

        var count = (uint)Math.Max(1, (int)Math.Round(DataFetchSize * Math.Max(1, CalculateAvailablePageSize())));

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

        // Rows realized while this load ran did not ask for more; ask on their behalf.
        ConsiderLoadingMoreItems(RealizedRows.LastVisualIndex);
    }
}
