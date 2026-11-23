using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
    private FrameworkElement? _contentPresenter; // not ContentPresenter: Native AOT can wrap it as a base class
    private Panel? _layersRoot;
    private Border? _selectionBorder;
    private Border? _backgroundBorder;
    private Border? _rootBorder;
    private bool _templateHasSelectionLayer;
    private bool _templateHasBackgroundLayer;
    private FrameworkElement? _v_gridLine;
    private object? _uneditedValue;
    private RoutedEventArgs? _editingArgs;
    private IList<TableViewConditionalCellStyle>? _cellStyles;
    private bool? _appliedSelected;
    private bool? _appliedCurrent;
    private bool _hasAppliedStyle;
    private bool _autoWidthDirty = true;
    private Style? _appliedStyle;
    private bool _resizePreviewActive;
    private double _resizePreviewWidth;
    private double _resizePreviewMaxWidth;
    private RectangleGeometry? _resizeClipGeometry;
    private TranslateTransform? _gridLineShiftTransform;
    private TranslateTransform? _downstreamShiftTransform;

    /// <summary>
    /// Initializes a new instance of the TableViewCell class.
    /// </summary>
    public TableViewCell()
    {
        DefaultStyleKey = typeof(TableViewCell);
        Loaded += OnLoaded;
        RegisterPropertyChangedCallback(BackgroundProperty, delegate { UpdateBackgroundLayer(); });
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

        _contentPresenter = GetTemplateChild("Content") as FrameworkElement;
        _layersRoot = GetTemplateChild("CellLayersRoot") as Panel;
        _selectionBorder = GetTemplateChild("SelectionBorder") as Border;
        _backgroundBorder = GetTemplateChild("BackgroundBorder") as Border;
        _rootBorder = GetTemplateChild("RootBorder") as Border;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as FrameworkElement;

        // A template that has these parts draws them through its own visual states. Without them, and with a root to
        // put them in, the cell adds them itself when it needs them.
        _templateHasSelectionLayer = _selectionBorder is not null;
        _templateHasBackgroundLayer = _backgroundBorder is not null;

        // A new template starts in its default states; the row may have pushed the cell's own before it had one.
        _appliedSelected = null;
        _appliedCurrent = null;
#if !WINDOWS
        // The new template's presenter starts visible; the next culling pass defers it again if need be.
        if (_isContentLayoutDeferred)
        {
            _isContentLayoutDeferred = false;
            ReleaseDeferredHeight();
        }
#endif

        EnsureGridLines();
        EnsureStyle(Row?.Content);
        UpdateBackgroundLayer();
        ApplySelectionState();
        ApplyCurrentVisualState();
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
            // The template's content exists only now, so the width it wants is measured in the next layout pass.
            ((ContentControl)sender).Loaded -= OnContentLoaded;
            InvalidateAutoWidth();
        }
    }

#if !WINDOWS
    private bool _isContentLayoutDeferred;
    private FrameworkElement? _deferredHeightHolder;

    /// <summary>
    /// Leaves the cell out of rendering while it is out of view, and its content out of layout until the cell is
    /// nearly in view, so text no one sees is not shaped when a row is reused (see <see cref="TableView.UpdateRenderCulling"/>).
    /// </summary>
    /// <param name="isInView">Whether any of the cell is inside the viewport.</param>
    /// <param name="isNearView">Whether any of the cell is inside the viewport or the margin around it.</param>
    internal void UpdateRenderCulling(bool isInView, bool isNearView)
    {
        ElementVisuals.SetInView(this, isInView);

        if (_contentPresenter is null)
        {
            return;
        }

        // A column sized by its cells needs every cell's natural width, seen or not.
        var defer = !isNearView
            && !(Column is { Width.IsAuto: true }
                 && (Column.ColumnAutoWidthMode ?? TableView?.ColumnAutoWidthMode)
                    is TableViewColumnAutoWidthMode.Cells or TableViewColumnAutoWidthMode.Both);

        if (_isContentLayoutDeferred != defer)
        {
            _isContentLayoutDeferred = defer;

            if (defer)
            {
                // The parent pins the height, so the cell's own MinHeight stays bound.
                if (_contentPresenter.Parent is FrameworkElement { ActualHeight: > 0 } holder)
                {
                    _deferredHeightHolder = holder;
                    holder.MinHeight = holder.ActualHeight;
                }

                _contentPresenter.Visibility = Visibility.Collapsed;
            }
            else
            {
                _contentPresenter.Visibility = Visibility.Visible;
                ReleaseDeferredHeight();
            }
        }
    }

    /// <summary>
    /// Lets go of the height held for content that is out of layout: it belonged to another item or to other
    /// row heights.
    /// </summary>
    internal void ReleaseDeferredHeight()
    {
        _deferredHeightHolder?.ClearValue(MinHeightProperty);
        _deferredHeightHolder = null;
    }
