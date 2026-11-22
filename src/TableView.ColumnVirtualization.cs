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
    // How far past the viewport's edges columns keep their cells.
    private const double RealizedColumnsLead = 200d;

    private (int First, int Last) _realizedColumns;
    private (int First, int Last)? _syncedColumns;
    private (int Version, int Frozen, double Offset, double ViewerOffset, double Width)? _realizedColumnsKey;
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
                _realizedColumns = columns.FindVisibleColumns(
                    frozen,
                    HorizontalOffset - RealizedColumnsLead,
                    HorizontalOffset + viewerOffset + width + RealizedColumnsLead);
            }

            return _realizedColumns;
        }
    }

    private const double UnknownViewportWidth = 2000d;

    /// <summary>
    /// Brings the cells of the realized rows in line with <see cref="RealizedColumns"/> when the range has changed.
    /// </summary>
    internal void UpdateRealizedColumns()
    {
        // A resize preview moves cells with transforms; they are synced once it ends.
        if (_isResizePreviewActive)
        {
            return;
        }

        var range = RealizedColumns;

        if (_syncedColumns == range)
        {
            return;
        }

        _syncedColumns = range;
        _headerRow?.SyncHeaders();

        foreach (var row in _rows)
        {
            row.SyncCells();
        }
    }
}
