#if !WINDOWS
using Microsoft.UI.Xaml;
using System;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Leaves the cells scrolled out of view sideways out of rendering, on renderers that compose them.
/// </summary>
/// <remarks>
/// Uno's Skia renderer draws every realized element on every frame and clips away what is out of view only after it
/// has done the work. A row has a cell for every column, and in a table wider than its viewport most of them are out of
/// view, so while rows scroll most of the work is for cells no one sees. Their visuals are hidden the way the renderer
/// hides a collapsed element, which leaves layout, hit testing and automation as they are.
/// <para>
/// Which cells are in view changes only with layout: the horizontal offset, column widths and the columns themselves
/// all arrange the rows again, and the renderer draws after the layout pass that raised the event. Realized rows above
/// and below the viewport are not hidden: a scroll animation moves them into view on the renderer's own schedule.
/// </para>
/// </remarks>
public partial class TableView
{
    /// <summary>
    /// Shows the cells of the realized rows that are in view, and hides the rest.
    /// </summary>
    private void UpdateRenderCulling()
    {
        if (_scrollViewer is null || ElementVisuals.AreKnownNotComposed)
        {
            return;
        }

        // The viewport's width is set when the scroll viewer handles its own size change, which can come after this
        // layout pass's event; the scroll viewer's width is set by the layout pass itself, and is never narrower.
        var viewportWidth = Math.Max(_scrollViewer.ViewportWidth, _scrollViewer.ActualWidth) - Padding.Left - Padding.Right;

        // A column resize preview moves cells with transforms and no layout, so where they are is not known here.
        var cullCells = !IsColumnResizing && viewportWidth > 0d;

        foreach (var row in _rows)
        {
            row.RowPresenter?.UpdateCellsRenderCulling(viewportWidth, cullCells);
        }
    }

    /// <summary>
    /// Lets the cells out of view take the new row heights: the height they hold is from the old ones.
    /// </summary>
    private void OnRowHeightsChanged(DependencyObject sender, DependencyProperty dp)
    {
        foreach (var row in _rows)
        {
            foreach (var cell in row.Cells)
            {
                cell.ReleaseDeferredHeight();
            }
        }
    }
}
#endif
