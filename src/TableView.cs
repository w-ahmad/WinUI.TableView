using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;
using Pointer = Microsoft.UI.Xaml.Input.Pointer;

namespace WinUI.TableView;

/// <summary>
/// Represents a control that displays data in customizable table-like interface.
/// </summary>
[StyleTypedProperty(Property = nameof(ColumnHeaderStyle), StyleTargetType = typeof(TableViewColumnHeader))]
[StyleTypedProperty(Property = nameof(CellStyle), StyleTargetType = typeof(TableViewCell))]
public partial class TableView : ListView
{
    private TableViewHeaderRow? _headerRow;
    private ScrollViewer? _scrollViewer;
    private RowDefinition? _headerRowDefinition;
    private bool _shouldThrowSelectionModeChangedException;
    private bool _ensureColumns = true;
    private bool _isItemsSourceSuspended;
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
    private readonly HashSet<int> _pendingCellStateRows = [];

    /// <summary>
    /// Initializes a new instance of the TableView class.
    /// </summary>
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);

        Columns = new TableViewColumnsCollection(this);
        FilterHandler = new ColumnFilterHandler(this);

        base.ItemsSource = _collectionView;
        base.SelectionMode = SelectionMode;

        SetValue(ConditionalCellStylesProperty, new TableViewConditionalCellStylesCollection());
        RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, OnBaseItemsSourceChanged);
        RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, OnBaseSelectionModeChanged);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SelectionChanged += TableView_SelectionChanged;
        _collectionView.ItemPropertyChanged += OnItemPropertyChanged;

        AddHandler(PointerPressedEvent, new PointerEventHandler(OnAnyPointerPressed), handledEventsToo: true);
        AddHandler(PointerReleasedEvent, new PointerEventHandler(OnAnyPointerReleased), handledEventsToo: true);
    }

    /// <summary>
    /// Handles the SelectionChanged event of the TableView control.
    /// </summary>
    private void TableView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TableViewTrace.Write($"TableViewSelectionChanged: AddedItems={e.AddedItems.Count}, RemovedItems={e.RemovedItems.Count}");

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
            else
            {
                var addedIndexes = e.AddedItems
                    .Select(item => Items.IndexOf(item))
                    .Where(i => i >= 0);

                if (Columns.VisibleColumns.Count == 0) return;

                foreach (var range in IndexRangeHelper.GetRanges(addedIndexes))
                {
                    var slotRange = TableViewCellSlotRange.FromCoordinates(range.FirstIndex, 0, range.LastIndex, Columns.VisibleColumns.Count - 1);
                    SubtractCellRangeFromSelection(slotRange);
                }
            }

            CurrentCellSlot = null;
            OnCellSelectionChanged();
        }

        if (SelectedItems?.Count == 1)
        {
            DispatcherQueue.TryEnqueue(async () => await ScrollRowIntoView(SelectedIndex));
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

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        DispatcherQueue.TryEnqueue(() =>
        {
            if (element is TableViewRow row)
            {
                if (!_rows.Contains(row))
                {
                    _rows.Add(row);
                }

                row.TableView = this;
                row.EnsureCellsStyle(default, item);

                _pendingCellStateRows.Add(row.Index);
                if (!_cellStateDispatchPending)
                {
                    _cellStateDispatchPending = true;
                    DispatcherQueue.TryEnqueue(ApplyPendingCellStates);
                }

                row.RowPresenter?.ApplyDetailsPaneState(item);

                if (CurrentCellSlot.HasValue)
                {
                    row.ApplyCurrentCellState(CurrentCellSlot.Value);
                }
            }
        });
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (element is TableViewRow row)
        {
            _rows.Remove(row);
            row.TableView = null;
        }

        base.ClearContainerForItemOverride(element, item);
    }

    /// <inheritdoc/>
    protected override DependencyObject GetContainerForItemOverride()
    {
        var row = new TableViewRow { TableView = this };

        // Set bindings for FontFamily and FontSize to propagate from TableView to TableViewRow
        row.SetBinding(FontFamilyProperty, new Binding { Path = new("TableView.FontFamily"), RelativeSource = new() { Mode = RelativeSourceMode.Self } });
        row.SetBinding(FontSizeProperty, new Binding { Path = new("TableView.FontSize"), RelativeSource = new() { Mode = RelativeSourceMode.Self } });

        _rows.Add(row);
        return row;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        var shiftKey = KeyboardHelper.IsShiftKeyDown();
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();

        if (HandleShortKeys(shiftKey, ctrlKey, e.Key))
        {
            e.Handled = true;
            return;
        }

        HandleNavigations(e, shiftKey, ctrlKey);
    }

    /// <summary>
    /// Handles pointer-pressed for all cases, including when elements sets <c>e.Handled = true</c>.
    /// </summary>
    private void OnAnyPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        var position = pointerPoint.Position;
        var canvasPoint = GetCanvasPoint(position);
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();
        var isShiftKey = KeyboardHelper.IsShiftKeyDown();
        var orignalSoruce = e.OriginalSource as FrameworkElement;
        UIElement? pressedElement = orignalSoruce?.FindAscendant<TableViewCell>();      // Check if the pointer is over a cell
        pressedElement ??= orignalSoruce?.FindAscendant<TableViewRow>();                // If not, check if the pointer is over a row

        if (SelectionMode is ListViewSelectionMode.None                                 // Skip selection when SelectionMode is None
            || IsDragSelecting                                                          // Skip selection when a drag is already in progress
            || orignalSoruce is ScrollBar                                               // Skip selection when the pointer is over the ScrollBar
            || orignalSoruce?.FindAscendant<ScrollBar>() is { }                         // Skip selection when the pointer is within a ScrollBar
            || (pressedElement == null && !ShowDragRectangle)                           // Skip selection when the pointer is not over a Cell or Row, and ShowDragRectangle is false.
            || !pointerPoint.Properties.IsLeftButtonPressed                             // Skip selection when the left mouse button is not pressed
            || canvasPoint is null                                                      // Skip selection when canvasPoint is null (e.g., pointer is outside the scroll canvas)
            || canvasPoint.Value.Y < 0                                                  // Skip selection when the pointer is in the column header area (above the scroll canvas)              
            || (pressedElement == null && canvasPoint.Value.X < CellsHorizontalOffset)  // Skip selection when the pointer is in the row header area (and not on a row/cell)
            || isShiftKey)                                                              // Skip selection when the Shift key is held
        {
            return;
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
        _dragStartRow = (pressedElement as TableViewRow) ?? orignalSoruce?.FindAscendant<TableViewRow>();
#endif
        pressedElement ??= this; // If not, default to the TableView itself

        SelectionStartCellSlot = (pressedElement as TableViewCell)?.Slot;
        SelectionStartRowIndex = (pressedElement as TableViewRow)?.Index;

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
            _lastDragCanvasPoint = canvasPoint;
            MakeSelectionInDragRect();
            SetCurrentCell(GetSlotAtCanvasPoint(_lastDragCanvasPoint.Value));

            return;
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
            return;
        }

        MakeSelectionInDragRect();
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

        // Selection-by-hit-test is only needed for TableView-initiated drags; cell-initiated
        // drags perform selection in the cell's OnManipulationDelta via FindCell.
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

        // Find the min/max row indices whose bounds intersect the rect vertically.
        var minRow = -1;
        var maxRow = -1;

        for (var rowIndex = 0; rowIndex < Items.Count; rowIndex++)
        {
            if (ContainerFromIndex(rowIndex) is not TableViewRow row)
            {
                continue;
            }

            var rowTop = row.Position.Y;
            var rowBottom = rowTop + row.ActualHeight;

            if (rowBottom <= rectTop || rowTop >= rectBottom)
            {
                continue;
            }

            if (minRow == -1) minRow = rowIndex;
            maxRow = rowIndex;
        }

        if (minRow == -1)
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
    /// Handles pointer-released for all cases, including when elements sets <c>e.Handled = true</c>.
    /// </summary>
    private void OnAnyPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndDragSelection();
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

            var numRows = CollectionView.Count;
            var nextRow = e.Key == VirtualKey.PageDown
                ? Math.Min(numRows - 1, row + pageSize)
                : Math.Max(0, row - pageSize);

            var newSlot = new TableViewCellSlot(nextRow, column);
            MakeSelection(newSlot, shiftKey);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Calculates how many rows should be able to fit within the actual height of the table without scrolling.
    /// </summary>
    private int CalculateAvailablePageSize()
    {
        var rowHeight = RowHeight is not double.NaN ? RowHeight : RowMinHeight;
        var headerHeight = HeaderRowHeight is not double.NaN ? HeaderRowHeight : HeaderRowMinHeight;
        var availableHeight = ActualHeight - headerHeight;
        return (int)Math.Floor(availableHeight / rowHeight);
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
    protected async override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerRow = GetTemplateChild("HeaderRow") as TableViewHeaderRow;
        _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
        _headerRowDefinition = GetTemplateChild("HeaderRowDefinition") as RowDefinition;
        DragRectangleCanvas = GetTemplateChild("DragRectangleCanvas") as Canvas;
        _dragRectangle = GetTemplateChild("DragRectangle") as Border;
        _scrollViewer?.Loaded += OnScrollViewerLoaded;
        _scrollViewer?.ViewChanged += OnScrollViewerViewChanged;

        if (IsLoaded)
        {
            while (ItemsPanelRoot is null) await Task.Yield();

            EnsureAutoColumns();
        }

        SetHeadersVisibility();
    }

    /// <summary>
    /// Handles the ViewChanged event of the ScrollViewer control, updating the position of each row when the view changes.
    /// </summary>
    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        foreach (var row in _rows)
        {
            row.UpdatePosition();
        }
    }

    /// <summary>
    /// Handles the Loaded event of the ScrollViewer control.
    /// </summary>
    private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        var scrollPresenter = _scrollViewer?.FindDescendant<ScrollContentPresenter>();
        var xScrollBar = _scrollViewer?.FindDescendant<ScrollBar>(sb => sb.Name is "HorizontalScrollBar2");
        var yScrollBar = _scrollViewer?.FindDescendant<ScrollBar>(sb => sb.Name is "VerticalScrollBar");

        scrollPresenter?.PointerWheelChanged += OnScrollContentPresenterPointerWheelChanged;

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
        EnsureAutoColumns();
    }

    /// <summary>
    /// Handles the Unloaded event of the TableView control.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        EndDragSelection();
        StopAutoScroll();

        if (IsEditing && CurrentCellSlot.HasValue && GetCellFromSlot(CurrentCellSlot.Value) is { } currentCell)
        {
            currentCell.EndEditing(TableViewEditAction.Commit);
        }

        SuspendItemsSource();
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

        _collectionView.ItemPropertyChanged -= OnItemPropertyChanged;
        _collectionView.Source = Enumerable.Empty<object>();
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

        if (ItemsSource is IEnumerable source)
        {
            _collectionView.Source = source;
        }

        _isItemsSourceSuspended = false;
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

        if (SelectedItems.Any() || SelectedCells.Count != 0)
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
    /// Refreshes all applied filters.
    /// </summary>
    public void RefreshFilter()
    {
        DeselectAll();
        _collectionView.RefreshFilter();
    }

    /// <summary>
    /// Selects all rows or cells in the TableView.
    /// </summary>
    internal new void SelectAll()
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
                    SelectRange(new ItemIndexRange(0, (uint)Items.Count));
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
                var selectionRange = new HashSet<TableViewCellSlot>();

                for (var row = 0; row < Items.Count; row++)
                {
                    for (var column = 0; column < Columns.VisibleColumns.Count; column++)
                    {
                        selectionRange.Add(new TableViewCellSlot(row, column));
                    }
                }
                SelectedCellRanges.Add(selectionRange);
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
        if (SelectedRanges.Count is 0) return;

        switch (SelectionMode)
        {
            case ListViewSelectionMode.Single:
                SelectedItem = null;
                break;
            case ListViewSelectionMode.Multiple:
            case ListViewSelectionMode.Extended:
                DeselectRange(new ItemIndexRange(0, (uint)Items.Count));
                break;
        }
    }

    /// <summary>
    /// Deselects all cells in the TableView.
    /// </summary>
    private void DeselectAllCells()
    {
        if (SelectedCellRanges.Count is 0) return;

        SelectedCellRanges.Clear();
        OnCellSelectionChanged();
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
        var selectionRange = SelectedRanges.FirstOrDefault(x => x.IsInRange(slot.Row));
        SelectionStartRowIndex ??= slot.Row;

        if (selectionRange is not null && ctrlKey && !shiftKey && (CurrentRowIndex != slot.Row || CurrentCellSlot == slot))
        {
            DeselectRange(new ItemIndexRange(slot.Row, 1));
        }
        else if ((!shiftKey && !ctrlKey && SelectedItems.Count <= 1) || SelectionMode is ListViewSelectionMode.Single)
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
            if (selectionRange?.LastIndex > newSelection.LastIndex)
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

        if (shiftKey && SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
        {
            var newRange = TableViewCellSlotRange.FromSlots(SelectionStartCellSlot.Value, slot);
            SelectedCellRanges.Add(newRange);
        }
        else
        {
            SelectionStartCellSlot = slot;
            SelectedCellRanges.Add(TableViewCellSlotRange.FromSlots(slot));
        }
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
        var newSelection = SelectedCellRanges.SelectMany(x => x.GetSlots()).ToHashSet();
        var removedCells = SelectedCells.Where(s => !newSelection.Contains(s)).ToList();
        var addedCells = newSelection.Where(s => !SelectedCells.Contains(s)).ToList();

        if (removedCells.Count is 0 && addedCells.Count is 0) return;

        foreach (var slot in removedCells) SelectedCells.Remove(slot);
        foreach (var slot in addedCells) SelectedCells.Add(slot);

        OnCellSelectionChanged(new TableViewCellSelectionChangedEventArgs(removedCells, addedCells));

        foreach (var slot in removedCells.Concat(addedCells))
            _pendingCellStateRows.Add(slot.Row);

        if (!_cellStateDispatchPending)
        {
            _cellStateDispatchPending = true;
            DispatcherQueue.TryEnqueue(ApplyPendingCellStates);
        }
    }

    private void ApplyPendingCellStates()
    {
        _cellStateDispatchPending = false;
        if (_pendingCellStateRows.Count is 0) return;

        foreach (var row in _rows)
        {
            if (_pendingCellStateRows.Contains(row.Index))
                row.ApplyCellsSelectionState();
        }
        _pendingCellStateRows.Clear();
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

        const double edgeThreshold = 40;
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

        var slot = GetSlotAtCanvasPoint(_lastDragCanvasPoint.Value);
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

        var (start, end) = GetColumnsInDisplay();
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
    public async Task<TableViewRow?> ScrollRowIntoView(int index)
    {
        if (_scrollViewer is null || index < 0) return default!;

        var item = Items[index];
        index = Items.IndexOf(item); // if the ItemsSource has duplicate items in it. ScrollIntoView will only bring first index of the item.
        ScrollIntoView(item);

        var tries = 0;
        while (tries < 10)
        {
            tries++;
            await Task.Yield();

            if (ContainerFromIndex(index) is TableViewRow row)
            {
                var transform = row.TransformToVisual(_scrollViewer);
                var positionInScrollViewer = transform.TransformPoint(new Point(0, 0));
                if ((index == 0 && _scrollViewer.VerticalOffset > 0) || (index > 0 && positionInScrollViewer.Y < HeaderRowHeight))
                {
                    var yOffset = index == 0 ? 0d : _scrollViewer.VerticalOffset - row.ActualHeight + positionInScrollViewer.Y + 8;
                    var tcs = new TaskCompletionSource<object?>();

                    try
                    {
                        _scrollViewer.ViewChanged += ViewChanged;
                        _scrollViewer.ChangeView(0, yOffset, null, true);
                        await tcs.Task;
                    }
                    finally
                    {
                        _scrollViewer.ViewChanged -= ViewChanged;
                    }

                    void ViewChanged(object? _, ScrollViewerViewChangedEventArgs e)
                    {
                        if (e.IsIntermediate)
                        {
                            return;
                        }

                        tcs.TrySetResult(result: default);
                    }
                }

                return row;
            }
        }

        return default;
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
        if (DragRectangleCanvas is null) return null;

        foreach (var row in _rows)
        {
            var rowTop = row.Position.Y;
            var rowBottom = rowTop + row.ActualHeight;

            if (canvasPoint.Y >= rowTop && canvasPoint.Y < rowBottom)
            {
                return row.Index;
            }
        }

        return null;
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
    private TableViewCellSlot? GetSlotAtCanvasPoint(Point canvasPoint)
    {
        if (DragRectangleCanvas is null) return null;

        if (GetRowIndexAtCanvasPoint(canvasPoint) is not int rowIndex) return null;

        // Mirror the row snapping: find the nearest column within the horizontal drag span.
        var horizontalScrollDelta = HorizontalOffset - _dragStartHorizontalOffset;
        var adjustedPointerX = canvasPoint.X - horizontalScrollDelta;
        var colIndex = GetColumnIndexAtCanvasX(adjustedPointerX);

        return colIndex is null ? null : new TableViewCellSlot(rowIndex, colIndex.Value);
    }

    /// <summary>
    /// Gets the columns currently in view.
    /// </summary>
    private (int start, int end) GetColumnsInDisplay()
    {
        if (_scrollViewer is null) return default!;

        var start = -1;
        var end = -1;
        var width = 0d;
        var headersOffset = CellsHorizontalOffset;

        foreach (var column in Columns.VisibleColumns)
        {
            if (width >= HorizontalOffset &&
                width + column.ActualWidth <= HorizontalOffset + _scrollViewer.ViewportWidth - headersOffset)
            {
                if (start == -1)
                {
                    start = end = Columns.VisibleColumns.IndexOf(column);
                }
                else
                {
                    end = Columns.VisibleColumns.IndexOf(column);
                }
            }

            width += column.ActualWidth;
        }

        return (start, end);
    }

    /// <summary>
    /// Updates the base SelectionMode property.
    /// </summary>
    private void UpdateBaseSelectionMode()
    {
        _shouldThrowSelectionModeChangedException = true;
        base.SelectionMode = SelectionUnit is TableViewSelectionUnit.Cell ? ListViewSelectionMode.None : SelectionMode;

        UpdateHorizontalScrollBarMargin();
        _headerRow?.SetHeadersVisibility();

        foreach (var row in _rows)
        {
            row.EnsureLayout();
            row.RowPresenter?.SetRowHeaderVisibility();

        }

        _shouldThrowSelectionModeChangedException = false;
    }

    /// <summary>
    /// Ensures grid lines are applied to the header row and body rows.
    /// </summary>
    private void EnsureGridLines()
    {
        _headerRow?.EnsureGridLines();

        foreach (var row in _rows)
        {
            row.RowPresenter?.EnsureGridLines();
        }
    }

    /// <summary>
    /// Ensures alternate row colors are applied.
    /// </summary>
    internal void EnsureAlternateRowColors()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
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
                    cell.InvalidateMeasure();
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
#if !WINDOWS
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
#if !WINDOWS
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

        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (SelectionMode is ListViewSelectionMode.Multiple && SelectionUnit is not TableViewSelectionUnit.Cell)
            {
                foreach (var row in _rows)
                {
                    row.UpdateSelectCheckMarkOpacity();
                }
            }
        });
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

        var offset = CellsHorizontalOffset + Columns.VisibleColumns.Where(c => c.IsFrozen).Sum(c => c.ActualWidth);
        AttachedPropertiesHelper.SetFrozenColumnScrollBarSpace(_scrollViewer, offset);
    }
}
