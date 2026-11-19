using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;
using WinUI.TableView.Selection;
using Pointer = Microsoft.UI.Xaml.Input.Pointer;

namespace WinUI.TableView;

/// <summary>
/// Represents a control that displays data in customizable table-like interface.
/// </summary>
[StyleTypedProperty(Property = nameof(ColumnHeaderStyle), StyleTargetType = typeof(TableViewColumnHeader))]
[StyleTypedProperty(Property = nameof(CellStyle), StyleTargetType = typeof(TableViewCell))]
public partial class TableView : Control
{
    private TableViewHeaderRow? _headerRow;
    private ScrollViewer? _scrollViewer;
    private RowDefinition? _headerRowDefinition;
    private bool _ensureColumns = true;
    private bool _isItemsSourceSuspended;
    private bool _isReattachingItemsSource;
    private List<object>? _selectionBeforeReattachReset;
    // Half of MinValue, so subtracting it from a tick count cannot overflow into "recent".
    private long _lastIntermediateViewChange = long.MinValue / 2;
    private readonly List<TableViewRow> _rows = [];
    private readonly CollectionView _collectionView = [];
    private Border? _dragRectangle;
    private Point? _dragStartPoint;
    private bool _suppressSelectionChangedCellClear;
    private Point? _lastDragCanvasPoint;
    private DispatcherTimer? _autoScrollTimer;
    private double _autoScrollVerticalDelta;
    private double _autoScrollHorizontalDelta;
    private double _dragStartVerticalOffset;
    private double _dragStartHorizontalOffset;
    private Pointer? _tableViewDragPointer;
    private UIElement? _pointerCaptureElement;
    private TableViewCellSlotRange? _lastDragSelectionCellRange;
    private ItemIndexRange? _lastDragSelectionRowRange;
    private bool _cellStateDispatchPending;
    private bool _alternateRowColorsPending;
    private readonly HashSet<int> _pendingCellStateRows = [];
    private bool _allCellStatesPending;
    private List<TableViewCellSlotRange> _previousSelectedCellRanges = [];
    private TableViewColumn? _resizingColumn;
    private double _resizingOriginalWidth;
    private readonly List<TableViewCell> _resizingPreviewCells = [];
    private readonly List<TableViewCell> _resizingDownstreamCells = [];
    private readonly List<(Panel Panel, TranslateTransform Shift)> _resizingScrollableShifts = [];
    private readonly ObservableCollection<GroupStyle> _groupStyle = [];
#if !WINDOWS
    // Uno leaves the pressed row/cell stuck in their pointer states after a drag, so the drag start elements are
    // tracked to repair them when it ends. These moved here from the deleted Uno-only partial, whose reason for
    // existing (patching ListViewBase range selection through private reflection) is gone.
    private TableViewRow? _dragStartRow;
    private TableViewCell? _dragStartCell;
#endif

    /// <summary>
    /// Initializes a new instance of the TableView class.
    /// </summary>
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);

        Columns = new TableViewColumnsCollection(this);
        FilterHandler = new ColumnFilterHandler(this);

        SetValue(ConditionalCellStylesProperty, new TableViewConditionalCellStylesCollection());

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
#if WINDOWS
        PreviewKeyDown += OnPreviewKeyDown;
#endif
        _collectionView.ItemPropertyChanged += OnItemPropertyChanged;
        _collectionView.ViewResetting += OnCollectionViewResetting;
        _collectionView.VectorChanged += OnCollectionViewVectorChanged;
        _groupStyle.CollectionChanged += delegate { RefreshGroupHeaderTemplates(); };
        Columns.ColumnPropertyChanged += OnColumnPropertyChanged;
        Columns.CollectionChanged += delegate { UpdateRowsMinWidth(); };
        RegisterPropertyChangedCallback(CellsHorizontalOffsetProperty, delegate { UpdateRowsMinWidth(); });
    }

    /// <summary>
    /// Keeps the rows' minimum width in step with a column's width or visibility.
    /// </summary>
    private void OnColumnPropertyChanged(object? sender, TableViewColumnPropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TableViewColumn.ActualWidth) or nameof(TableViewColumn.Visibility))
        {
            UpdateRowsMinWidth();
        }
    }

    /// <summary>
    /// Reconciles cell selection with a row selection change.
    /// </summary>
    /// <remarks>
    /// Reads the change as index ranges rather than as lists of items, so selecting a large span no longer costs
    /// an <c>IndexOf</c> per selected item.
    /// </remarks>
    private void OnRowSelectionChanged(TableViewSelectionModel added, TableViewSelectionModel removed)
    {
        TableViewTrace.Write($"TableViewSelectionChanged: Added={added.Count}, Removed={removed.Count}");

        if (_suppressSelectionChangedCellClear)
        {
            _suppressSelectionChangedCellClear = false;
        }
        else
        {
            if (!KeyboardHelper.IsCtrlKeyDown())
            {
                SelectedCellRanges.Clear();
            }
            else if (Columns.VisibleColumns.Count > 0)
            {
                foreach (var range in added.GetRanges())
                {
                    var slotRange = TableViewCellSlotRange.FromCoordinates(range.FirstIndex, 0, range.LastIndex, Columns.VisibleColumns.Count - 1);
                    SubtractCellRangeFromSelection(slotRange);
                }
            }

            CurrentCellSlot = null;
            OnCellSelectionChanged();
        }

        if (_rowSelection.Count is 1)
        {
            DispatcherQueue.TryEnqueue(async () => await ScrollRowIntoView(_rowSelection.FirstIndex));
        }
    }

    /// <summary>
    /// Subtracts a specified cell range from the current selection.
    /// </summary>
    /// <param name="slotRange">The cell range to subtract from the current selection.</param>
    private void SubtractCellRangeFromSelection(TableViewCellSlotRange slotRange)
    {
        while (SelectedCellRanges.FirstOrDefault(r => r.IntersectsWith(slotRange)) is { } intersectingRange)
        {
            foreach (var slicedRange in intersectingRange.Subtract(slotRange))
            {
                SelectedCellRanges.Add(slicedRange);
            }

            SelectedCellRanges.Remove(intersectingRange);
        }
    }

    /// <summary>
    /// Handles the PropertyChanged event of an item in the TableView.
    /// </summary>
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var row = ContainerFromItem(sender) as TableViewRow;

        row?.EnsureCellsStyle(default, sender);
    }

    /// <summary>
    /// Gets a value indicating whether a column is currently being resized by the user via a live
    /// drag preview (see <see cref="BeginColumnResizePreview"/>).
    /// </summary>
    internal bool IsColumnResizing { get; private set; }

    /// <summary>
    /// Starts a live resize-drag preview for <paramref name="column"/>: generously (re)measures the
    /// column's currently-realized cells once, and creates the per-cell composition-only clip/shift
    /// state that <see cref="UpdateColumnResizePreview"/> will mutate on every subsequent pointer-move
    /// frame. No real layout (Width/ActualWidth) is touched here or during the drag — that only
    /// happens once, in <see cref="EndColumnResizePreview"/>.
    /// </summary>
    internal void BeginColumnResizePreview(TableViewColumn column)
    {
        var visibleColumns = Columns.VisibleColumns;
        var columnIndex = visibleColumns.IndexOf(column);

        if (columnIndex < 0)
        {
            return;
        }

        _resizingColumn = column;
        _resizingOriginalWidth = column.ActualWidth;
        IsColumnResizing = true;
        column.IsResizing = true;

        var effectiveMax = column.MaxWidth ?? MaxColumnWidth;
        if (double.IsPositiveInfinity(effectiveMax))
        {
            effectiveMax = 4000d;
        }

        _resizingPreviewCells.Clear();
        _resizingDownstreamCells.Clear();
        _resizingScrollableShifts.Clear();

        foreach (var row in _rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Column is null || cell.Column.IsFrozen != column.IsFrozen)
                {
                    continue;
                }

                if (cell.Column == column)
                {
                    cell.BeginResizePreview(effectiveMax);
                    _resizingPreviewCells.Add(cell);
                }
                else if (visibleColumns.IndexOf(cell.Column) > columnIndex)
                {
                    cell.ApplyDownstreamShift();
                    _resizingDownstreamCells.Add(cell);
                }
            }

            if (column.IsFrozen && row.RowPresenter?.ScrollableCellsPanel is { } scrollablePanel)
            {
                var shift = new TranslateTransform();
                scrollablePanel.RenderTransform = shift;
                _resizingScrollableShifts.Add((scrollablePanel, shift));
            }
        }

#if !WINDOWS
        // The preview slides cells sideways without layout, so cells out of view now can come into view.
        UpdateRenderCulling();
#endif
    }

    /// <summary>
    /// Updates the live resize-drag preview to <paramref name="liveWidth"/>. The only work done per
    /// pointer-move frame: mutate each touched cell's own (not shared — see
    /// <see cref="TableViewCell.BeginResizePreview"/>) clip/shift in place. No Measure/Arrange, just
    /// cheap composition-only property writes, still nowhere near the cost of the real layout cascade
    /// this preview mechanism replaces.
    /// </summary>
    internal void UpdateColumnResizePreview(double liveWidth)
    {
        if (_resizingColumn is null)
        {
            return;
        }

        var delta = liveWidth - _resizingOriginalWidth;

        foreach (var cell in _resizingPreviewCells)
        {
            cell.UpdateResizePreviewClip(liveWidth, cell.ActualHeight);
            cell.UpdateGridLineShift(delta);
        }

        foreach (var cell in _resizingDownstreamCells)
        {
            cell.UpdateDownstreamShift(delta);
        }

        foreach (var (_, shift) in _resizingScrollableShifts)
        {
            shift.X = delta;
        }
    }

    /// <summary>
    /// Ends the live resize-drag preview, synchronously: clears every clip/transform the preview
    /// touched, then — if <paramref name="commitWidth"/> is not null — performs the single real width
    /// commit (<see cref="TableViewColumn.ActualWidth"/> then <see cref="TableViewColumn.Width"/>),
    /// cascading into a normal, one-time layout pass. Doing this all in one synchronous call (no
    /// await/DispatcherQueue in between) means the compositor never presents an intermediate frame —
    /// the preview's last shown state and the committed real state are numerically identical.
    /// </summary>
    internal void EndColumnResizePreview(double? commitWidth)
    {
        if (_resizingColumn is null)
        {
            return;
        }

        var column = _resizingColumn;
        _resizingColumn = null;
        IsColumnResizing = false;
        column.IsResizing = false;

        foreach (var cell in _resizingPreviewCells)
        {
            cell.EndResizePreview();
        }

        foreach (var cell in _resizingDownstreamCells)
        {
            cell.EndResizePreview();
        }

        foreach (var (panel, _) in _resizingScrollableShifts)
        {
            panel.RenderTransform = null;
        }

        _resizingPreviewCells.Clear();
        _resizingDownstreamCells.Clear();
        _resizingScrollableShifts.Clear();

        if (commitWidth is double width)
        {
            column.ActualWidth = width;
            column.Width = new GridLength(width, GridUnitType.Pixel);
        }
    }

    /// <summary>
    /// Starts a <see cref="TableViewColumnResizeMode.Live"/> resize drag for <paramref name="column"/>:
    /// unlike <see cref="BeginColumnResizePreview"/>, no cell state is touched here — every frame's
    /// width change goes through the normal, real <see cref="TableViewColumn.ActualWidth"/> cascade
    /// instead (see <see cref="UpdateColumnResizeLive"/>).
    /// </summary>
    internal void BeginColumnResizeLive(TableViewColumn column)
    {
        _resizingColumn = column;
        IsColumnResizing = true;
        column.IsResizing = true;
    }

    /// <summary>
    /// Updates a <see cref="TableViewColumnResizeMode.Live"/> resize drag to <paramref name="liveWidth"/>
    /// by setting <see cref="TableViewColumn.ActualWidth"/> directly — every visible row's cell
    /// relayouts for real on every call. Deliberately does not touch <see cref="TableViewColumn.Width"/>
    /// (which would additionally re-run <c>CalculateHeaderWidths</c> for every column on every frame);
    /// that commit happens once, in <see cref="EndColumnResizeLive"/>.
    /// </summary>
    internal void UpdateColumnResizeLive(double liveWidth)
    {
        if (_resizingColumn is null)
        {
            return;
        }

        _resizingColumn.ActualWidth = liveWidth;
    }

    /// <summary>
    /// Ends a <see cref="TableViewColumnResizeMode.Live"/> resize drag. <see cref="TableViewColumn.ActualWidth"/>
    /// already reflects the live width from <see cref="UpdateColumnResizeLive"/>, so only the
    /// <see cref="TableViewColumn.Width"/> GridLength commit is left to do here.
    /// </summary>
    internal void EndColumnResizeLive(double? commitWidth)
    {
        if (_resizingColumn is null)
        {
            return;
        }

        var column = _resizingColumn;
        _resizingColumn = null;
        IsColumnResizing = false;
        column.IsResizing = false;

        if (commitWidth is double width)
        {
            column.Width = new GridLength(width, GridUnitType.Pixel);
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Handled) return;

        var shiftKey = KeyboardHelper.IsShiftKeyDown();
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();

        if (HandleShortKeys(shiftKey, ctrlKey, e.Key))
        {
            e.Handled = true;
            return;
        }

        HandleNavigations(e, shiftKey, ctrlKey);
    }