#endif

    /// <summary>
    /// Has the next measure pass measure the cell's content at its natural width again, for the column's auto width.
    /// </summary>
    internal void InvalidateAutoWidth()
    {
        _autoWidthDirty = true;
        InvalidateMeasure();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The content is measured at the column's width, once. Only an auto-width column whose cells count towards its
    /// width (<see cref="TableViewColumnAutoWidthMode.Cells"/> or <see cref="TableViewColumnAutoWidthMode.Both"/>)
    /// also needs the width the content would take unconstrained, and only when that can have changed: the content is
    /// new, or it fills the cell, so it may be clipped. Measuring every cell twice used to make every row the table
    /// showed cost double.
    /// </remarks>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is not null && Column is not null && Row is not null && _contentPresenter is not null && Content is FrameworkElement element
            && !Column.IsResizing
            && Column.Width.IsAuto
            && (Column.ColumnAutoWidthMode ?? TableView.ColumnAutoWidthMode) is TableViewColumnAutoWidthMode.Cells or TableViewColumnAutoWidthMode.Both)
        {
            if (Column is TableViewTemplateColumn)
            {
#if WINDOWS
                if (element is ContentControl { ContentTemplateRoot: FrameworkElement root })
#else
                if (element.FindDescendant<ContentPresenter>()?.GetContentTemplateRoot() is { } root)
#endif
                    element = root;
                else
                    return base.MeasureOverride(availableSize);
            }

            if (!_autoWidthDirty)
            {
                var size = base.MeasureOverride(availableSize);
                var givenWidth = LayoutInformation.GetAvailableSize(element).Width;

                if (double.IsInfinity(givenWidth))
                {
                    // Measured without a width limit already: what it took is what it wants.
                    RecordDesiredWidth(element.DesiredSize.Width);
                    return size;
                }

                // Content that takes less than the width it was given is not clipped, so it wants no more.
                if (element.DesiredSize.Width < givenWidth - 0.5)
                {
                    return size;
                }
            }

            _autoWidthDirty = false;
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            RecordDesiredWidth(element.DesiredSize.Width);
        }

        return base.MeasureOverride(availableSize);
    }

    /// <summary>
    /// Widens the column's auto width to fit content of the given width.
    /// </summary>
    private void RecordDesiredWidth(double contentWidth)
    {
        Column!.DesiredWidth = Math.Max(Column.DesiredWidth, contentWidth + GetChromeWidth());
    }

    /// <summary>
    /// Gets the width the cell's padding, borders and grid line take around its content.
    /// </summary>
    private double GetChromeWidth()
    {
        return Padding.Left
               + Padding.Right
               + BorderThickness.Left
               + BorderThickness.Right
               + SelectionBorderThickness.Left
               + SelectionBorderThickness.Right
               + (_v_gridLine?.ActualWidth ?? 0d);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);
#if !WINDOWS
        TableView?.InvalidateRenderCulling();
#endif

        // During a resize-drag preview, manually re-arrange the overlapping template borders wider
        // than the Grid's own column-based sizing would give them (the Grid still thinks this cell is
        // its pre-drag width, since Width itself is left untouched for the whole drag) — this is what
        // lets the generously-premeasured content in BeginResizePreview actually render past the old
        // boundary; Clip then reveals/hides it every frame. Same "arrange a child beyond what the
        // framework gave it" technique already used in TableViewRow.ArrangeOverride for _itemPresenter.
        if (_resizePreviewActive)
        {
            var bordersRect = new Rect(0, 0, _resizePreviewMaxWidth, finalSize.Height);
            _backgroundBorder?.Arrange(bordersRect);
            _selectionBorder?.Arrange(bordersRect);
            _rootBorder?.Arrange(new Rect(0, 0, _resizePreviewWidth, finalSize.Height));
        }

        return finalSize;
    }

    /// <summary>
    /// Begins a live resize-drag preview for this cell: generously (re)measures its content once so
    /// widening can freely reveal more of it, and creates this cell's own <see cref="UIElement.Clip"/> geometry
    /// and gridline shift transform. These are per-cell instances (not shared across cells — WinUI
    /// throws if the same <see cref="RectangleGeometry"/> is assigned as <see cref="UIElement.Clip"/> on more
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
                if (element.FindDescendant<ContentPresenter>()?.GetContentTemplateRoot() is { } root)
