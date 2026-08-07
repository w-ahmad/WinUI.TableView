using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a cell in a TableView.
/// </summary>
[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePointerOver, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateRegular, GroupName = VisualStates.GroupCurrent)]
[TemplateVisualState(Name = VisualStates.StateCurrent, GroupName = VisualStates.GroupCurrent)]
[TemplateVisualState(Name = VisualStates.StateSelected, GroupName = VisualStates.GroupSelection)]
[TemplateVisualState(Name = VisualStates.StateUnselected, GroupName = VisualStates.GroupSelection)]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewCell : ContentControl
{
    private ContentPresenter? _contentPresenter;
    private Border? _selectionBorder;
    private Border? _backgroundBorder;
    private Border? _rootBorder;
    private Rectangle? _v_gridLine;
    private object? _uneditedValue;
    private RoutedEventArgs? _editingArgs;
    private IList<TableViewConditionalCellStyle>? _cellStyles;
    private bool _resizePreviewActive;
    private double _resizePreviewWidth;
    private RectangleGeometry? _resizeClipGeometry;
    private TranslateTransform? _gridLineShiftTransform;
    private TranslateTransform? _downstreamShiftTransform;

    /// <summary>
    /// Initializes a new instance of the TableViewCell class.
    /// </summary>
    public TableViewCell()
    {
        DefaultStyleKey = typeof(TableViewCell);
        ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        Loaded += OnLoaded;
#if WINDOWS
        ContextRequested += OnContextRequested;
#endif
    }

#if !WINDOWS
    /// <inheritdoc/>
    protected override void OnRightTapped(RightTappedRoutedEventArgs e)
    {
        base.OnRightTapped(e);

        var position = e.GetPosition(this);
#else
    /// <summary>
    /// Handles the ContextRequested event.
    /// </summary>
    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs e)
    {
        if (!e.TryGetPosition(sender, out var position)) return;
#endif

        // Select the cell before showing the Context Menu
        if (TableView is not null && TableView.ForceRowOrCellSelectionOnContextRequested && !IsSelected)
        {
            TableView.MakeSelection(Slot, false);
        }

        e.Handled = TableView?.ShowCellContext(this, position) is true;
    }


    /// <summary>
    /// Handles the Loaded event.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InvalidateMeasure();
        ApplySelectionState();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPresenter = GetTemplateChild("Content") as ContentPresenter;
        _selectionBorder = GetTemplateChild("SelectionBorder") as Border;
        _backgroundBorder = GetTemplateChild("BackgroundBorder") as Border;
        _rootBorder = GetTemplateChild("RootBorder") as Border;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;

        EnsureGridLines();
        EnsureStyle(Row?.Content);
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (newContent is ContentControl contentControl)
        {
            contentControl.Loaded += OnContentLoaded;
        }

        void OnContentLoaded(object sender, RoutedEventArgs e)
        {
            ((ContentControl)sender).Loaded -= OnContentLoaded;
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is not null && Column is not null && Row is not null && _contentPresenter is not null && Content is FrameworkElement element)
        {
            if (Column is TableViewTemplateColumn)
            {
#if WINDOWS
                if (element is ContentControl { ContentTemplateRoot: FrameworkElement root })
#else
                if (element.FindDescendant<ContentPresenter>() is { ContentTemplateRoot: FrameworkElement root })
#endif
                    element = root;
                else
                    return base.MeasureOverride(availableSize);
            }

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            element.MaxWidth = double.PositiveInfinity;
            element.MaxHeight = double.PositiveInfinity;
            #endregion

            // Skip the unconstrained auto-width measurement while the column is being manually
            // resized — it only feeds Column.DesiredWidth, which is irrelevant to a pixel-width drag,
            // and this cell doesn't get remeasured on every drag frame anyway (see BeginResizePreview).
            if (!Column.IsResizing)
            {
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                var autoSizeMode = Column.ColumnAutoWidthMode ?? TableView.ColumnAutoWidthMode;
                if (autoSizeMode is TableViewColumnAutoWidthMode.Cells or TableViewColumnAutoWidthMode.Both)
                {
                    var desiredWidth = element.DesiredSize.Width;
                    desiredWidth += Padding.Left;
                    desiredWidth += Padding.Right;
                    desiredWidth += BorderThickness.Left;
                    desiredWidth += BorderThickness.Right;
                    desiredWidth += _selectionBorder?.BorderThickness.Right ?? 0;
                    desiredWidth += _selectionBorder?.BorderThickness.Left ?? 0;
                    desiredWidth += _v_gridLine?.ActualWidth ?? 0d;

                    Column.DesiredWidth = Math.Max(Column.DesiredWidth, desiredWidth);
                }
            }

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            // While a resize preview is active, the content was already generously (re)measured once
            // in BeginResizePreview and must keep that width so Clip can freely reveal/hide it every
            // frame without another Measure pass — using the live Column.ActualWidth here (which is
            // intentionally frozen during the drag, see TableView.UpdateColumnResizePreview) would
            // re-clamp the content straight back to the pre-drag size.
            var contentWidth = _resizePreviewActive ? _resizePreviewWidth : Column.ActualWidth;
            contentWidth -= element.Margin.Left;
            contentWidth -= element.Margin.Right;
            contentWidth -= Padding.Left;
            contentWidth -= Padding.Right;
            contentWidth -= BorderThickness.Left;
            contentWidth -= BorderThickness.Right;
            contentWidth -= _selectionBorder?.BorderThickness.Left ?? 0;
            contentWidth -= _selectionBorder?.BorderThickness.Right ?? 0;
            contentWidth -= _v_gridLine?.ActualWidth ?? 0d;

            var height = Height is double.NaN ? double.PositiveInfinity : Height;
            var contentHeight = Math.Min(height, MaxHeight);
            contentHeight -= element.Margin.Top;
            contentHeight -= element.Margin.Bottom;
            contentHeight -= Padding.Top;
            contentHeight -= Padding.Bottom;
            contentHeight -= BorderThickness.Top;
            contentHeight -= BorderThickness.Bottom;
            contentHeight -= _selectionBorder?.BorderThickness.Top ?? 0;
            contentHeight -= _selectionBorder?.BorderThickness.Bottom ?? 0;
            contentHeight -= GetHorizontalGridlineHeight();

            if (contentWidth < 0 || contentHeight < 0)
            {
                _contentPresenter.Visibility = Visibility.Collapsed;
            }
            else
            {
                element.MaxWidth = contentWidth;
                element.MaxHeight = contentHeight;
                _contentPresenter.Visibility = Visibility.Visible;
            }
            #endregion
        }

        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        // During a resize-drag preview, manually re-arrange the overlapping template borders wider
        // than the Grid's own column-based sizing would give them (the Grid still thinks this cell is
        // its pre-drag width, since Width itself is left untouched for the whole drag) — this is what
        // lets the generously-premeasured content in BeginResizePreview actually render past the old
        // boundary; Clip then reveals/hides it every frame. Same "arrange a child beyond what the
        // framework gave it" technique already used in TableViewRow.ArrangeOverride for _itemPresenter.
        if (_resizePreviewActive)
        {
            var wideRect = new Rect(0, 0, _resizePreviewWidth, finalSize.Height);
            _backgroundBorder?.Arrange(wideRect);
            _selectionBorder?.Arrange(wideRect);
            _rootBorder?.Arrange(wideRect);
        }

        return finalSize;
    }

    /// <summary>
    /// Begins a live resize-drag preview for this cell: generously (re)measures its content once so
    /// widening can freely reveal more of it, and creates this cell's own <see cref="Clip"/> geometry
    /// and gridline shift transform. These are per-cell instances (not shared across cells — WinUI
    /// throws if the same <see cref="RectangleGeometry"/> is assigned as <see cref="Clip"/> on more
    /// than one element at a time), mutated in place every frame by
    /// <see cref="UpdateResizePreviewClip"/>/<see cref="UpdateGridLineShift"/> — still no Measure/Arrange
    /// per frame, just not a single shared instance across every row.
    /// </summary>
    internal void BeginResizePreview(double maxPreviewWidth)
    {
        _resizePreviewWidth = ActualWidth;

        if (Content is FrameworkElement element)
        {
            if (Column is TableViewTemplateColumn)
            {
#if WINDOWS
                if (element is ContentControl { ContentTemplateRoot: FrameworkElement root })
#else
                if (element.FindDescendant<ContentPresenter>() is { ContentTemplateRoot: FrameworkElement root })
#endif
                    element = root;
                else
                    element = null!;
            }

            if (element is not null)
            {
                element.MaxWidth = maxPreviewWidth;
                element.MaxHeight = double.PositiveInfinity;
                element.Measure(new Size(maxPreviewWidth, double.PositiveInfinity));

                var desiredWidth = element.DesiredSize.Width;
                desiredWidth += element.Margin.Left;
                desiredWidth += element.Margin.Right;
                desiredWidth += Padding.Left;
                desiredWidth += Padding.Right;
                desiredWidth += BorderThickness.Left;
                desiredWidth += BorderThickness.Right;
                desiredWidth += _selectionBorder?.BorderThickness.Left ?? 0;
                desiredWidth += _selectionBorder?.BorderThickness.Right ?? 0;
                desiredWidth += _v_gridLine?.ActualWidth ?? 0d;

                _resizePreviewWidth = Math.Min(maxPreviewWidth, Math.Max(ActualWidth, desiredWidth));
            }
        }

        _resizePreviewActive = true;

        _resizeClipGeometry = new RectangleGeometry { Rect = ComputeClipRect(ActualWidth, ActualHeight) };
        Clip = _resizeClipGeometry;

        if (_v_gridLine is not null)
        {
            _gridLineShiftTransform = new TranslateTransform();
            _v_gridLine.RenderTransform = _gridLineShiftTransform;
        }

        InvalidateArrange();
    }

    /// <summary>
    /// Shifts this cell sideways to visually make room for the column being resized, without any
    /// real layout — creates this cell's own <see cref="TranslateTransform"/>, mutated in place every
    /// frame by <see cref="UpdateDownstreamShift"/>.
    /// </summary>
    internal void ApplyDownstreamShift()
    {
        _downstreamShiftTransform = new TranslateTransform();
        RenderTransform = _downstreamShiftTransform;
    }

    /// <summary>
    /// Updates this resize-preview cell's clip to the given live drag width. No-op if this cell
    /// isn't the one being resized (i.e. <see cref="BeginResizePreview"/> was never called on it).
    /// </summary>
    internal void UpdateResizePreviewClip(double liveWidth, double height)
    {
        if (_resizeClipGeometry is not null)
        {
            _resizeClipGeometry.Rect = ComputeClipRect(liveWidth, height);
        }
    }

    /// <summary>
    /// Shifts this resize-preview cell's own gridline to track the live drag boundary. No-op if this
    /// cell isn't the one being resized.
    /// </summary>
    internal void UpdateGridLineShift(double deltaX)
    {
        if (_gridLineShiftTransform is not null)
        {
            _gridLineShiftTransform.X = deltaX;
        }
    }

    /// <summary>
    /// Updates this downstream cell's shift to the given delta. No-op if <see cref="ApplyDownstreamShift"/>
    /// was never called on this cell.
    /// </summary>
    internal void UpdateDownstreamShift(double deltaX)
    {
        if (_downstreamShiftTransform is not null)
        {
            _downstreamShiftTransform.X = deltaX;
        }
    }

    /// <summary>
    /// Ends a resize-drag preview started by <see cref="BeginResizePreview"/> or
    /// <see cref="ApplyDownstreamShift"/>, reverting this cell to normal layout-driven sizing.
    /// </summary>
    internal void EndResizePreview()
    {
        _resizePreviewActive = false;
        Clip = null;
        RenderTransform = null;
        _resizeClipGeometry = null;
        _gridLineShiftTransform = null;
        _downstreamShiftTransform = null;

        if (_v_gridLine is not null)
        {
            _v_gridLine.RenderTransform = null;
        }

        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>
    /// Computes the clip rect that reveals/hides a resize-preview cell's content for a given live
    /// drag width. Pure function — no side effects — so it's directly unit-testable.
    /// </summary>
    internal static Rect ComputeClipRect(double liveWidth, double height)
    {
        return new Rect(0, 0, Math.Max(0, liveWidth), Math.Max(0, height));
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);

        if ((TableView?.SelectionMode is not ListViewSelectionMode.None
           && TableView?.SelectionUnit is not TableViewSelectionUnit.Row)
           || !TableView.IsReadOnly)
        {
            VisualStates.GoToState(this, false, VisualStates.StatePointerOver);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);

        if ((TableView?.SelectionMode is not ListViewSelectionMode.None
            && TableView?.SelectionUnit is not TableViewSelectionUnit.Row)
            || !TableView.IsReadOnly)
        {
            VisualStates.GoToState(this, false, VisualStates.StateNormal);
        }
    }

    /// <inheritdoc/>
    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

        if (!TryEndCurrentCellEdit())
        {
            e.Handled = true;
            return;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!TryEndCurrentCellEdit())
        {
            e.Handled = true;
            return;
        }
    }
    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if(TableView?.SelectionUnit is not TableViewSelectionUnit.Row)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Tries to end the current edit operation, if any.
    /// </summary>
    /// <returns>True if an edit operation was successfully ended, or there is no edit operation.
    /// False if the current edit operation can not be ended.</returns>
    private bool TryEndCurrentCellEdit()
    {
        if ((TableView?.IsEditing ?? false) &&
             TableView.CurrentCellSlot != Slot &&
             TableView.CurrentCellSlot.HasValue &&
             TableView.GetCellFromSlot(TableView.CurrentCellSlot.Value) is { } currentCell)
        {
            if (!TableView.EndCellEditing(TableViewEditAction.Commit, currentCell)) return false;

            TableView.SetIsEditing(false);
        }

        return true;
    }

    /// <summary>
    /// Gets the height of the horizontal gridlines/>.
    /// </summary>
    private double GetHorizontalGridlineHeight()
    {
        return TableView?.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
            ? TableView.HorizontalGridLinesStrokeThickness : 0d;
    }

    /// <inheritdoc/>
    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        var eventArgs = new TableViewCellDoubleTappedEventArgs(Slot, this, Row?.Content);
        TableView?.OnCellDoubleTapped(eventArgs);
        e.Handled = eventArgs.Handled;

        if (e.Handled) return;

        base.OnDoubleTapped(e);

        e.Handled = IsReadOnly || TableView is null || TableView.IsEditing || !Column?.UseSingleElement is not true || BeginCellEditing(e);
    }

    /// <summary>
    /// Initiates editing mode for the current cell, raising the beginning edit event and allowing cancellation.
    /// </summary>
    /// <param name="editingArgs">The event data associated with the editing request. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if cell editing was
    /// successfully started; otherwise, <see langword="false"/> if the operation was canceled.</returns>
    internal bool BeginCellEditing(RoutedEventArgs editingArgs)
    {
        var args = new TableViewBeginningEditEventArgs(this, Row?.Content, Column!, editingArgs);
        TableView?.OnBeginningEdit(args);

        if (!args.Cancel)
        {
            PrepareForEdit(editingArgs);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Prepares the cell for editing.
    /// </summary>
    internal void PrepareForEdit(RoutedEventArgs editingArgs)
    {
        var editingElement = SetEditingElement();
        Content = editingElement;

        if (TableView is not null)
        {
            TableView.SetIsEditing(true);
            TableView.UpdateCornerButtonState();
        }

        if (editingElement is { IsHitTestVisible: true })
        {
            _editingArgs = editingArgs;
            editingElement.Loaded += OnEditingElementLoaded;
        }
    }

    private void OnEditingElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement editingElement)
        {
            editingElement.Loaded -= OnEditingElementLoaded;
            editingElement.Focus(FocusState.Pointer);
            _editingArgs ??= new RoutedEventArgs();

            var args = new TableViewPreparingCellForEditEventArgs(this, Row?.Content, Column!, editingElement, _editingArgs);
            _uneditedValue = Column?.PrepareCellForEdit(this, _editingArgs);
            TableView?.OnPreparingCellForEdit(args);
        }
    }

    /// <summary>
    /// Sets the editing element for the cell.
    /// </summary>
    private FrameworkElement? SetEditingElement()
    {
        if (Column?.UseSingleElement ?? false)
        {
            return Content as FrameworkElement;
        }
        else
        {
            var element = Column?.GenerateEditingElement(this, Row?.Content);

            if (element is not null && Column is TableViewBoundColumn { EditingElementStyle: { } } boundColumn)
            {
                element.Style = boundColumn.EditingElementStyle;
            }

            return element;
        }
    }

    internal void EndEditing(TableViewEditAction editAction)
    {
        Column?.EndCellEditing(this, Row?.Content, editAction, _uneditedValue);
        SetElement();
    }

    /// <summary>
    /// Sets the element for the cell.
    /// </summary>
    internal void SetElement()
    {
        var element = Column?.GenerateElement(this, Row?.Content);

        if (element is not null && Column is TableViewBoundColumn { ElementStyle: { } } boundColumn)
        {
            element.Style = boundColumn.ElementStyle;
        }

        Content = element;

#if !WINDOWS
        DispatcherQueue.TryEnqueue(async () =>
        {
            await Task.Delay(20);
            Focus(FocusState.Pointer);
        });
#endif

        DispatcherQueue.TryEnqueue(InvalidateMeasure);
    }

    /// <summary>
    /// Refreshes the element for the cell.
    /// </summary>
    internal void RefreshElement()
    {
        Column?.RefreshElement(this, Row?.Content);
    }

    /// <summary>
    /// Applies the selection state to the cell.
    /// </summary>
    internal void ApplySelectionState()
    {
        var stateName = IsSelected ? VisualStates.StateSelected : VisualStates.StateUnselected;
        VisualStates.GoToState(this, false, stateName);
    }

    /// <summary>
    /// Applies the current cell state to the cell.
    /// </summary>
    internal async void ApplyCurrentCellState(bool skipFocus = false)
    {
        var stateName = IsCurrent ? VisualStates.StateCurrent : VisualStates.StateRegular;
        VisualStates.GoToState(this, false, stateName);

        if (IsCurrent && !skipFocus)
        {
            Focus(FocusState.Pointer);

            await Task.Delay(20);
            if (Content is UIElement { IsHitTestVisible: true } element)
            {
                element.Focus(FocusState.Pointer);
            }
        }
    }

    /// <summary>
    /// Updates the element state for the cell.
    /// </summary>
    internal void UpdateElementState()
    {
        Column?.UpdateElementState(this, Row?.Content);
    }

    /// <summary>
    /// Handles changes to the column.
    /// </summary>
    private void OnColumnChanged()
    {
        if (TableView?.IsEditing == true)
        {
            SetEditingElement();
        }
        else
        {
            SetElement();
        }
    }

    /// <summary>
    /// Ensures grid lines are applied to the cell.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (_v_gridLine is not null && TableView is not null)
        {
            _v_gridLine.Fill = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                               ? TableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
            _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
            _v_gridLine.Visibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                     || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                     ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Ensures the correct style is applied to the cell.
    /// </summary>
    /// <param name="item">The data item associated with the cell.</param>
    internal void EnsureStyle(object? item)
    {
        _cellStyles ??= [
            .. Column?.ConditionalCellStyles ?? [], // Column styles have first priority
            .. TableView?.ConditionalCellStyles ?? []]; // TableView styles have second priority

        Style = _cellStyles.FirstOrDefault(c => c.Predicate?.Invoke(new(Column!, item)) is true)?
                          .Style ?? Column?.CellStyle ?? TableView?.CellStyle;
    }

    /// <summary>
    /// Gets a value indicating whether the cell is read-only.
    /// </summary>
    public bool IsReadOnly => TableView?.IsReadOnly is true
                              || Column is TableViewTemplateColumn { EditingTemplate: null, EditingTemplateSelector: null } or { IsReadOnly: true };

    /// <summary>
    /// Gets the slot for the cell.
    /// </summary>
    public TableViewCellSlot Slot => new(Row?.Index ?? -1, Index);

    /// <summary>
    /// Gets or sets the index of the cell.
    /// </summary>
    internal int Index { get; set; }

    /// <summary>
    /// Gets a value indicating whether the cell is selected.
    /// </summary>
    public bool IsSelected => TableView?.SelectedCells.Contains(Slot) is true;

    /// <summary>
    /// Gets a value indicating whether the cell is the current cell.
    /// </summary>
    public bool IsCurrent => TableView?.CurrentCellSlot == Slot;

    /// <summary>
    /// Gets or sets the column for the cell.
    /// </summary>
    public TableViewColumn? Column
    {
        get;
        internal set
        {
            if (field != value)
            {
                field = value;
                OnColumnChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the row for the cell.
    /// </summary>
    public TableViewRow? Row { get; internal set; }

    /// <summary>
    /// Gets or sets the TableView for the cell.
    /// </summary>
    public TableView? TableView { get; internal set; }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationPeers.TableViewCellAutomationPeer(this);
    }
}