#if WINDOWS
    /// <summary>
    /// Handles arrows before the row ScrollViewer consumes them while bubbling.
    /// Editing controls retain their own arrow-key behavior.
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || IsEditing
            || e.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down))
        {
            return;
        }

        // Header controls and other controls outside the rows keep their own keyboard behavior.
        if (e.OriginalSource is not FrameworkElement source
            || (source != this && source is not (TableViewCell or TableViewRow)
                && source.FindAscendant<TableViewRow>() is null))
        {
            return;
        }

        HandleNavigations(e, KeyboardHelper.IsShiftKeyDown(), KeyboardHelper.IsCtrlKeyDown());
    }
#endif

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        var originalSource = e.OriginalSource as FrameworkElement;

        if (originalSource?.FindAscendant<ScrollBar>() is not null          // Skip if the pointer is over a ScrollBar.
            || originalSource?.FindAscendant<TableViewCell>() is not null   // Skip if the pointer is over a TableViewCell.
            || originalSource?.FindAscendant<TableViewRow>() is not null)   // Skip if the pointer is over a TableViewRow.
        {
            return;
        }

        e.Handled = OnAnyPointerPressed(this, e);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        EndDragSelection();
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        EndDragSelectionOnCaptureLost();
    }

    /// <summary>
    /// Ends a drag selection whose pointer capture was lost, such as to a window switch or a cancelled touch.
    /// </summary>
    /// <remarks>
    /// On Windows the pressed row captures the pointer and handles the loss itself; on the other platforms the table
    /// captures it (see <see cref="OnAnyPointerPressed"/>), so without this the table stayed in drag selection and
    /// the rows stayed looking pressed.
    /// </remarks>
    internal void EndDragSelectionOnCaptureLost()
    {
        foreach (var row in _rows)
        {
            row.ResetPointerState();
        }

        EndDragSelection();
    }

    /// <summary>
    /// Handles pointer-pressed for all cases, including when elements sets <c>e.Handled = true</c>.
    /// </summary>
    internal bool OnAnyPointerPressed(UIElement pressedElement, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        var position = pointerPoint.Position;
        var canvasPoint = GetCanvasPoint(position);
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();
        var isShiftKey = KeyboardHelper.IsShiftKeyDown();

        if (SelectionMode is ListViewSelectionMode.None                                 // Skip selection when SelectionMode is None
            || IsDragSelecting                                                          // Skip selection when a drag is already in progress
            || canvasPoint is null                                                      // Skip selection when canvasPoint is null (e.g., pointer is outside the scroll canvas)
            || canvasPoint.Value.Y < 0                                                  // Skip selection when the pointer is in the column header area (above the scroll canvas)
            || !pointerPoint.Properties.IsLeftButtonPressed                             // Skip selection when the left mouse button is not pressed
            || (pressedElement == this && !ShowDragRectangle)                           // Skip selection when the pointer is not over a Cell or Row, and ShowDragRectangle is false.
            )
        {
            return false;
        }

        // Shift extends the selection from the anchor instead of starting a new one. It has to be handled here:
        // the base ListView never sees the plain clicks handled below, so its own anchor would be stale.
        if (isShiftKey && SelectionMode is ListViewSelectionMode.Extended or ListViewSelectionMode.Multiple)
        {
            return ExtendSelectionTo(pressedElement, ctrlKey);
        }

        _lastDragCanvasPoint = null;
        CurrentCellSlot = null;
        SelectionStartCellSlot = null;
        SelectionStartRowIndex = null;
        _lastDragSelectionRowRange = null;
        _lastDragSelectionCellRange = null;
        LastSelectionUnit = TableViewSelectionUnit.Row;
#if !WINDOWS
        _dragStartCell = pressedElement as TableViewCell;
        _dragStartRow = (pressedElement as TableViewRow) ?? pressedElement?.FindAscendant<TableViewRow>();
#endif
        pressedElement ??= this; // If not, default to the TableView itself

        SelectionStartCellSlot = (pressedElement as TableViewCell)?.Slot;
        SelectionStartRowIndex = (pressedElement as TableViewRow)?.Index ?? SelectionStartCellSlot?.Row;

        LastSelectionUnit = SelectionUnit switch
        {
            TableViewSelectionUnit.Cell => TableViewSelectionUnit.Cell,
            TableViewSelectionUnit.Row => TableViewSelectionUnit.Row,
            _ => pressedElement is TableViewCell
                ? TableViewSelectionUnit.Cell
                : TableViewSelectionUnit.Row
        };

        if (SelectionMode is ListViewSelectionMode.Single)
        {
            if (LastSelectionUnit is TableViewSelectionUnit.Cell && SelectionStartCellSlot is not null)
                MakeSelection(SelectionStartCellSlot.Value, false, false);
            else if (SelectionStartRowIndex is not null)
                MakeSelection(new(SelectionStartRowIndex.Value, -1), false, false);

            SetCurrentCell(SelectionStartCellSlot);

            return true;
        }

        pressedElement.Focus(FocusState.Programmatic);
#if WINDOWS
        _pointerCaptureElement = pressedElement;
#else
        _pointerCaptureElement = this;
#endif

        _pointerCaptureElement.CapturePointer(e.Pointer);
        _tableViewDragPointer = e.Pointer;

        if (!ctrlKey && SelectionMode is not ListViewSelectionMode.Multiple && LastSelectionUnit is not TableViewSelectionUnit.Cell)
            DeselectAll();

        StartDragSelection(canvasPoint.Value);

        if (!IsDragSelecting)
        {
            _pointerCaptureElement?.ReleasePointerCaptures();
            _pointerCaptureElement = null;
            _tableViewDragPointer = null;
            return false;
        }

        MakeSelectionInDragRect();
        return true;
    }

    /// <summary>
    /// Extends the selection from the selection anchor to the pressed row or cell (Shift+Click).
    /// </summary>
    /// <returns>True if the press was on a row or cell and the selection was extended to it.</returns>
    internal bool ExtendSelectionTo(UIElement pressedElement, bool ctrlKey)
    {
        TableViewCellSlot slot;

        if (pressedElement is TableViewCell cell)
        {
            slot = cell.Slot;
            // A finished click or drag clears the cell anchor and leaves the current cell as the anchor.
            SelectionStartCellSlot ??= CurrentCellSlot;
        }
        else if (pressedElement is TableViewRow row)
        {
            slot = new(row.Index, -1);
        }
        else
        {
            return false;
        }

        MakeSelection(slot, shiftKey: true, ctrlKey);

        return true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!IsDragSelecting)
        {
            return;
        }

        var canvasPoint = GetCanvasPoint(e.GetCurrentPoint(this).Position);
        if (canvasPoint is null)
        {
            return;
        }

        // Drive the rect visual for all drag sources (cell-initiated drags bubble pointer events here).
        UpdateDragRectangleVisual(canvasPoint.Value);

        // _tableViewDragPointer is only set for drags this TableView captured (see OnAnyPointerPressed),
        // which covers every drag in Multiple/Extended mode. The guard stops a foreign pointer capture
        // from driving selection.
        if (_tableViewDragPointer is not null)
        {
            MakeSelectionInDragRect();
        }
    }

    /// <summary>
    /// Makes selection based on the current drag rectangle, selecting either rows or cells depending on the last selection unit.
    /// </summary>
    private void MakeSelectionInDragRect()
    {
        if (LastSelectionUnit is not TableViewSelectionUnit.Cell)
        {
            if (GetRowsInDragRect() is ItemIndexRange rows)
            {
                SelectRowsInDragRect(rows);
            }
            else if (_lastDragSelectionRowRange?.Length > 0)
            {
                DeselectRange(_lastDragSelectionRowRange);

                _lastDragSelectionRowRange = null;
                SelectionStartRowIndex = null;
            }
        }
        else if (LastSelectionUnit is not TableViewSelectionUnit.Row)
        {
            if (GetCellsInDragRect() is TableViewCellSlotRange cells)
            {
                SelectCellsInDragRect(cells);
            }
            else if (_lastDragSelectionCellRange?.Length > 0)
            {
                DeselectCellRange(_lastDragSelectionCellRange);

                _lastDragSelectionCellRange = null;
                SelectionStartCellSlot = null;
            }
            else if (!KeyboardHelper.IsCtrlKeyDown())
            {
                DeselectAllCells();
            }
        }
    }

    /// <summary>
    /// Returns the range of cell slots covered by the current drag rectangle.
    /// The first slot is the one nearest the drag start point and the last slot
    /// is the one nearest the drag end point.
    /// </summary>
    private ItemIndexRange? GetRowsInDragRect()
    {
        if (_dragRectangle is null || _dragStartPoint is null || _lastDragCanvasPoint is null)
        {
            return null;
        }

        // Reconstruct the scroll-adjusted start corner the same way PositionDragRectangle does,
        // so we know which corner of the rect corresponds to the drag origin.
        var verticalScrollDelta = (_scrollViewer?.VerticalOffset ?? 0) - _dragStartVerticalOffset;
        var startY = _dragStartPoint.Value.Y - verticalScrollDelta;
        var endY = _lastDragCanvasPoint.Value.Y;

        // Orientation of the drag, used to order the returned range from start to end.
        var rowsTopToBottom = startY <= endY; ;

        // Drag rect bounds in canvas space (already clamped and scroll-adjusted by PositionDragRectangle).
        var rectTop = Canvas.GetTop(_dragRectangle);
        var rectBottom = rectTop + _dragRectangle.Height;

        // The rect's top and bottom map straight to row indexes, so there is no scan over the items here.
        var firstVisual = GetVisualRowIndexAtCanvasY(rectTop);
        var lastVisual = GetVisualRowIndexAtCanvasY(Math.Max(rectTop, rectBottom - 1));

        if (firstVisual < 0 && lastVisual < 0)
        {
            return null;
        }

        firstVisual = firstVisual < 0 ? 0 : firstVisual;
        lastVisual = lastVisual < 0 ? VisualRowCount - 1 : lastVisual;

        var minRow = FirstItemIndexAtOrAfter(firstVisual, lastVisual);
        var maxRow = LastItemIndexAtOrBefore(firstVisual, lastVisual);

        if (minRow < 0 || maxRow < 0)
        {
            return null;
        }

        // Use the anchor slot captured at drag start as the first slot. The visible scan above
        // can't see rows/columns that auto-scroll has moved out of view (virtualized), so the
        // anchor is the only reliable record of where the drag actually began.
        if (SelectionStartRowIndex is { } anchor)
        {
            if (rowsTopToBottom) minRow = anchor;
            else maxRow = anchor;
        }
        else
        {
            SelectionStartRowIndex = rowsTopToBottom ? minRow : maxRow;
        }

        return new ItemIndexRange(minRow, (uint)(maxRow - minRow + 1));
    }

    /// <summary>
    /// Returns the range of cell slots covered by the current drag rectangle.
    /// The first slot is the one nearest the drag start point and the last slot
    /// is the one nearest the drag end point.
    /// </summary>
    private TableViewCellSlotRange? GetCellsInDragRect()
    {
        if (_dragRectangle is null || DragRectangleCanvas is null || _dragRectangle.Visibility != Visibility.Visible
            || _dragStartPoint is null || _lastDragCanvasPoint is null)
        {
            return null;
        }

        // Reconstruct the scroll-adjusted start corner the same way PositionDragRectangle does,
        // so we know which corner of the rect corresponds to the drag origin.
        var verticalScrollDelta = (_scrollViewer?.VerticalOffset ?? 0) - _dragStartVerticalOffset;
        var horizontalScrollDelta = HorizontalOffset - _dragStartHorizontalOffset;
        var startX = _dragStartPoint.Value.X - horizontalScrollDelta;
        var startY = _dragStartPoint.Value.Y - verticalScrollDelta;
        var endX = _lastDragCanvasPoint.Value.X;
        var endY = _lastDragCanvasPoint.Value.Y;

        // Orientation of the drag, used to order the returned range from start to end.
        var rowsTopToBottom = startY <= endY;
        var colsLeftToRight = startX <= endX;

        // Drag rect bounds in canvas space (already clamped and scroll-adjusted by PositionDragRectangle).
        var rectLeft = Canvas.GetLeft(_dragRectangle);
        var rectRight = rectLeft + _dragRectangle.Width;

        var rows = GetRowsInDragRect();

        if (rows is null || rows.Length == 0) return null;

        // Find the min/max row indices whose bounds intersect the rect vertically.
        var minRow = rows.FirstIndex;
        var maxRow = rows.LastIndex;

        // Find the min/max column indices whose bounds intersect the rect horizontally.
        // Frozen columns are pinned and don't scroll; non-frozen columns shift with HorizontalOffset.
        // Non-frozen columns that scroll behind the frozen panel are not selectable from that area.
        var minColumn = -1;
        var maxColumn = -1;
        var frozenCount = FrozenColumnCount;
        var columnLeft = CellsHorizontalOffset;
        var frozenPanelRight = CellsHorizontalOffset; // updated when we cross into non-frozen territory

        for (var colIndex = 0; colIndex < Columns.VisibleColumns.Count; colIndex++)
        {
            if (colIndex == frozenCount)
            {
                frozenPanelRight = columnLeft;
                columnLeft -= HorizontalOffset;
            }

            var columnRight = columnLeft + Columns.VisibleColumns[colIndex].ActualWidth;

            // Clamp non-frozen columns to the visible area past the frozen panel.
            var effectiveLeft = colIndex >= frozenCount ? Math.Max(columnLeft, frozenPanelRight) : columnLeft;

            if (columnRight > rectLeft && effectiveLeft < rectRight)
            {
                if (minColumn == -1) minColumn = colIndex;
                maxColumn = colIndex;
            }

            columnLeft = columnRight;
        }

        if (minColumn == -1)
        {
            return null;
        }

        // Use the anchor slot captured at drag start as the first slot. The visible scan above
        // can't see rows/columns that auto-scroll has moved out of view (virtualized), so the
        // anchor is the only reliable record of where the drag actually began.
        if (SelectionStartCellSlot is { } anchor)
        {
            if (rowsTopToBottom) minRow = anchor.Row;
            else maxRow = anchor.Row;

            if (colsLeftToRight) minColumn = anchor.Column;
            else maxColumn = anchor.Column;
        }
        else
        {
            var startCol = colsLeftToRight ? minColumn : maxColumn;
            SelectionStartCellSlot = new(SelectionStartRowIndex ?? minRow, startCol);
        }

        return TableViewCellSlotRange.FromSlots(new(minRow, minColumn), new(maxRow, maxColumn));
    }

    /// <summary>
    /// Selects rows that intersect with the current drag rectangle, updating the selection state accordingly.
    /// </summary>
    private void SelectRowsInDragRect(ItemIndexRange rows)
    {
        if (_lastDragSelectionRowRange?.FirstIndex == rows.FirstIndex && _lastDragSelectionRowRange?.LastIndex == rows.LastIndex) return;

        if (SelectionMode is ListViewSelectionMode.Single && rows.Length is 1)
        {
            SelectedIndex = rows.FirstIndex;
        }
        else if (_lastDragSelectionRowRange is not null && _lastDragSelectionRowRange.Contains(rows))
        {
            foreach (var slicedRange in _lastDragSelectionRowRange.Subtract(rows))
            {
                DeselectRange(slicedRange);
            }
        }
        else if (rows.Length > 0)
        {
            SelectRange(rows);
        }

        _lastDragSelectionRowRange = rows;
    }

    /// <summary>
    /// Selects cells that intersect with the current drag rectangle, updating the selection state accordingly.
    /// </summary>
    private void SelectCellsInDragRect(TableViewCellSlotRange cells)
    {
        if (_lastDragSelectionCellRange == cells) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            if (_lastDragSelectionCellRange is null
            && !KeyboardHelper.IsCtrlKeyDown()
            && SelectionMode is not ListViewSelectionMode.Multiple)
            {
                DeselectAllItems();
                SelectedCellRanges.Clear();
            }
            else if (_lastDragSelectionCellRange is not null && cells is not null)
            {
                foreach (var range in _lastDragSelectionCellRange.Subtract(cells))
                {
                    SubtractCellRangeFromSelection(range);
                }
            }

            if (SelectedCellRanges.Any(r => r == cells))
            {
                OnCellSelectionChanged();
            }
            else if (cells?.Length > 0)
            {
                SelectCellRange(cells);
            }

            _lastDragSelectionCellRange = cells;
        });
    }

    /// <summary>
    /// Handles navigation keys.
    /// </summary>
    private void HandleNavigations(KeyRoutedEventArgs e, bool shiftKey, bool ctrlKey)
    {
        var currentCell = CurrentCellSlot.HasValue ? GetCellFromSlot(CurrentCellSlot.Value) : default;

        if (e.Key is VirtualKey.F2 && currentCell is { IsReadOnly: false } && !IsEditing)
        {
            e.Handled = currentCell.BeginCellEditing(e);
        }
        else if (e.Key is VirtualKey.Escape && currentCell is not null && IsEditing)
        {
            // Transfer focus from the editing element (e.g. TextBox) to the cell
            // itself BEFORE EndCellEditing tears down that element.  If we wait,
            // WinUI's focus manager will move focus to the next focusable sibling
            // the moment the editing element is removed from the visual tree, and
            // screen readers will announce that sibling instead of the current cell.
            currentCell.Focus(FocusState.Programmatic);

            e.Handled = EndCellEditing(TableViewEditAction.Cancel, currentCell);
            SetIsEditing(false);
        }
        else if (e.Key is VirtualKey.Space && currentCell is not null && CurrentCellSlot.HasValue && !IsEditing)
        {
            if (!currentCell.IsSelected)
            {
                MakeSelection(CurrentCellSlot.Value, shiftKey, ctrlKey);
            }
            else
            {
                DeselectCell(CurrentCellSlot.Value);
            }
        }

        // Handle navigation keys
        else if (e.Key is VirtualKey.Tab or VirtualKey.Enter)
        {
            var isEditing = IsEditing;

            var newSlot = CurrentCellSlot ?? new();

            do
            {
                newSlot = GetNextSlot(newSlot, shiftKey, e.Key is VirtualKey.Enter);

            } while (isEditing && Columns[newSlot.Column].IsReadOnly);

            if (isEditing && currentCell is not null)
            {
                if (!EndCellEditing(TableViewEditAction.Commit, currentCell)) return;

                if (CurrentCellSlot == newSlot || GetCellFromSlot(newSlot) is not { } nextCell || !nextCell.BeginCellEditing(e))
                {
                    SetIsEditing(false);
                }
            }

            MakeSelection(newSlot, false);

            e.Handled = true;
        }
        else if ((e.Key is VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down)
                 && !IsEditing)
        {
            var row = (LastSelectionUnit is TableViewSelectionUnit.Row ? CurrentRowIndex : CurrentCellSlot?.Row) ?? -1;
            var column = CurrentCellSlot?.Column ?? -1;

            if (row == -1 && column == -1)
            {
                row = column = 0;
            }
            else if (e.Key is VirtualKey.Left or VirtualKey.Right)
            {
                column = e.Key is VirtualKey.Left ? ctrlKey ? 0 : column - 1 : ctrlKey ? Columns.VisibleColumns.Count - 1 : column + 1;
                if (column >= Columns.VisibleColumns.Count)
                {
                    column = 0;
                    row++;
                }
            }
            else
            {
                row = e.Key == VirtualKey.Up ? ctrlKey ? 0 : row - 1 : ctrlKey ? Items.Count - 1 : row + 1;
            }

            var newSlot = new TableViewCellSlot(row, column);
            MakeSelection(newSlot, shiftKey);
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.Home or VirtualKey.End)
        {
            var row = ctrlKey ? (e.Key == VirtualKey.Home ? 0 : _collectionView.Count - 1) : CurrentCellSlot?.Row;
            var column = e.Key == VirtualKey.Home ? 0 : Columns.VisibleColumns.Count - 1;

            var newSlot = new TableViewCellSlot(row ?? -1, column);
            MakeSelection(newSlot, shiftKey);
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.PageDown or VirtualKey.PageUp)
        {
            var pageSize = CalculateAvailablePageSize();

            var row = (LastSelectionUnit is TableViewSelectionUnit.Row ? CurrentRowIndex : CurrentCellSlot?.Row) ?? -1;
            var column = CurrentCellSlot?.Column ?? -1;
            var nextRow = GetRowOnePageAway(row, pageSize, down: e.Key is VirtualKey.PageDown);

            if (nextRow >= 0)
            {
                MakeSelection(new TableViewCellSlot(nextRow, column), shiftKey);
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Returns the item index a page away from <paramref name="itemIndex"/>, or -1 when there are no rows.
    /// </summary>
    /// <remarks>
    /// Pages are measured in displayed rows, group headers included, so the step is taken in visual space and
    /// then settled on the nearest data row in the direction of travel.
    /// </remarks>
    private int GetRowOnePageAway(int itemIndex, int pageSize, bool down)
    {
        if (_visualRows is null || VisualRowCount is 0)
        {
            return _collectionView.Count is 0
                ? -1
                : Math.Clamp(down ? itemIndex + pageSize : itemIndex - pageSize, 0, _collectionView.Count - 1);
        }

        var visualIndex = Math.Max(0, GetVisualIndexFromItemIndex(itemIndex));
        var target = Math.Clamp(down ? visualIndex + pageSize : visualIndex - pageSize, 0, VisualRowCount - 1);
        var ahead = down ? _visualRows.FindItemIndexAtOrAfter(target) : _visualRows.FindItemIndexAtOrBefore(target);

        return ahead >= 0 ? ahead : down ? _visualRows.FindItemIndexAtOrBefore(target) : _visualRows.FindItemIndexAtOrAfter(target);
    }

    /// <summary>
    /// Calculates how many rows should be able to fit within the actual height of the table without scrolling.
    /// </summary>
    internal int CalculateAvailablePageSize()
    {
        // The viewport over the height of a row: the configured one, else the realized row at the top of the viewport.
        if (_scrollViewer is { ViewportHeight: > 0 } scrollViewer && VisualRowCount > 0)
        {
            var pageRowHeight = RowHeight;

            if (double.IsNaN(pageRowHeight)
                && RealizedRows.FindAtOffset(RowsViewportTop, out _) is >= 0 and var topRow
                && RealizedRows.TryGetBounds(topRow, out _, out var height))
            {
                pageRowHeight = height;
            }

            if (double.IsNaN(pageRowHeight) || pageRowHeight <= 0d)
            {
                pageRowHeight = RowMinHeight;
            }

            return Math.Max(1, (int)(scrollViewer.ViewportHeight / pageRowHeight));
        }

        var rowHeight = double.IsNaN(RowHeight) ? RowMinHeight : RowHeight;
        var headerHeight = double.IsNaN(HeaderRowHeight) ? HeaderRowMinHeight : HeaderRowHeight;
        var availableHeight = ActualHeight - headerHeight;

        return rowHeight > 0 ? Math.Max(1, (int)Math.Floor(availableHeight / rowHeight)) : 1;
    }

    /// <summary>
    /// Ends the editing of a cell, committing or canceling the edit based on the specified action.
    /// </summary>
    internal bool EndCellEditing(TableViewEditAction editAction, TableViewCell cell)
    {
        var editingElement = cell.Content as FrameworkElement;
        var endingArgs = new TableViewCellEditEndingEventArgs(cell, cell.Row?.Content, cell.Column!, editingElement!, editAction);
        OnCellEditEnding(endingArgs);
        if (endingArgs.Cancel)
        {
            return false;
        }

        cell.EndEditing(editAction);

        var endArgs = new TableViewCellEditEndedEventArgs(cell, cell.Row?.Content, cell.Column!, editAction);
        OnCellEditEnded(endArgs);

        return true;
    }

    /// <summary>
    /// Handles shortcut keys.
    /// </summary>
    private bool HandleShortKeys(bool shiftKey, bool ctrlKey, VirtualKey key)
    {
        if (key == VirtualKey.A && ctrlKey && !shiftKey)
        {
            SelectAll();
            return true;
        }
        else if (key == VirtualKey.A && ctrlKey && shiftKey)
        {
            DeselectAll();
            return true;
        }
        else if (key == VirtualKey.C && ctrlKey)
        {
            CopyToClipboardInternal(shiftKey);
            return true;
        }
        else if (key == VirtualKey.V && ctrlKey && !shiftKey)
        {
            return TryStartPasteFromClipboard();
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerRow = GetTemplateChild("HeaderRow") as TableViewHeaderRow;
        _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
        _headerRowDefinition = GetTemplateChild("HeaderRowDefinition") as RowDefinition;
        DragRectangleCanvas = GetTemplateChild("DragRectangleCanvas") as FrameworkElement;
        _dragRectangle = GetTemplateChild("DragRectangle") as Border;
        _scrollViewer?.Loaded += OnScrollViewerLoaded;
        _scrollViewer?.ViewChanged += OnScrollViewerViewChanged;
        _scrollViewer?.SizeChanged += OnScrollViewerSizeChanged;

        if (GetTemplateChild("RowsRepeater") is ItemsRepeater rowsRepeater)
        {
            InitializeRowsHost(rowsRepeater);
        }

        if (IsLoaded)
        {
            EnsureAutoColumns();
            ApplyStickyGroupHeadersSetting();
        }

        SetHeadersVisibility();
    }

    /// <summary>
    /// Applies <see cref="AreStickyGroupHeadersEnabled"/> to the sticky group header.
    /// </summary>
    private void ApplyStickyGroupHeadersSetting()
    {
        UpdateStickyGroupHeader();
    }

    /// <summary>
    /// Handles the ViewChanged event of the ScrollViewer control.
    /// </summary>
    /// <remarks>
    /// Deliberately empty of per-row work. Row positions used to be recomputed here with a visual-tree transform
    /// walk per realized row on every scroll frame, purely to feed hit testing; hit testing now reads them from the
    /// realized rows when it needs them, so scrolling costs nothing outside layout itself.
    /// </remarks>
    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate)
        {
            _lastIntermediateViewChange = Environment.TickCount64;
        }

        SetValue(VerticalOffsetProperty, _scrollViewer?.VerticalOffset ?? 0d);

        // Which group is at the top, and how far the next header pushes its sticky header up, change on any scroll.
        if (AreStickyGroupHeadersActive || _stickyGroupHeader?.Visibility is Visibility.Visible)
        {
            UpdateStickyGroupHeader();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the view is in the middle of a scroll: a wheel animation, panning or a
    /// fling that has not settled yet.
    /// </summary>
    /// <remarks>
    /// An intermediate view change arrives every frame while the view moves, and the final one on the frame it
    /// settles, so recency of the last intermediate one is what says the view is still moving. A programmatic
    /// jump with animation disabled raises only the final change and never reads as in flight.
    /// </remarks>
    internal bool IsViewChangeInFlight => Environment.TickCount64 - _lastIntermediateViewChange < 100;

    /// <summary>
    /// Re-measures the group headers and the open row details when the viewport's width changes, since they are
    /// measured no wider than it.
    /// </summary>
    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width != e.PreviousSize.Width)
        {
            foreach (var header in _groupHeaderRows)
            {
                header.InvalidateMeasure();
            }

            foreach (var row in _rows)
            {
                if (row.RowPresenter is { IsDetailsPanelVisible: true } presenter)
                {
                    presenter.InvalidateMeasure();
                }
            }
        }
    }

    /// <summary>
    /// Handles the Loaded event of the ScrollViewer control.
    /// </summary>
    private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        // Found by name, not by type: under Native AOT the wrapper CsWinRT creates for an element from XAML can
        // be a base class of its runtime class when the exact projection is not in the app's type map, and a
        // search by concrete type then walks right past it.
        var scrollPresenter = _scrollViewer?.FindDescendant<FrameworkElement>(x => x.Name is "ScrollContentPresenter");
        var xScrollBar = _scrollViewer?.FindDescendant<ScrollBar>(sb => sb.Name is "HorizontalScrollBar2");
        var yScrollBar = _scrollViewer?.FindDescendant<ScrollBar>(sb => sb.Name is "VerticalScrollBar");

        scrollPresenter?.PointerWheelChanged += OnScrollContentPresenterPointerWheelChanged;

        if (_scrollViewer is not null)
        {
            AttachStickyGroupHeaderHost(_scrollViewer);
        }

        yScrollBar?.ValueChanged += (_, _) => SetValue(VerticalOffsetProperty, yScrollBar.Value);

        xScrollBar?.SetBinding(RangeBase.ValueProperty, new Binding
        {
            Path = new PropertyPath(nameof(HorizontalOffset)),
            Mode = BindingMode.TwoWay,
            Source = this
        });
    }

    /// <summary>
    /// Handles the Loaded event of the TableView control.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isItemsSourceSuspended) // indicates that the control was unloaded and loaded back
        {
            _headerRow?.CalculateHeaderWidths();  // Needed when switching back to an existing TableView (without provided column Widths)
        }

        ResumeItemsSource();
        ResumeRowsHost();
        ConsiderLoadingMoreItems(-1);
        EnsureAutoColumns();
        ApplyStickyGroupHeadersSetting();
    }

    /// <summary>
    /// Handles the Unloaded event of the TableView control.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // WinUI can raise a stale Unloaded after Loaded while the control is still in the live tree
        // (e.g. content of a ContentDialog, microsoft-ui-xaml#8342); treat it as a no-op.
        if (IsLoaded) return;

        EndDragSelection();
        StopAutoScroll();

        if (IsEditing && CurrentCellSlot.HasValue && GetCellFromSlot(CurrentCellSlot.Value) is { } currentCell)
        {
            currentCell.EndEditing(TableViewEditAction.Commit);
        }

        SuspendItemsSource();
        SuspendRowsHost();
        HideStickyGroupHeader(releaseGroup: true);
    }

    /// <summary>
    /// Suspends subscriptions to the current items source while the control is unloaded.
    /// </summary>
    private void SuspendItemsSource()
    {
        if (_isItemsSourceSuspended)
        {
            return;
        }

        // The view keeps its items, so the selection (and anything bound to it) survives being unloaded.
        _collectionView.ItemPropertyChanged -= OnItemPropertyChanged;
        _collectionView.DetachFromSource();
        _isItemsSourceSuspended = true;
    }

    /// <summary>
    /// Restores subscriptions to the current items source when the control is loaded.
    /// </summary>
    private void ResumeItemsSource()
    {
        if (!_isItemsSourceSuspended)
        {
            return;
        }

        _collectionView.ItemPropertyChanged += OnItemPropertyChanged;
        _isItemsSourceSuspended = false;

        if (ItemsSource is IEnumerable source && !ReferenceEquals(_collectionView.Source, source))
        {
            _collectionView.Source = source; // the source was replaced while the control was unloaded
            return;
        }

        // A source that changed while the control was unloaded is applied item by item, which keeps the
        // selection, or by a reset, after which the items still in the view are selected again.
        _isReattachingItemsSource = true;

        try
        {
            _collectionView.AttachToSource();
        }
        finally
        {
            _isReattachingItemsSource = false;
        }

        if (_selectionBeforeReattachReset is { } selectedItems)
        {
            _selectionBeforeReattachReset = null;
            RestoreSelection(selectedItems);
        }
    }

    /// <summary>
    /// Selects the given items again, skipping the ones that are no longer in the view.
    /// </summary>
    private void RestoreSelection(List<object> selectedItems)
    {
        if (SelectionMode is ListViewSelectionMode.None) return;

        if (SelectionMode is ListViewSelectionMode.Single)
        {
            if (_collectionView.IndexOf(selectedItems[0]) is >= 0 and var index)
            {
                SelectedIndex = index;
            }

            return;
        }

        ChangeSelection(model =>
        {
            var changed = false;

            foreach (var item in selectedItems)
            {
                if (_collectionView.IndexOf(item) is >= 0 and var index)
                {
                    changed |= model.Select(index, index);
                }
            }

            return changed;
        });
    }

    /// <summary>
    /// Handles the PointerWheelChanged event of the ScrollContentPresenter.
    /// </summary>
    private void OnScrollContentPresenterPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        var isShiftButton = KeyboardHelper.IsShiftKeyDown();
        var isHorizontalScroll = isShiftButton || pointerPoint.Properties.IsHorizontalMouseWheel;

        if (isHorizontalScroll && _scrollViewer?.ComputedHorizontalScrollBarVisibility is Visibility.Visible)
        {
            e.Handled = true;
            var mouseWheelDelta = isShiftButton ? -pointerPoint.Properties.MouseWheelDelta : pointerPoint.Properties.MouseWheelDelta;
            var xOffset = HorizontalOffset + (mouseWheelDelta / 4.0);
            SetValue(HorizontalOffsetProperty, Math.Clamp(xOffset, 0, _scrollViewer.ScrollableWidth));
        }
    }

    /// <summary>
    /// Gets the next cell slot based on the current slot and input keys.
    /// </summary>
    private TableViewCellSlot GetNextSlot(TableViewCellSlot? currentSlot, bool isShiftKeyDown, bool isEnterKey)
    {
        var rows = Items.Count;
        var columns = Columns.VisibleColumns.Count;
        var currentRow = currentSlot?.Row ?? SelectedIndex;
        var currentColumn = currentSlot?.Column ?? -1;
        var nextRow = currentRow;
        var nextColumn = currentColumn;

        if (nextRow == -1 && nextColumn == -1)
        {
            nextRow = nextColumn = 0;
        }
        else if (isEnterKey)
        {
            nextRow += isShiftKeyDown ? -1 : 1;
            if (nextRow < 0)
            {
                nextRow = rows - 1;
                nextColumn = (nextColumn - 1 + columns) % columns;
            }
            else if (nextRow >= rows)
            {
                nextRow = 0;
                nextColumn = (nextColumn + 1) % columns;
            }
        }
        else
        {
            nextColumn += isShiftKeyDown ? -1 : 1;
            if (nextColumn < 0)
            {
                nextColumn = columns - 1;
                nextRow = (nextRow - 1 + rows) % rows;
            }
            else if (nextColumn >= columns)
            {
                nextColumn = 0;
                nextRow = (nextRow + 1) % rows;
            }
        }

        return new TableViewCellSlot(nextRow, nextColumn);
    }

    /// <summary>
    /// Copies the selected rows or cells content to the clipboard.
    /// </summary>
    internal void CopyToClipboardInternal(bool includeHeaders)
    {
        // Skip TableView copy logic when a cell editor already handles Ctrl+C.
        // TextBox, PasswordBox, and RichEditBox all implement their own copy behavior.
        var focused = FocusManager.GetFocusedElement(XamlRoot!) as FrameworkElement;
        if (focused is TextBox or PasswordBox or RichEditBox)
        {
            return;
        }

        var args = new TableViewCopyToClipboardEventArgs(includeHeaders);
        OnCopyToClipboard(args);

        if (!CanCopy || args.Handled)
        {
            return;
        }

        var content = GetSelectedClipboardContent(includeHeaders);

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        // Try/catch to prevent CLIPBRD_E_CANT_OPEN crashes.
        try
        {
            var package = new DataPackage();
            package.SetText(content);

            Clipboard.SetContent(package);
        }
        catch (Exception ex)
        {
            // Clipboard failures are normal on Windows (e.g., CLIPBRD_E_CANT_OPEN).
            // Swallow to avoid crashing the application.
            TableViewTrace.Write($"TableView: Clipboard.SetContent failed: {ex}");
        }
    }

    /// <summary>
    /// Returns the selected cells' or rows' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of selected cell content separated by the specified character.</returns>
    public string GetSelectedContent(bool includeHeaders, char separator = '\t')
    {
        var slots = GetSelectedCellSlots();

        return GetCellsContent(slots, includeHeaders, separator);
    }

    /// <summary>
    /// Returns the selected cells' or rows' clipboard content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of selected cell clipboard content separated by the specified character.</returns>
    public string GetSelectedClipboardContent(bool includeHeaders, char separator = '\t')
    {
        var slots = GetSelectedCellSlots();

        return GetCellsContent(slots, includeHeaders, separator, true);
    }

    private IEnumerable<TableViewCellSlot> GetSelectedCellSlots()
    {
        var slots = Enumerable.Empty<TableViewCellSlot>();

        if (_rowSelection.Count > 0 || SelectedCells.Count != 0)
        {
            slots = SelectedRanges.SelectMany(x => Enumerable.Range(x.FirstIndex, (int)x.Length))
                                  .SelectMany(r => Enumerable.Range(0, Columns.VisibleColumns.Count)
                                                                     .Select(c => new TableViewCellSlot(r, c)))
                                  .Concat(SelectedCells)
                                  .OrderBy(x => x.Row)
                                  .ThenByDescending(x => x.Column);
        }
        else if (CurrentCellSlot.HasValue)
        {
            slots = [CurrentCellSlot.Value];
        }

        return slots;
    }

    /// <summary>
    /// Returns all the cells' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of all cell content separated by the specified character.</returns>
    public string GetAllContent(bool includeHeaders, char separator = '\t')
    {
        var rows = Enumerable.Range(0, Items.Count).ToArray();

        return GetRowsContent(rows, includeHeaders, separator);
    }

    /// <summary>
    /// Returns specified rows' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="rows">Row indexes to get content for.</param>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <returns>A string of specified row content separated by the specified character.</returns>
    public string GetRowsContent(int[] rows, bool includeHeaders, char separator = '\t')
    {
        var slots = rows.SelectMany(r => Enumerable.Range(0, Columns.VisibleColumns.Count)
                                                           .Select(c => new TableViewCellSlot(r, c)))
                        .OrderBy(x => x.Row)
                        .ThenByDescending(x => x.Column);

        return GetCellsContent(slots, includeHeaders, separator);
    }

    /// <summary>
    /// Returns specified cells' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="slots">Cell slots to get content for.</param>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <returns>A string of specified cell content separated by the specified character.</returns>
    public string GetCellsContent(IEnumerable<TableViewCellSlot> slots, bool includeHeaders, char separator = '\t')
    {
        return GetCellsContent(slots, includeHeaders, separator, false);
    }

    private string GetCellsContent(IEnumerable<TableViewCellSlot> slots, bool includeHeaders, char separator, bool isClipboardContent)
    {
        if (!slots.Any())
        {
            return string.Empty;
        }

        var minColumn = slots.Select(x => x.Column).Min();
        var maxColumn = slots.Select(x => x.Column).Max();
        var stringBuilder = new StringBuilder();

        if (includeHeaders)
        {
            stringBuilder.Append(GetHeadersContent(separator, minColumn, maxColumn));
            stringBuilder.Append('\n');
        }

        foreach (var row in slots.Select(x => x.Row).Distinct())
        {
            var item = Items[row];

            for (var col = minColumn; col <= maxColumn; col++)
            {
                if (Columns.VisibleColumns[col] is not TableViewColumn column ||
                   !slots.Contains(new TableViewCellSlot(row, col)))
                {
                    stringBuilder.Append(separator);
                    continue;
                }

                var content = isClipboardContent ? column.GetClipboardContent(item) : column.GetCellContent(item);
                stringBuilder.Append($"{content}{separator}");
            }

            stringBuilder.Remove(stringBuilder.Length - 1, 1); // remove extra separator at the end of the line
            stringBuilder.Append('\n');
        }

        stringBuilder.Remove(stringBuilder.Length - 1, 1); // remove extra line at the end

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Returns all headers content as a string with values separated by the given character.
    /// </summary>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <param name="minColumn">Min index column.</param>
    /// <param name="maxColumn">Max index column.</param>
    /// <returns>A string of all headers content separated by the specified character.</returns>
    private string GetHeadersContent(char separator, int minColumn, int maxColumn)
    {
        var stringBuilder = new StringBuilder();
        for (var col = minColumn; col <= maxColumn; col++)
        {
            var column = Columns.VisibleColumns[col];
            stringBuilder.Append($"{column.Header}{separator}");
        }

        stringBuilder.Remove(stringBuilder.Length - 1, 1); // remove extra separator at the end of the line

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Generates columns based on the types of the properties of the ItemsSource collection type.
    /// </summary>
    private void GenerateColumns()
    {
        if (ItemsSource is not IEnumerable source) return;

        var dataType = source?.GetItemType();
        if (dataType is null || dataType.IsPrimitive())
        {
            var columnArgs = GenerateColumn(dataType, null, "", dataType?.IsInheritedFromIComparable() is true);
            OnAutoGeneratingColumn(columnArgs);

            if (!columnArgs.Cancel && columnArgs.Column is not null)
            {
                Columns.Insert(Columns.Count, columnArgs.Column);
            }
        }
        else
        {
            foreach (var propertyInfo in dataType.GetProperties())
            {
                var displayAttribute = propertyInfo.GetCustomAttributes().OfType<DisplayAttribute>().FirstOrDefault();
                var autoGenerateField = displayAttribute?.GetAutoGenerateField();
                if (autoGenerateField == false)
                {
                    continue;
                }

                var header = displayAttribute?.GetShortName() ?? propertyInfo.Name;
                var canFilter = displayAttribute?.GetAutoGenerateFilter() is true or null;
                var columnArgs = GenerateColumn(propertyInfo.PropertyType, propertyInfo.Name, header, canFilter);
                OnAutoGeneratingColumn(columnArgs);

                if (!columnArgs.Cancel && columnArgs.Column is not null)
                {
                    columnArgs.Column.Order = displayAttribute?.GetOrder();
                    Columns.Add(columnArgs.Column);
                }
            }
        }
    }

    /// <summary>
    /// Generates a column based on the property type.
    /// </summary>
    private static TableViewAutoGeneratingColumnEventArgs GenerateColumn(Type? propertyType, string? propertyName, string header, bool canFilter)
    {
        var newColumn = GetTableViewColumnFromType(propertyName, propertyType);
        newColumn.Header = header;
        newColumn.CanFilter = canFilter;
        newColumn.IsAutoGenerated = true;

        return new TableViewAutoGeneratingColumnEventArgs(propertyName!, propertyType, newColumn);
    }

    /// <summary>
    /// Gets a TableViewColumn based on the property type.
    /// </summary>
    private static TableViewBoundColumn GetTableViewColumnFromType(string? propertyName, Type? type)
    {
        var binding = new Binding { Path = new PropertyPath(propertyName), Mode = BindingMode.TwoWay };
        TableViewBoundColumn column = new TableViewTextColumn { Binding = binding };

        if (type is null)
        {
            return column;
        }
        else if (type.IsTimeSpan() || type.IsTimeOnly())
        {
            column = new TableViewTimeColumn();
        }
        else if (type.IsDateOnly() || type.IsDateTime() || type.IsDateTimeOffset())
        {
            column = new TableViewDateColumn();
        }
        else if (type.IsNumeric())
        {
            column = new TableViewNumberColumn();
        }
        else if (type.IsBoolean())
        {
            column = new TableViewCheckBoxColumn();
        }
        else if (type.IsUri())
        {
            column = new TableViewHyperlinkColumn();
        }

        column.Binding = binding;

        return column;
    }

    /// <summary>
    /// Handles the ItemsSource property changed event.
    /// </summary>
    private void ItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        DetailsPaneStates.Clear();

        using var defer = _collectionView.DeferRefresh();
        _collectionView.Source = null!;

        if (e.NewValue is IEnumerable source)
        {
            EnsureAutoColumns();

            if (!_isItemsSourceSuspended)
            {
                _collectionView.Source = source;
            }
        }
    }

    /// <summary>
    /// Ensures that columns are automatically generated based on the current state of the control.
    /// </summary>
    private void EnsureAutoColumns(bool force = false)
    {
        if ((_ensureColumns || force) && IsLoaded && AutoGenerateColumns && ItemsSource is not null)
        {
            RemoveAutoGeneratedColumns();
            GenerateColumns();

            _ensureColumns = false;
        }
    }

    /// <summary>
    /// Removes auto-generated columns.
    /// </summary>
    private void RemoveAutoGeneratedColumns()
    {
        Columns.RemoveWhere(x => x.IsAutoGenerated);
    }

    /// <summary>
    /// Exports the selected rows or cells content to a CSV file.
    /// </summary>
    internal async void ExportSelectedToCSV()
    {
        var args = new TableViewExportContentEventArgs();
        OnExportSelectedContent(args);

        if (args.Handled)
        {
            return;
        }

        try
        {
            if (await GetStorageFile() is not { } file)
            {
                return;
            }

            var content = GetSelectedContent(true, ',');
            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);

            using var tw = new StreamWriter(stream);
            await tw.WriteAsync(content);
        }
        catch { }
    }

    /// <summary>
    /// Exports all rows content to a CSV file.
    /// </summary>
    internal async void ExportAllToCSV()
    {
        var args = new TableViewExportContentEventArgs();
        OnExportAllContent(args);

        if (args.Handled)
        {
            return;
        }

        try
        {
            if (await GetStorageFile() is not { } file)
            {
                return;
            }

            var content = GetAllContent(true, ',');
            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);

            using var tw = new StreamWriter(stream);
            await tw.WriteAsync(content);
        }
        catch { }
    }

    /// <summary>
    /// Gets a storage file for saving the CSV.
    /// </summary>
    private
