using System;

namespace WinUI.TableView;

/// <summary>
/// Keeps cells only for the columns in or near the viewport.
/// </summary>
/// <remarks>
/// A row has a cell, and the header row a header, for every frozen column and for the scrollable columns within
/// <see cref="RealizedColumns"/>.
/// The range follows the horizontal offset, the viewport's width and the column widths; rows bring their cells in
/// line with it when it changes (see <see cref="TableViewRowPresenter.SyncCells"/>).
/// </remarks>
public partial class TableView
{
    // How far past the viewport's edges columns are given cells, and how close to an edge a column without one
    // may come before the range moves. The range then moves at both ends at once, so the cells that leave at one
    // end are there to be reused at the other.
    private const double RealizedColumnsLead = 200d;
    private const double RealizedColumnsMinLead = 40d;

    private (int First, int Last) _realizedColumns;
    private (int First, int Last)? _syncedColumns;
    private (int Version, int Frozen, double Offset, double ViewerOffset, double Width)? _realizedColumnsKey;
    private (int Version, int Frozen)? _realizedColumnsLayout;
    private bool _isLeadPending;
    private bool _hasStaleRows;
    private Microsoft.UI.Xaml.DispatcherTimer? _columnsIdleTimer;

    // How long the view has to rest after a jump before what the jump left out is filled in.
    private static readonly TimeSpan ColumnsIdleDelay = TimeSpan.FromMilliseconds(100);

    private void RestartColumnsIdleTimer()
    {
        if (_columnsIdleTimer is null)
        {
            _columnsIdleTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = ColumnsIdleDelay };
            _columnsIdleTimer.Tick += (_, _) => CompleteColumnJump();
        }