#endif
                    element = root;
                else
                    element = null!;
            }

            if (element is not null)
            {
                element.Measure(new Size(maxPreviewWidth, double.PositiveInfinity));

                var desiredWidth = element.DesiredSize.Width;
                desiredWidth += element.Margin.Left;
                desiredWidth += element.Margin.Right;
                desiredWidth += GetChromeWidth();

                _resizePreviewWidth = Math.Min(maxPreviewWidth, Math.Max(ActualWidth, desiredWidth));
            }
        }

        _resizePreviewActive = true;
        _resizePreviewMaxWidth = maxPreviewWidth;

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
        _resizePreviewMaxWidth = 0d;
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
            ApplyPointerOverState(isPointerOver: true);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        if ((TableView?.SelectionMode is not ListViewSelectionMode.None
            && TableView?.SelectionUnit is not TableViewSelectionUnit.Row)
            || !TableView.IsReadOnly)
        {
            ApplyPointerOverState(isPointerOver: false);
        }
    }

    /// <summary>
    /// Shows or clears the pointer-over look: through the template's visual states when it defines them, or
    /// directly on the root border, since the default template carries no visual states.
    /// </summary>
    internal void ApplyPointerOverState(bool isPointerOver)
    {
        if (!VisualStateManager.GoToState(this, isPointerOver ? VisualStates.StatePointerOver : VisualStates.StateNormal, false))
        {
            SetPointerOverVisual(isPointerOver);
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

        e.Handled = TableView?.OnAnyPointerPressed(this, e) ?? false;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (TableView?.SelectionUnit is not TableViewSelectionUnit.Row)
        {
            e.Handled = true;
        }

        TableView?.EndDragSelection();
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
            TableView.SetEditingCell(this);
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

#if !WINDOWS
        if (IsCurrent)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(20);
                Focus(FocusState.Pointer);
            });
        }
#endif

        DispatcherQueue.TryEnqueue(InvalidateMeasure);
    }

    /// <summary>
    /// Sets the element for the cell.
    /// </summary>
    internal void SetElement()
    {
        var element = Column?.GenerateElement(this, Row?.Content);

        if (element is not null && Column is TableViewBoundColumn boundColumn
            && (boundColumn.ElementStyle ?? boundColumn.DefaultElementStyle) is { } style)
        {
            element.Style = style;
        }

        Content = element;
        _autoWidthDirty = true;
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
        var isSelected = IsSelected;

        if (_appliedSelected != isSelected)
        {
            _appliedSelected = isSelected;
            VisualStates.GoToState(this, false, isSelected ? VisualStates.StateSelected : VisualStates.StateUnselected);
            UpdateSelectionLayer();
        }
    }

    /// <summary>
    /// Shows whether the cell is the current cell, without moving focus.
    /// </summary>
    internal void ApplyCurrentVisualState()
    {
        var isCurrent = IsCurrent;

        if (_appliedCurrent != isCurrent)
        {
            _appliedCurrent = isCurrent;
            VisualStates.GoToState(this, false, isCurrent ? VisualStates.StateCurrent : VisualStates.StateRegular);
            UpdateSelectionLayer();
        }
    }

    /// <summary>
    /// Applies the current cell state to the cell.
    /// </summary>
    internal async void ApplyCurrentCellState(bool skipFocus = false)
    {
        ApplyCurrentVisualState();

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
            GridLineSprite.SetFill(_v_gridLine, TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                                ? TableView.VerticalGridLinesStroke : SharedResources.TransparentBrush);
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

        Style? style = null;

        if (_cellStyles.Count > 0)
        {
            var context = new TableViewConditionalCellStyleContext(Column!, item);

            foreach (var cellStyle in _cellStyles)
            {
                if (cellStyle.Predicate?.Invoke(context) is true)
                {
                    style = cellStyle.Style;
                    break;
                }
            }
        }

        style ??= Column?.CellStyle ?? TableView?.CellStyle;

        // Rows are restyled for every item they show; setting the same style again still costs a call into XAML.
        if (!_hasAppliedStyle || !ReferenceEquals(_appliedStyle, style))
        {
            _hasAppliedStyle = true;
            _appliedStyle = style;
            Style = style;
        }
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