#if !WINDOWS
    static
#endif
    async Task<StorageFile> GetStorageFile()
    {
        var savePicker = new FileSavePicker();
        savePicker.FileTypeChoices.Add("CSV (Comma delimited)", [".csv"]);
#if WINDOWS
        var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
#endif

        return await savePicker.PickSaveFileAsync();
    }

    /// <summary>
    /// Refreshes the items view of the TableView.
    /// </summary>
    public void RefreshView()
    {
        DeselectAll();
        _collectionView.Refresh();
    }

    /// <summary>
    /// Refreshes the sorting applied to the items in the TableView.
    /// </summary>
    public void RefreshSorting()
    {
        DeselectAll();
        _collectionView.RefreshSorting();
    }

    /// <summary>
    /// Clears all sorting applied to the items.
    /// </summary>
    public void ClearAllSorting()
    {
        DeselectAll();
        SortDescriptions.Clear();

        foreach (var column in Columns.Where(c => c.SortDirection is not null))
        {
            // A grouped column's SortDirection mirrors its group's own order, not an independent sort -
            // sorting a different column shouldn't strip its indicator or desync it from the group.
            if (_collectionView.GroupDescriptions.Any(x => x is ColumnGroupDescription columnGroup && columnGroup.Column == column))
            {
                continue;
            }
            column?.SortDirection = null;
        }
    }

    /// <summary>
    /// Clears all sorting applied to the items with event.
    /// </summary>
    internal void ClearAllSortingWithEvent()
    {
        var eventArgs = new TableViewClearSortingEventArgs();
        OnClearSorting(eventArgs);

        if (eventArgs.Handled)
        {
            return;
        }

        ClearAllSorting();
    }

    /// <summary>
    /// Clears all filters applied to the items.
    /// </summary>
    public void ClearAllFilters()
    {
        FilterHandler.ClearFilter(null);
    }

    /// <summary>
    /// Removes all grouping applied to the items.
    /// </summary>
    public void UngroupAll()
    {
        var groupedColumns = _collectionView.GroupDescriptions.OfType<ColumnGroupDescription>().Select(x => x.Column).ToList();

        foreach (var column in groupedColumns)
        {
            column.SortDirection = null;
        }

        _collectionView.GroupDescriptions.Clear();

        // A column can still have an independent ColumnSortDescription mirroring its group's order (see
        // HasGroupSortCompanion) - without removing it too, the view would stay sorted by that column even
        // though its SortDirection indicator above was just cleared.
        _collectionView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && groupedColumns.Contains(columnSort.Column));
    }

    /// <summary>
    /// Refreshes the grouping applied to the items in the TableView.
    /// </summary>
    public void RefreshGrouping()
    {
        DeselectAll();
        _collectionView.RefreshGrouping();
    }

    /// <summary>
    /// Refreshes all applied filters.
    /// </summary>
    public void RefreshFilter()
    {
        DeselectAll();
        _collectionView.RefreshFilter();
    }

    /// <summary>
    /// Selects all rows or cells in the TableView, as <kbd>Ctrl</kbd>+<kbd>A</kbd> does, according to
    /// <see cref="SelectionUnit"/>.
    /// </summary>
    public void SelectAll()
    {
        if (IsEditing)
        {
            return;
        }

        if (SelectionUnit is TableViewSelectionUnit.Cell)
        {
            SelectAllCells();
            CurrentCellSlot = null;
        }
        else
        {
            switch (SelectionMode)
            {
                case ListViewSelectionMode.Single:
                    SelectedItem = Items.FirstOrDefault();
                    break;
                case ListViewSelectionMode.Multiple:
                case ListViewSelectionMode.Extended:
                    if (Items.Count > 0)
                    {
                        SelectRange(new ItemIndexRange(0, (uint)Items.Count));
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Selects all cells in the TableView.
    /// </summary>
    private void SelectAllCells()
    {
        switch (SelectionMode)
        {
            case ListViewSelectionMode.Single:
                if (Items.Count > 0 && Columns.VisibleColumns.Count > 0)
                {
                    SelectedCellRanges.Clear();
                    SelectedCellRanges.Add(TableViewCellSlotRange.FromSlots(new(0, 0)));
                }
                break;
            case ListViewSelectionMode.Multiple:
            case ListViewSelectionMode.Extended:
                SelectedCellRanges.Clear();

                if (Items.Count > 0 && Columns.VisibleColumns.Count > 0)
                {
                    // One rectangle covers the whole grid. The previous implementation built a set of every
                    // individual slot and passed it to SelectedCellRanges.Add, which bound to the
                    // CollectionExtensions.Add(this IEnumerable, object) extension and silently dropped it —
                    // so selecting all cells actually selected nothing.
                    SelectedCellRanges.Add(new TableViewCellSlotRange(0, 0, Items.Count, Columns.VisibleColumns.Count));
                }
                break;
        }

        OnCellSelectionChanged();
    }

    /// <summary>
    /// Deselects all rows or cells in the TableView.
    /// </summary>
    public void DeselectAll()
    {
        DeselectAllItems();
        DeselectAllCells();
    }

    /// <summary>
    /// Deselects all rows in the TableView.
    /// </summary>
    private void DeselectAllItems()
    {
        ChangeSelection(static model => model.Clear());
    }

    /// <summary>
    /// Deselects all cells in the TableView.
    /// </summary>
    private void DeselectAllCells()
    {
        if (SelectedCellRanges.Count > 0)
        {
            SelectedCellRanges.Clear();
            OnCellSelectionChanged();
        }

        CurrentCellSlot = null;
    }

    /// <summary>
    /// Selects a row or cell based on the specified cell slot.
    /// </summary>
    internal void MakeSelection(TableViewCellSlot slot, bool shiftKey, bool ctrlKey = false)
    {
        if (!slot.IsValidRow(this))
        {
            return;
        }

        if (SelectionMode != ListViewSelectionMode.None)
        {
            ctrlKey = ctrlKey || SelectionMode is ListViewSelectionMode.Multiple;
            _suppressSelectionChangedCellClear = SelectionUnit is TableViewSelectionUnit.CellWithRow;
            var shouldSelectRows = SelectionUnit is TableViewSelectionUnit.Row
                || (SelectionUnit is TableViewSelectionUnit.CellWithRow && !slot.IsValidColumn(this))
                || (LastSelectionUnit is TableViewSelectionUnit.Row && slot.IsValidRow(this) && !slot.IsValidColumn(this))
                || (SelectionUnit is TableViewSelectionUnit.CellOrRow && slot.IsValidRow(this) && !slot.IsValidColumn(this));

            if (shouldSelectRows)
            {
                if (!ctrlKey)
                    DeselectAllCells();
                SelectRows(slot, shiftKey, ctrlKey);
                LastSelectionUnit = TableViewSelectionUnit.Row;
            }
            else
            {
                if (SelectionUnit is TableViewSelectionUnit.CellWithRow)
                {
                    SelectRows(slot, shiftKey, ctrlKey);
                }
                else if (!ctrlKey)
                {
                    DeselectAllItems();
                }

                SelectCells(slot, shiftKey, ctrlKey);
                LastSelectionUnit = TableViewSelectionUnit.Cell;
            }
        }
        else if (!IsReadOnly)
        {
            SelectionStartCellSlot = slot;
            CurrentCellSlot = slot;
        }
    }

    /// <summary>
    /// Selects rows based on the specified cell slot.
    /// </summary>
    private void SelectRows(TableViewCellSlot slot, bool shiftKey, bool ctrlKey)
    {
        var selectionRange = _rowSelection.GetRangeContaining(slot.Row);
        SelectionStartRowIndex ??= slot.Row;

        if (selectionRange is not null && ctrlKey && !shiftKey && (CurrentRowIndex != slot.Row || CurrentCellSlot == slot))
        {
            DeselectRange(new ItemIndexRange(slot.Row, 1));
        }
        else if ((!shiftKey && !ctrlKey && _rowSelection.Count <= 1) || SelectionMode is ListViewSelectionMode.Single)
        {
            SelectionStartRowIndex = CurrentRowIndex = SelectedIndex = slot.Row;
        }
        else if ((!ctrlKey && !shiftKey) || !(SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended))
        {
            SelectionStartRowIndex = CurrentRowIndex = SelectedIndex = slot.Row;
        }
        else if (SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
        {
            var min = Math.Min(SelectionStartRowIndex.Value, slot.Row);
            var max = Math.Max(SelectionStartRowIndex.Value, slot.Row);
            var newSelection = new ItemIndexRange(min, (uint)(max - min) + 1);

            if (!ctrlKey && newSelection.Length == 1)
            {
                SelectionStartRowIndex = CurrentRowIndex = SelectedIndex = slot.Row;
            }

            if (!ctrlKey)
            {
                // Without Ctrl the selection becomes exactly the anchor-to-target range, wherever the target is:
                // a Shift+Click can land far from the previous range, on either side of the anchor.
                foreach (var range in SelectedRanges.ToArray())
                {
                    if (range.LastIndex < newSelection.FirstIndex || range.FirstIndex > newSelection.LastIndex)
                    {
                        DeselectRange(range);
                        continue;
                    }

                    foreach (var outside in range.Subtract(newSelection))
                    {
                        DeselectRange(outside);
                    }
                }

                SelectRange(newSelection);
            }
            else if (SelectionMode is ListViewSelectionMode.Extended)
            {
                // Ctrl+Shift only ever adds the anchor-to-target range; rows that are already selected stay selected.
                SelectRange(newSelection);
            }
            else if (selectionRange?.LastIndex > newSelection.LastIndex)
            {
                var deselectRange = new ItemIndexRange(newSelection.LastIndex + 1, (uint)(selectionRange.LastIndex - newSelection.LastIndex));
                DeselectRange(deselectRange);
            }
            else if (selectionRange?.FirstIndex < newSelection.FirstIndex)
            {
                var deselectRange = new ItemIndexRange(selectionRange.FirstIndex, (uint)(newSelection.FirstIndex - selectionRange.FirstIndex));
                DeselectRange(deselectRange);
            }
            else if (selectionRange != newSelection)
            {
                SelectRange(newSelection);
            }

            // The target becomes the current row, so keyboard navigation continues from it; the anchor stays put.
            CurrentRowIndex = slot.Row;
        }

        if (!IsReadOnly && slot.IsValid(this))
        {
            CurrentCellSlot = slot;
        }
        else
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                var row = await ScrollRowIntoView(slot.Row);
                row?.Focus(FocusState.Programmatic);
            });
        }
    }

    /// <summary>
    /// Selects cells based on the specified cell slot.
    /// </summary>
    private void SelectCells(TableViewCellSlot slot, bool shiftKey, bool ctrlKey)
    {
        if (!slot.IsValid(this))
        {
            return;
        }

        if (!ctrlKey || !(SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended))
        {
            if (SelectionUnit is TableViewSelectionUnit.CellWithRow)
            {
                DeselectAllCells();
            }
            else
            {
                DeselectAll();
            }
        }

        var selectionRange = (SelectionStartCellSlot is null ? null : SelectedCellRanges.LastOrDefault(x => SelectionStartCellSlot.HasValue && x.Contains(SelectionStartCellSlot.Value.Row, SelectionStartCellSlot.Value.Column)));

        if (ctrlKey && SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
        {
            // Keep existing ranges; the new slot/range will be added alongside them.
        }
        else
        {
            SelectedCellRanges.Remove(selectionRange!);
        }

        SelectionStartCellSlot ??= CurrentCellSlot;
        SelectionStartCellSlot ??= slot;

        var extendsFromAnchor = shiftKey && SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended;
        var addedRange = extendsFromAnchor
            ? TableViewCellSlotRange.FromSlots(SelectionStartCellSlot.Value, slot)
            : TableViewCellSlotRange.FromSlots(slot);

        if (!extendsFromAnchor)
        {
            SelectionStartCellSlot = slot;
        }

        // Keep SelectedCellRanges disjoint so the slot count projected from it stays exact.
        SubtractCellRangeFromSelection(addedRange);
        SelectedCellRanges.Add(addedRange);
        OnCellSelectionChanged();
        CurrentCellSlot = slot;
    }

    /// <summary>
    /// Deselects the specified cell slot.
    /// </summary>
    internal void DeselectCell(TableViewCellSlot slot)
    {
        var singleCellRange = TableViewCellSlotRange.FromSlots(slot);
        var containingRanges = SelectedCellRanges.Where(x => x.Contains(slot.Row, slot.Column)).ToList();

        foreach (var range in containingRanges)
        {
            SelectedCellRanges.Remove(range);
            foreach (var remaining in range.Subtract(singleCellRange))
            {
                SelectedCellRanges.Add(remaining);
            }
        }

        CurrentCellSlot = slot;
        OnCellSelectionChanged();
    }

    /// <summary>
    /// Selects all the cells within the specified range, raising the <see cref="CellSelectionChanged"/> event only once.
    /// </summary>
    /// <param name="range">The range of cell slots to select.</param>
    public void SelectCellRange(TableViewCellSlotRange? range)
    {
        if (range is null || range.Length <= 0
            || !range.IsValid(this)
            || SelectionMode is ListViewSelectionMode.None
            || SelectionUnit is TableViewSelectionUnit.Row)
        {
            return;
        }

        if (SelectedCellRanges.Any(x => x == range)) return;

        if (SelectionUnit is TableViewSelectionUnit.CellWithRow)
        {
            _suppressSelectionChangedCellClear = true;
            var rowRange = new ItemIndexRange(range.FirstRow, (uint)range.Rows);
            SelectRange(rowRange);
        }

        SubtractCellRangeFromSelection(range);
        SelectedCellRanges.Add(range);
        OnCellSelectionChanged();
    }

    /// <summary>
    /// Deselects all the cells within the specified range, raising the <see cref="CellSelectionChanged"/> event only once.
    /// </summary>
    /// <param name="range">The range of cell slots to deselect.</param>
    public void DeselectCellRange(TableViewCellSlotRange? range)
    {
        if (range is null || range.Length <= 0 || SelectedCellRanges.Count is 0)
        {
            return;
        }

        SubtractCellRangeFromSelection(range);
        OnCellSelectionChanged();
    }

    /// <summary>
    /// Handles changes to the current cell in the table view.
    /// </summary>
    private async Task OnCurrentCellChanged(TableViewCellSlot? oldSlot, TableViewCellSlot? newSlot)
    {
        if (oldSlot == newSlot)
        {
            return;
        }

        // This runs from CurrentCellSlot's DependencyProperty changed callback, which fires synchronously
        // as part of CurrentCellSlot's own SetValue call. Without yielding first, everything below - down
        // through ScrollCellIntoView's native ScrollIntoView call - executes re-entrantly on that same
        // native SetValue call stack, which crashes the process outright (an unhandled native exception,
        // not a catchable .NET one). Yielding lets that SetValue call fully unwind before touching XAML's
        // scroll/layout machinery again.
        await Task.Yield();

        if (oldSlot.HasValue)
        {
            var cell = GetCellFromSlot(oldSlot.Value);
            cell?.ApplyCurrentCellState();
        }

        if (newSlot.HasValue)
        {
            var cell = await ScrollCellIntoView(newSlot.Value);
            cell?.ApplyCurrentCellState();
            cell?.Focus(FocusState.Programmatic);
        }
    }

    /// <summary>
    /// Handles cell selection changes.
    /// </summary>
    private void OnCellSelectionChanged()
    {
        // Diff rectangles, not slots. The previous implementation expanded every selected range into a set of
        // individual slots on every change, which is rows x columns entries for a whole-grid selection.
        var currentRanges = SelectedCellRanges.Count is 0 ? [] : new List<TableViewCellSlotRange>(SelectedCellRanges);
        var removedRanges = SubtractRangeSet(_previousSelectedCellRanges, currentRanges);
        var addedRanges = SubtractRangeSet(currentRanges, _previousSelectedCellRanges);

        if (removedRanges.Count is 0 && addedRanges.Count is 0) return;

        _previousSelectedCellRanges = currentRanges;

        OnCellSelectionChanged(new TableViewCellSelectionChangedEventArgs(
            new TableViewCellSlotCollection(removedRanges),
            new TableViewCellSlotCollection(addedRanges)));

        MarkCellStatesDirty(removedRanges);
        MarkCellStatesDirty(addedRanges);

        if (!_cellStateDispatchPending)
        {
            _cellStateDispatchPending = true;
            DispatcherQueue.TryEnqueue(ApplyPendingCellStates);
        }
    }

    /// <summary>
    /// Returns the parts of <paramref name="source"/> that <paramref name="subtract"/> does not cover.
    /// </summary>
    private static List<TableViewCellSlotRange> SubtractRangeSet(List<TableViewCellSlotRange> source,
                                                                List<TableViewCellSlotRange> subtract)
    {
        if (source.Count is 0) return [];

        List<TableViewCellSlotRange> result = [];

        foreach (var range in source)
        {
            result.AddRange(range.SubtractAll(subtract));
        }

        return result;
    }

    /// <summary>
    /// Records which rows need their cells' selection visuals refreshed. Beyond a bounded number of rows the
    /// individual row indexes stop being worth tracking — only realized rows are ever repainted, so a
    /// "refresh everything realized" flag is both smaller and cheaper than a set of a million row indexes.
    /// </summary>
    private void MarkCellStatesDirty(List<TableViewCellSlotRange> ranges)
    {
        const int maxTrackedRows = 256;

        foreach (var range in ranges)
        {
            if (_allCellStatesPending) return;

            if (range.Rows > maxTrackedRows || _pendingCellStateRows.Count + range.Rows > maxTrackedRows)
            {
                _allCellStatesPending = true;
                _pendingCellStateRows.Clear();
                return;
            }

            for (var row = range.FirstRow; row <= range.LastRow; row++)
            {
                _pendingCellStateRows.Add(row);
            }
        }
    }

    private void ApplyPendingCellStates()
    {
        _cellStateDispatchPending = false;
        if (!_allCellStatesPending && _pendingCellStateRows.Count is 0) return;

        foreach (var row in _rows)
        {
            if (_allCellStatesPending || _pendingCellStateRows.Contains(row.Index))
                row.ApplyCellsSelectionState();
        }

        _pendingCellStateRows.Clear();
        _allCellStatesPending = false;
    }

    /// <summary>
    /// Starts drag selection tracking, auto-scroll, and optionally the drag rectangle visual.
    /// </summary>
    /// <param name="startPoint">The starting point relative to the drag rectangle canvas.</param>
    internal void StartDragSelection(Point startPoint)
    {
        if (SelectionMode is not (ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended))
        {
            return;
        }

        // Guard against re-entry (e.g., multi-touch) to prevent double ViewChanged subscription
        if (IsDragSelecting)
        {
            EndDragSelection();
        }

        IsDragSelecting = true;
        _lastDragCanvasPoint = startPoint;
        _dragStartVerticalOffset = _scrollViewer?.VerticalOffset ?? 0;
        _dragStartHorizontalOffset = HorizontalOffset;

        _scrollViewer?.ViewChanged += OnScrollViewerViewChangedDuringDrag;

        // Show the drag rectangle visual if enabled and template parts are available
        if (DragRectangleCanvas is not null && _dragRectangle is not null)
        {
            _dragStartPoint = startPoint;

            Canvas.SetLeft(_dragRectangle, startPoint.X);
            Canvas.SetTop(_dragRectangle, startPoint.Y);
            _dragRectangle.Width = 0;
            _dragRectangle.Height = 0;

            _dragRectangle.Visibility = ShowDragRectangle ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Updates the drag visual and auto-scroll during drag selection.
    /// </summary>
    /// <param name="currentPoint">The current pointer position relative to the drag rectangle canvas.</param>
    internal void UpdateDragRectangleVisual(Point currentPoint)
    {
        if (!IsDragSelecting)
        {
            return;
        }

        _lastDragCanvasPoint = currentPoint;

        // Update the rectangle visual if it's active
        if (_dragStartPoint is not null && DragRectangleCanvas is not null && _dragRectangle is not null)
        {
            PositionDragRectangle(currentPoint);
        }

        UpdateAutoScroll(currentPoint);
    }

    /// <summary>
    /// Transforms a point relative to this <see cref="TableView"/> into coordinates relative to the <see cref="DragRectangleCanvas"/>.
    /// Returns <c>null</c> when the canvas is unavailable or the transform cannot be computed.
    /// A negative Y value indicates the point is above the scroll area (column header territory).
    /// </summary>
    /// <param name="position">The position relative to this TableView.</param>
    /// <returns>The canvas-relative point, or <c>null</c> if unavailable.</returns>
    private Point? GetCanvasPoint(Point position)
    {
        if (DragRectangleCanvas is null) return null;
        try
        {
            return TransformToVisual(DragRectangleCanvas).TransformPoint(position);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Positions the drag rectangle visual from the scroll-adjusted start point to the current point,
    /// so the rectangle follows the mouse and extends naturally when content scrolls.
    /// </summary>
    private void PositionDragRectangle(Point currentPoint)
    {
        if (_dragStartPoint is null || DragRectangleCanvas is null || _dragRectangle is null) return;

        // Adjust the start point by how much the view has scrolled since drag began.
        // This makes the rectangle extend naturally as content scrolls.
        var verticalScrollDelta = (_scrollViewer?.VerticalOffset ?? 0) - _dragStartVerticalOffset;
        var horizontalScrollDelta = HorizontalOffset - _dragStartHorizontalOffset;
        var adjustedStartY = _dragStartPoint.Value.Y - verticalScrollDelta;
        var adjustedStartX = _dragStartPoint.Value.X - horizontalScrollDelta;

        var canvasWidth = DragRectangleCanvas.ActualWidth;
        var canvasHeight = DragRectangleCanvas.ActualHeight;

        var left = Math.Max(0, Math.Min(adjustedStartX, currentPoint.X));
        var top = Math.Max(0, Math.Min(adjustedStartY, currentPoint.Y));
        var right = Math.Min(canvasWidth, Math.Max(adjustedStartX, currentPoint.X));
        var bottom = Math.Min(canvasHeight, Math.Max(adjustedStartY, currentPoint.Y));

        Canvas.SetLeft(_dragRectangle, left);
        Canvas.SetTop(_dragRectangle, top);
        _dragRectangle.Width = Math.Max(0, right - left);
        _dragRectangle.Height = Math.Max(0, bottom - top);
    }

    /// <summary>
    /// Manages auto-scroll behavior when the pointer is near the top or bottom edge during drag selection.
    /// </summary>
    private void UpdateAutoScroll(Point canvasPoint)
    {
        if (_scrollViewer is null) return;

        const double edgeThreshold = 0;
        const double maxScrollSpeed = 20;

        var viewportHeight = _scrollViewer.ViewportHeight;
        var viewportWidth = _scrollViewer.ViewportWidth;
        double vDelta = 0;
        double hDelta = 0;

        if (canvasPoint.Y > viewportHeight - edgeThreshold)
        {
            var proximity = Math.Min(1.0, (canvasPoint.Y - (viewportHeight - edgeThreshold)) / edgeThreshold);
            vDelta = proximity * maxScrollSpeed;
        }
        else if (canvasPoint.Y < edgeThreshold)
        {
            var proximity = Math.Min(1.0, (edgeThreshold - canvasPoint.Y) / edgeThreshold);
            vDelta = -(proximity * maxScrollSpeed);
        }

        if (canvasPoint.X > viewportWidth - edgeThreshold)
        {
            var proximity = Math.Min(1.0, (canvasPoint.X - (viewportWidth - edgeThreshold)) / edgeThreshold);
            hDelta = proximity * maxScrollSpeed;
        }
        else if (canvasPoint.X < edgeThreshold)
        {
            var proximity = Math.Min(1.0, (edgeThreshold - canvasPoint.X) / edgeThreshold);
            hDelta = -(proximity * maxScrollSpeed);
        }

        if (Math.Abs(vDelta) > 0.5 || Math.Abs(hDelta) > 0.5)
        {
            _autoScrollVerticalDelta = vDelta;
            _autoScrollHorizontalDelta = hDelta;
            if (_autoScrollTimer is null)
            {
                _autoScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                _autoScrollTimer.Tick += OnAutoScrollTimerTick;
                _autoScrollTimer.Start();
            }
            // else: timer already running — delta values above are picked up on the next tick
        }
        else
        {
            StopAutoScroll();
        }
    }

    /// <summary>
    /// Handles the auto-scroll timer tick to scroll the view and update drag selection.
    /// </summary>
    private void OnAutoScrollTimerTick(object? sender, object e)
    {
        if (!IsDragSelecting || _scrollViewer is null)
        {
            StopAutoScroll();
            return;
        }

        var scrolled = false;

        // Vertical auto-scroll via ChangeView
        if (Math.Abs(_autoScrollVerticalDelta) > 0.5)
        {
            var newOffset = Math.Clamp(
                _scrollViewer.VerticalOffset + _autoScrollVerticalDelta,
                0,
                _scrollViewer.ScrollableHeight);

            if (Math.Abs(newOffset - _scrollViewer.VerticalOffset) >= 0.5)
            {
                _scrollViewer.ChangeView(null, newOffset, null, true);
                scrolled = true;
            }
        }

        // Horizontal auto-scroll via HorizontalOffset DP
        if (Math.Abs(_autoScrollHorizontalDelta) > 0.5)
        {
            var newOffset = Math.Clamp(
                HorizontalOffset + _autoScrollHorizontalDelta,
                0,
                _scrollViewer.ScrollableWidth);

            if (Math.Abs(newOffset - HorizontalOffset) >= 0.5)
            {
                SetValue(HorizontalOffsetProperty, newOffset);
                scrolled = true;
            }
        }

        if (!scrolled)
        {
            StopAutoScroll();
            return;
        }

        // Horizontal scroll does not fire ViewChanged, so reposition the rectangle here.
        // Selection is updated for all scroll directions from the timer tick, not from ViewChanged,
        // so that MakeSelectionInDragRect runs after ChangeView completes rather than inside the layout pass.
        if (_lastDragCanvasPoint is not null)
        {
            if (Math.Abs(_autoScrollHorizontalDelta) > 0.5 &&
                _dragStartPoint is not null && DragRectangleCanvas is not null && _dragRectangle is not null)
            {
                PositionDragRectangle(_lastDragCanvasPoint.Value);
            }

            if (_tableViewDragPointer is not null)
            {
                MakeSelectionInDragRect();
            }
        }
    }

    /// <summary>
    /// Stops the auto-scroll timer.
    /// </summary>
    private void StopAutoScroll()
    {
        if (_autoScrollTimer is not null)
        {
            _autoScrollTimer.Stop();
            _autoScrollTimer.Tick -= OnAutoScrollTimerTick;
            _autoScrollTimer = null;
        }
    }

    /// <summary>
    /// Handles ScrollViewer.ViewChanged during drag to re-evaluate selection when scroll position changes.
    /// </summary>
    private void OnScrollViewerViewChangedDuringDrag(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!IsDragSelecting || _lastDragCanvasPoint is null) return;

        // Reposition the rectangle using scroll-adjusted start point (if rectangle is active)
        if (_dragStartPoint is not null && DragRectangleCanvas is not null && _dragRectangle is not null)
        {
            PositionDragRectangle(_lastDragCanvasPoint.Value);
        }

        // During auto-scroll the timer tick owns selection updates to keep MakeSelectionInDragRect
        // out of the scroll layout pass. Only update here for non-auto-scroll scrolls (e.g. scroll wheel).
        if (_autoScrollTimer is null && _tableViewDragPointer is not null)
        {
            MakeSelectionInDragRect();
        }
    }

    /// <summary>
    /// Ends drag selection tracking, auto-scroll, and hides the drag rectangle if visible.
    /// </summary>
    internal async void EndDragSelection()
    {
        if (!IsDragSelecting || _lastDragCanvasPoint is null) return;

        StopAutoScroll();

        _pointerCaptureElement?.ReleasePointerCaptures();
        _pointerCaptureElement = null;
        _tableViewDragPointer = null;

        _scrollViewer?.ViewChanged -= OnScrollViewerViewChangedDuringDrag;
        _dragRectangle?.Visibility = Visibility.Collapsed;

        var slot = GetSlotAtCanvasPoint(_lastDragCanvasPoint);
        SetCurrentCell(slot);

        IsDragSelecting = false;
        _dragStartPoint = null;
        _lastDragCanvasPoint = null;
        SelectionStartCellSlot = null;

#if !WINDOWS
        if (_dragStartCell is not null && slot != _dragStartCell.Slot)
        {
            VisualStates.GoToState(_dragStartCell, false, VisualStates.StateNormal);

            if (_dragStartCell.IsSelected)
            {
                VisualStates.GoToState(_dragStartCell, false, VisualStates.StateSelected);
            }
        }

        if (_dragStartRow is not null && _dragStartRow.Index != slot?.Row)
        {
            VisualStates.GoToState(_dragStartRow, false, VisualStates.StateNormal);

            if (_dragStartRow.IsSelected)
            {
                VisualStates.GoToState(_dragStartRow, false, VisualStates.StateSelected);
            }
        }
#endif
    }

    private void SetCurrentCell(TableViewCellSlot? slot)
    {
        if (slot is null) return;

        CurrentRowIndex = slot.Value.Row;

        if (!(SelectionUnit is TableViewSelectionUnit.Row && IsReadOnly))
        {
            CurrentCellSlot = slot;

        }
    }

    /// <summary>
    /// Scrolls the specified cell slot into view.
    /// </summary>
    /// <param name="slot">The cell slot to scroll into view.</param>
    public async Task<TableViewCell> ScrollCellIntoView(TableViewCellSlot slot)
    {
        if (_scrollViewer is null || !slot.IsValid(this) || await ScrollRowIntoView(slot.Row) is not { } row)
            return default!;

        var xOffset = 0d;
        var yOffset = _scrollViewer.VerticalOffset;

        // Calculate the left and right edge of the cell
        var cellLeft = Columns.VisibleColumns.Take(slot.Column).Sum(x => x.ActualWidth);
        var cellWidth = Columns.VisibleColumns[slot.Column].ActualWidth;
        var cellRight = cellLeft + cellWidth;
        var viewportLeft = HorizontalOffset;
        var headersOffset = CellsHorizontalOffset;
        var viewportRight = viewportLeft + _scrollViewer.ViewportWidth - headersOffset;

        // If cell is wider than the viewport, align left edge
        if (cellWidth > _scrollViewer.ViewportWidth - headersOffset)
        {
            xOffset = cellLeft;
        }
        // If cell is left of the viewport, scroll to its left edge
        else if (cellLeft < viewportLeft)
        {
            xOffset = cellLeft;
        }
        // If cell is right of the viewport, scroll so its right edge is visible
        else if (cellRight > viewportRight)
        {
            xOffset = cellRight - (_scrollViewer.ViewportWidth - headersOffset);
        }

        // If cell is fully in view, just return
        if ((cellLeft >= viewportLeft && cellRight <= viewportRight) ||
            xOffset == HorizontalOffset)
        {
            return row.Cells.ElementAt(slot.Column);
        }

        SetValue(HorizontalOffsetProperty, xOffset);

        return row?.Cells.ElementAt(slot.Column)!;
    }

    /// <summary>
    /// Scrolls the specified row into view.
    /// </summary>
    /// <param name="index">The index of the row to scroll into view.</param>
    /// <remarks>
    /// A realized row is scrolled to from where it is. Any other row is realized by the repeater where the layout
    /// estimates it to be and brought into view from there, which is how a layout that only knows the heights of the
    /// rows it has realized reaches a row far away.
    /// </remarks>
    public async Task<TableViewRow?> ScrollRowIntoView(int index)
    {
        if (_scrollViewer is null || _rowsRepeater is null || index < 0 || index >= _collectionView.Count)
        {
            return null;
        }

        var visualIndex = GetVisualIndexFromItemIndex(index);

        if (visualIndex < 0)
        {
            // The row is inside a collapsed group. Expanding it is the caller's decision, not ours.
            return null;
        }

        // A sticky group header covers the top of the viewport, so a row brought in from above has to stop just
        // below it rather than underneath it.
        var topInset = AreStickyGroupHeadersActive && FindGroupHeaderAtOrBefore(visualIndex) is >= 0 and var header && header != visualIndex
            ? GetStickyGroupHeaderHeight(header)
            : 0d;

        if (RealizedRows.Get(visualIndex) is TableViewRow realizedRow && RealizedRows.TryGetBounds(visualIndex, out var rowTop, out var rowHeight))
        {
            // The scroll viewer's content starts with the table's padding, above the rows.
            rowTop += Padding.Top;

            var viewportTop = _scrollViewer.VerticalOffset;
            var viewportHeight = _scrollViewer.ViewportHeight;
            var target = viewportTop;

            if (rowTop - topInset < viewportTop)
            {
                target = rowTop - topInset;
            }
            else if (rowTop + rowHeight > viewportTop + viewportHeight)
            {
                target = rowTop + rowHeight - viewportHeight;
            }

            if (Math.Abs(target - viewportTop) > 0.5)
            {
                _scrollViewer.ChangeView(null, Math.Clamp(target, 0d, _scrollViewer.ScrollableHeight), null, true);

                await Task.Yield();
            }

            return realizedRow;
        }

        var firstVisible = RealizedRows.FindAtOffset(RowsViewportTop, out _);
        var alignToTop = firstVisible < 0 || visualIndex < firstVisible;

#if WINDOWS
        // The repeater realizes the row where the layout estimates it to be and holds it there, realizing nothing
        // around it, until it is brought into view; it has to be laid out before it can be.
        if (_rowsRepeater.GetOrCreateElement(visualIndex) is not TableViewRow row)
        {
            return null;
        }

        row.UpdateLayout();

        var height = Math.Max(1d, row.ActualHeight);

        // Above, the target reaches up over the sticky header. It is a single point across, at the current horizontal
        // offset, so bringing it into view never scrolls the table sideways.
        row.StartBringIntoView(new BringIntoViewOptions
        {
            AnimationDesired = false,
            VerticalAlignmentRatio = alignToTop ? 0d : 1d,
            TargetRect = alignToTop
                ? new Rect(Math.Max(0d, _scrollViewer.HorizontalOffset - Padding.Left), -topInset, 1d, height + topInset)
                : new Rect(Math.Max(0d, _scrollViewer.HorizontalOffset - Padding.Left), 0d, 1d, height)
        });

        await Task.Yield();

        return row;
#else
        return await ScrollToUnrealizedRowAsync(visualIndex, alignToTop, topInset);
#endif
    }

#if !WINDOWS
    /// <summary>
    /// Scrolls to a row that is not realized, where Uno's repeater cannot be asked to realize it out of view.
    /// </summary>
    /// <remarks>
    /// Uno's repeater, like WinUI's, pins an element realized out of view at the origin, but its bring-into-view does
    /// not then move the scroll viewer to where the row belongs. So the scroll viewer is moved to where the realized
    /// rows suggest the row is, which realizes the rows there, and again from those until the row is realized and
    /// its real position can be scrolled to.
    /// </remarks>
    private async Task<TableViewRow?> ScrollToUnrealizedRowAsync(int visualIndex, bool alignToTop, double topInset)
    {
        if (_scrollViewer is null || _rowsRepeater is null)
        {
            return null;
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (RealizedRows.Get(visualIndex) is TableViewRow row && RealizedRows.TryGetBounds(visualIndex, out var rowTop, out var rowHeight))
            {
                rowTop += Padding.Top;

                var target = alignToTop ? rowTop - topInset : rowTop + rowHeight - _scrollViewer.ViewportHeight;

                if (Math.Abs(Math.Clamp(target, 0d, _scrollViewer.ScrollableHeight) - _scrollViewer.VerticalOffset) <= 0.5)
                {
                    return row;
                }

                // Rows measured on the way replace the estimated heights, so the row may not stay where it was seen;
                // look again after each move until it does.
                _scrollViewer.ChangeView(null, Math.Clamp(target, 0d, _scrollViewer.ScrollableHeight), null, true);

                await Task.Delay(32);

                continue;
            }

            var nearest = RealizedRows.FindAtOffset(RowsViewportTop, out _);

            if (nearest < 0 || !RealizedRows.TryGetBounds(nearest, out var nearestTop, out var nearestHeight))
            {
                // No row is realized yet, as the table has not been laid out, so there is nothing to go by.
                UpdateLayout();

                await Task.Delay(16);

                continue;
            }

            // Rows not realized are estimated to be as tall as the one the viewport starts at.
            var estimate = nearestTop + Padding.Top + ((visualIndex - nearest) * Math.Max(1d, nearestHeight));
            var estimatedTarget = alignToTop ? estimate - topInset : estimate + nearestHeight - _scrollViewer.ViewportHeight;

            _scrollViewer.ChangeView(null, Math.Clamp(estimatedTarget, 0d, _scrollViewer.ScrollableHeight), null, true);

            await Task.Delay(32);
        }

        return RealizedRows.Get(visualIndex) as TableViewRow;
    }
#endif

    /// <summary>
    /// Brings the given item into view.
    /// </summary>
    /// <param name="item">The item to scroll to.</param>
    public void ScrollIntoView(object? item)
    {
        var index = _collectionView.IndexOf(item);

        if (index >= 0)
        {
            _ = ScrollRowIntoView(index);
        }
    }

    /// <summary>
    /// Returns the first item index in the given visual row span, or -1 when the span holds only group headers.
    /// </summary>
    private int FirstItemIndexAtOrAfter(int firstVisualIndex, int lastVisualIndex)
    {
        var itemIndex = _visualRows?.FindItemIndexAtOrAfter(firstVisualIndex) ?? -1;

        return itemIndex >= 0 && GetVisualIndexFromItemIndex(itemIndex) <= lastVisualIndex ? itemIndex : -1;
    }

    /// <summary>
    /// Returns the last item index in the given visual row span, or -1 when the span holds only group headers.
    /// </summary>
    private int LastItemIndexAtOrBefore(int firstVisualIndex, int lastVisualIndex)
    {
        var itemIndex = _visualRows?.FindItemIndexAtOrBefore(lastVisualIndex) ?? -1;

        return itemIndex >= 0 && GetVisualIndexFromItemIndex(itemIndex) >= firstVisualIndex ? itemIndex : -1;
    }

    /// <summary>
    /// Gets the cell based on the specified cell slot.
    /// </summary>
    internal TableViewCell? GetCellFromSlot(TableViewCellSlot slot)
    {
        return slot.IsValid(this) && ContainerFromIndex(slot.Row) is TableViewRow row ? row.Cells[slot.Column] : default;
    }

    /// <summary>
    /// Returns the index of the realized row whose vertical bounds contain <paramref name="canvasPoint"/>.
    /// Returns <c>null</c> when no realized row contains the point.
    /// </summary>
    private int? GetRowIndexAtCanvasPoint(Point canvasPoint)
    {
        var visualIndex = GetVisualRowIndexAtCanvasY(canvasPoint.Y);

        if (visualIndex < 0)
        {
            return null;
        }

        var itemIndex = GetItemIndexFromVisualIndex(visualIndex);

        return itemIndex < 0 ? null : itemIndex;
    }

    /// <summary>
    /// Returns the visual row index at a Y coordinate in drag-canvas space, or -1 when outside the rows.
    /// </summary>
    /// <remarks>
    /// Read from the realized rows, which always include the rows under the pointer and under a drag rectangle,
    /// since both lie within the viewport. Below the last row there is no row; anywhere else short of an exact hit,
    /// such as between rows, the nearest one is returned.
    /// </remarks>
    internal int GetVisualRowIndexAtCanvasY(double canvasY)
    {
        if (_scrollViewer is null || VisualRowCount is 0 || canvasY < 0)
        {
            return -1;
        }

        var offset = canvasY + RowsViewportTop;
        var visualIndex = RealizedRows.FindAtOffset(offset, out var exact);

        if (!exact
            && visualIndex == VisualRowCount - 1
            && RealizedRows.TryGetBounds(visualIndex, out var top, out var height)
            && offset >= top + height)
        {
            return -1;
        }

        return visualIndex;
    }

    /// <summary>
    /// Returns the index of the visible column whose bounds contain the given canvas X coordinate.
    /// Returns <c>null</c> when x falls outside the column area or there are no visible columns.
    /// </summary>
    private int? GetColumnIndexAtCanvasX(double x)
    {
        var frozenCount = FrozenColumnCount;
        var columnLeft = CellsHorizontalOffset;
        var frozenPanelRight = CellsHorizontalOffset;

        for (var i = 0; i < Columns.VisibleColumns.Count; i++)
        {
            if (i == frozenCount)
            {
                frozenPanelRight = columnLeft;
                columnLeft -= HorizontalOffset;
            }

            var columnRight = columnLeft + Columns.VisibleColumns[i].ActualWidth;
            var effectiveLeft = i >= frozenCount ? Math.Max(columnLeft, frozenPanelRight) : columnLeft;

            if (x <= columnRight)
                return x < effectiveLeft ? null : i;

            columnLeft = columnRight;
        }

        return null;
    }

    /// <summary>
    /// Resolves the cell slot at <paramref name="canvasPoint"/>.
    /// Returns <c>null</c> when no realized row or visible column contains the point.
    /// </summary>
    private TableViewCellSlot? GetSlotAtCanvasPoint(Point? canvasPoint)
    {
        if (DragRectangleCanvas is null || canvasPoint is null) return null;

        if (GetRowIndexAtCanvasPoint(canvasPoint.Value) is not int rowIndex) return null;

        // Mirror the row snapping: find the nearest column within the horizontal drag span.
        var horizontalScrollDelta = HorizontalOffset - _dragStartHorizontalOffset;
        var adjustedPointerX = canvasPoint.Value.X - horizontalScrollDelta;
        var colIndex = GetColumnIndexAtCanvasX(adjustedPointerX);

        return colIndex is null ? null : new TableViewCellSlot(rowIndex, colIndex.Value);
    }

    /// <summary>
    /// Refreshes the header row and the realized rows after <see cref="SelectionMode"/> or
    /// <see cref="SelectionUnit"/> changed: the multi-select check box column, row header visibility and the
    /// horizontal scroll bar margin all depend on them.
    /// </summary>
    private void RefreshSelectionModeVisuals()
    {
        _headerRow?.SetHeadersVisibility();
        _headerRow?.SetCellsHorizontalOffset();
        UpdateHorizontalScrollBarMargin();

        // Pooled rows catch up when they are reused; see ApplyRowVisuals.
        _selectionModeVersion++;

        foreach (var row in _rows)
        {
            ApplySelectionModeLayout(row);
            row.RefreshVisualStates();
        }
    }

    /// <summary>
    /// Lays a row out for the table's selection mode, such as with room for its check box in multiple selection.
    /// </summary>
    private void ApplySelectionModeLayout(TableViewRow row)
    {
        row.SelectionModeVersion = _selectionModeVersion;
        row.EnsureLayout();
        row.RowPresenter?.SetRowHeaderVisibility();
    }

    /// <summary>
    /// Ensures grid lines are applied to the header row and body rows.
    /// </summary>
    private void EnsureGridLines()
    {
        _headerRow?.EnsureGridLines();
        _headerRow?.CalculateHeaderWidths();

        foreach (var row in _rows)
        {
            row.RowPresenter?.EnsureGridLines();
        }

        foreach (var groupHeaderRow in _groupHeaderRows)
        {
            groupHeaderRow.EnsureGridLines();
        }

        _stickyGroupHeader?.EnsureGridLines();
    }

    /// <summary>
    /// Ensures alternate row colors are applied.
    /// </summary>
    internal void EnsureAlternateRowColors()
    {
        if (_alternateRowColorsPending) return;

        _alternateRowColorsPending = true;
        DispatcherQueue.TryEnqueue(() =>
        {
            _alternateRowColorsPending = false;

            foreach (var row in _rows)
            {
                row.EnsureAlternateColors();
            }
        });
    }

    /// <summary>
    /// Resets the auto-calculated widths of the specified columns and recalculates them.
    /// </summary>
    /// <param name="columns">The columns to refresh. When null, all columns are refreshed.</param>
    internal void RefreshColumnsAutoWidth(IEnumerable<TableViewColumn>? columns = null)
    {
        var targetColumns = (columns ?? Columns).ToHashSet();
        if (targetColumns.Count == 0)
        {
            return;
        }

        foreach (var column in targetColumns)
        {
            column.DesiredWidth = 0d;
            column.HeaderControl?.InvalidateMeasure();
        }

        foreach (var row in _rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Column is { } cellColumn && targetColumns.Contains(cellColumn))
                {
                    cell.InvalidateAutoWidth();
                }
            }
        }

        DispatcherQueue.TryEnqueue(() => _headerRow?.CalculateHeaderWidths());
    }

    /// <summary>
    /// Ensures the column headers style is applied.
    /// </summary>
    private void EnsureColumnHeadersStyle()
    {
        foreach (var column in Columns)
        {
            column.EnsureHeaderStyle();
        }
    }

    /// <summary>
    /// Ensures the cells style is applied.
    /// </summary>
    private void EnsureCellsStyle()
    {
        foreach (var row in _rows)
        {
            row.EnsureCellsStyle();
        }
    }

#if !WINDOWS
    /// <summary>
    /// Ensures the cells are created.
    /// </summary>
    internal void EnsureCells()
    {
        foreach (var row in _rows)
        {
            row.EnsureCells();
        }
    }
#endif

    /// <summary>
    /// Shows the context flyout for the specified row.
    /// </summary>
    internal bool ShowRowContext(TableViewRow row, Point position)
    {
        var eventArgs = new TableViewRowContextFlyoutEventArgs(row.Index, row, row.Content, RowContextFlyout);
        OnRowContextFlyoutOpening(eventArgs);

        if (RowContextFlyout is not null && !eventArgs.Handled)
        {
#if !WINDOWS && !NET10_0_OR_GREATER
            // Uno 6 flyouts do not inherit their target's DataContext; Uno 7 does, and dropped the property.
            RowContextFlyout.DataContext = row.Content;
#endif
            RowContextFlyout.ShowAt(row.RowPresenter, new FlyoutShowOptions
            {
#if WINDOWS
                ShowMode = FlyoutShowMode.Standard,
#endif
                Placement = RowContextFlyout.Placement,
                Position = position
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// Shows the context flyout for the specified cell.
    /// </summary>
    internal bool ShowCellContext(TableViewCell cell, Point position)
    {
        var eventArgs = new TableViewCellContextFlyoutEventArgs(cell.Slot, cell, cell.Row?.Content!, CellContextFlyout);
        OnCellContextFlyoutOpening(eventArgs);

        if (CellContextFlyout is not null && !eventArgs.Handled)
        {
#if !WINDOWS && !NET10_0_OR_GREATER
            // Uno 6 flyouts do not inherit their target's DataContext; Uno 7 does, and dropped the property.
            CellContextFlyout.DataContext = cell.Row?.Content;
#endif
            CellContextFlyout.ShowAt(cell, new FlyoutShowOptions
            {
#if WINDOWS
                ShowMode = FlyoutShowMode.Standard,
#endif
                Placement = CellContextFlyout.Placement,
                Position = position
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the state of the corner button.
    /// </summary>
    internal void UpdateCornerButtonState()
    {
        _headerRow?.SetCornerButtonState();

    }

    internal void SetIsEditing(bool value)
    {
        if (IsEditing == value)
        {
            return;
        }

        IsEditing = value;
        UpdateCornerButtonState();
    }

    /// <summary>
    /// Sets the visibility of the headers.
    /// </summary>
    private void SetHeadersVisibility()
    {
        if (_headerRowDefinition is not null)
        {
            var areColumnHeadersVisible = HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Columns;
            _headerRowDefinition.Height = areColumnHeadersVisible ? GridLength.Auto : new(0);
        }

        _headerRow?.SetHeadersVisibility();

        foreach (var row in _rows)
        {
            row.RowPresenter?.SetRowHeaderVisibility();
        }
    }

    /// <summary>
    /// Updates the margin of the horizontal scroll bar to account for frozen columns and row headers.
    /// </summary>
    internal void UpdateHorizontalScrollBarMargin()
    {
        if (_scrollViewer is null) return;

        var frozenColumns = Columns.VisibleColumns.Where(c => c.IsFrozen);
        var offset = (CellsHorizontalOffset is double.NaN ? 0 : CellsHorizontalOffset) + frozenColumns.Sum(c => c.ActualWidth);
        AttachedPropertiesHelper.SetFrozenColumnScrollBarSpace(_scrollViewer, offset);
    }
}