        _columnsIdleTimer.Stop();
        _columnsIdleTimer.Start();
    }

    /// <summary>
    /// Fills in what a jump across many columns left out: the columns just outside the view, and the cells of the
    /// rows that are realized above and below it.
    /// </summary>
    internal void CompleteColumnJump()
    {
        _columnsIdleTimer?.Stop();

        if (_realizedColumnsLayout is not null && _realizedColumns.Last >= _realizedColumns.First)
        {
            _isLeadPending = true;
            _realizedColumnsKey = null;
        }

        SyncStaleRows();
        UpdateRealizedColumns();
    }

    /// <summary>
    /// Brings the cells of the rows a jump left out in line with the columns in view.
    /// </summary>
    internal void SyncStaleRows()
    {
        if (!_hasStaleRows)
        {
            return;
        }

        _hasStaleRows = false;

        foreach (var row in _rows)
        {
            if (row.HasStaleCells)
            {
                row.SyncCells();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether any of a row is inside the viewport.
    /// </summary>
    private bool IsRowInView(TableViewRow row)
    {
        if (_scrollViewer is not { ViewportHeight: > 0d } scrollViewer
            || !RealizedRows.TryGetBounds(row.VisualIndex, out var top, out var height))
        {
            return true;
        }

        var viewportTop = RowsViewportTop;

        return top + height > viewportTop && top < viewportTop + scrollViewer.ViewportHeight;
    }
    private bool _isResizePreviewActive;
    private double _resizePreviewLead;

    /// <summary>
    /// Gets the first and last visible-column index of the scrollable columns that have cells.
    /// </summary>
    internal (int First, int Last) RealizedColumns
    {
        get
        {
            var columns = (TableViewColumnsCollection)Columns;
            var count = columns.VisibleColumns.Count;
            var frozen = Math.Clamp(FrozenColumnCount, 0, count);
            var width = _scrollViewer?.ViewportWidth is > 0d and var viewport ? viewport
                      : ActualWidth > 0d ? ActualWidth
                      : XamlRoot?.Size.Width is > 0d and var root ? root
                      : UnknownViewportWidth;

            // Uno's scroll viewer can also pan sideways by itself.
            var viewerOffset = _scrollViewer?.HorizontalOffset ?? 0d;
            width += _resizePreviewLead;
            var key = (columns.LayoutVersion, frozen, HorizontalOffset, viewerOffset, width);

            if (_realizedColumnsKey != key)
            {
                _realizedColumnsKey = key;

                var end = HorizontalOffset + viewerOffset + width;
                var full = columns.FindVisibleColumns(frozen, HorizontalOffset - RealizedColumnsLead, end + RealizedColumnsLead);
                var needed = columns.FindVisibleColumns(frozen, HorizontalOffset - RealizedColumnsMinLead, end + RealizedColumnsMinLead);
                var layout = (columns.LayoutVersion, frozen);
                var current = _realizedColumns;

                // The range stays while it covers what is needed and is no longer than a new one would be.
                if (_realizedColumnsLayout != layout
                    || _isLeadPending
                    || current.First > needed.First
                    || current.Last < needed.Last
                    || current.Last - current.First > full.Last - full.First + 1)
                {
                    // A jump that replaces most columns takes only those in view, which is every cell of every
                    // row on screen already; the columns either side follow once the view rests.
                    var isJump = _realizedColumnsLayout == layout && !_isLeadPending && IsFarFrom(current, full);

                    _isLeadPending = false;
                    _realizedColumnsLayout = layout;
                    _realizedColumns = isJump ? needed : full;

                    if (isJump)
                    {
                        RestartColumnsIdleTimer();
                    }
                }
            }

            return _realizedColumns;
        }
    }

    private const double UnknownViewportWidth = 2000d;

    /// <summary>
    /// Brings the headers and the cells of the realized rows in line with <see cref="RealizedColumns"/> when the
    /// range has changed.
    /// </summary>
    /// <remarks>
    /// Dragging the scroll bar's thumb can move the view by many columns between two frames, which gives every
    /// cell of every row another column. Where that has proved too slow to do on every frame, as in a large window
    /// with many rows, the headers follow such moves and the cells follow a few times a second, and as soon as the
    /// thumb slows down, rests or is let go. A drag that moves a few columns at a time is never held back.
    /// </remarks>
    /// <param name="flush">Whether to sync the cells now even while the thumb is being dragged.</param>
    internal void UpdateRealizedColumns(bool flush = false)
    {
        // A resize preview moves cells with transforms; they are synced once it ends.
        if (_isResizePreviewActive)
        {
            return;
        }

        var range = RealizedColumns;

        if (_syncedHeaderColumns != range)
        {
            _syncedHeaderColumns = range;
            _headerRow?.SyncHeaders();
        }

        if (_syncedColumns == range)
        {
            _deferredColumns = null;
            _deferredColumnsTimer?.Stop();
            return;
        }

        var isFar = _syncedColumns is { } synced && IsFarFrom(synced, range);

        if (!flush
            && IsHorizontalThumbTracking
            && FarCellsSyncCost > FarCellsSyncBudget
            && Environment.TickCount64 - _lastCellsSync < MaxCellsDeferral.TotalMilliseconds)
        {
            // Nothing has moved since the cells were held back.
            if (_deferredColumns == range)
            {
                return;
            }

            // Held back only when this one move of the thumb replaced most of the columns.
            if ((_deferredColumns ?? _syncedColumns) is { } previous && IsFarFrom(previous, range))
            {
                _deferredColumns = range;
                _deferredColumnsTimer ??= CreateDeferredColumnsTimer();
                _deferredColumnsTimer.Stop();
                _deferredColumnsTimer.Start();

                return;
            }
        }

        _deferredColumns = null;
        _deferredColumnsTimer?.Stop();
        _syncedColumns = range;

        var started = System.Diagnostics.Stopwatch.GetTimestamp();

        foreach (var row in _rows)
        {
            // A jump gives every cell another column, so the rows kept above and below the view wait.
            if (isFar && !IsRowInView(row))
            {
                row.HasStaleCells = true;
                _hasStaleRows = true;
            }
            else
            {
                row.SyncCells();
            }
        }

        if (_hasStaleRows)
        {
            RestartColumnsIdleTimer();
        }

        _lastCellsSync = Environment.TickCount64;

        if (isFar)
        {
            FarCellsSyncCost = System.Diagnostics.Stopwatch.GetElapsedTime(started);
        }
    }

    // What giving every cell another column may cost before it is no longer done on every frame of a drag. Laying
    // the cells out afterwards takes about as long again.
    private static readonly TimeSpan FarCellsSyncBudget = TimeSpan.FromMilliseconds(25);

    /// <summary>
    /// Gets or sets how long it last took to give most cells of the realized rows another column.
    /// </summary>
    internal TimeSpan FarCellsSyncCost { get; set; }

    private (int First, int Last)? _syncedHeaderColumns;
    private (int First, int Last)? _deferredColumns;
    private Microsoft.UI.Xaml.DispatcherTimer? _deferredColumnsTimer;

    /// <summary>
    /// Gets or sets a value indicating whether the horizontal scroll bar's thumb is being dragged.
    /// </summary>
    internal bool IsHorizontalThumbTracking { get; set; }

    /// <summary>
    /// Gets a value indicating whether the rows show cells for the columns in view, which they do not while a far
    /// drag of the scroll bar's thumb is under way.
    /// </summary>
    internal bool AreCellsDeferred => _deferredColumns is not null;

    // How long the thumb has to rest before the cells follow it.
    private static readonly TimeSpan DeferredColumnsDelay = TimeSpan.FromMilliseconds(40);

    private long _lastCellsSync;

    /// <summary>
    /// Gets or sets the longest the cells are held back while the thumb keeps moving far.
    /// </summary>
    internal TimeSpan MaxCellsDeferral { get; set; } = TimeSpan.FromMilliseconds(120);

    private Microsoft.UI.Xaml.DispatcherTimer CreateDeferredColumnsTimer()
    {
        var timer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = DeferredColumnsDelay };
        timer.Tick += (_, _) => UpdateRealizedColumns(flush: true);

        return timer;
    }

    /// <summary>
    /// Gets a value indicating whether less than half of a range's columns are also in another.
    /// </summary>
    private static bool IsFarFrom((int First, int Last) synced, (int First, int Last) range)
    {
        var shared = Math.Min(synced.Last, range.Last) - Math.Max(synced.First, range.First) + 1;

        return shared * 2 < range.Last - range.First + 1;
    }
}
