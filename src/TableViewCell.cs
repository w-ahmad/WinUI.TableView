using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
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
    private const double TreeExpanderSlotWidth = 20d;
    private TableViewColumn? _column;
    private ScrollViewer? _scrollViewer;
    private ContentPresenter? _contentPresenter;
    private Button? _treeExpanderButton;
    private Border? _selectionBorder;
    private Rectangle? _v_gridLine;
    private Thickness _basePadding;
    private bool _isBasePaddingInitialized;
    private object? _uneditedValue;
    private RoutedEventArgs? _editingArgs;
    private IList<TableViewConditionalCellStyle>? _cellStyles;

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

        if (!_isBasePaddingInitialized)
        {
            _basePadding = Padding;
            _isBasePaddingInitialized = true;
        }

        _contentPresenter = GetTemplateChild("Content") as ContentPresenter;
        _treeExpanderButton = GetTemplateChild("TreeExpanderButton") as Button;
        _selectionBorder = GetTemplateChild("SelectionBorder") as Border;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;

        if (_treeExpanderButton is not null)
        {
            _treeExpanderButton.Click -= OnTreeExpanderButtonClicked;
            _treeExpanderButton.Click += OnTreeExpanderButtonClicked;
            _treeExpanderButton.KeyDown -= OnTreeExpanderButtonKeyDown;
            _treeExpanderButton.KeyDown += OnTreeExpanderButtonKeyDown;
        }

        EnsureGridLines();
        EnsureStyle(Row?.Content);
        ApplyHierarchyPresentation();
    }

    private void OnTreeExpanderButtonClicked(object sender, RoutedEventArgs e)
    {
        if (TableView is null || Row?.Content is null)
        {
            return;
        }

        TableView.ToggleItemExpansion(Row.Content);
    }

    private void OnTreeExpanderButtonKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (TableView is null || Row?.Content is null || !TableView.HasChildItems(Row.Content))
        {
            return;
        }

        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            TableView.ToggleItemExpansion(Row.Content);
            e.Handled = true;
            return;
        }

        if (e.Key is VirtualKey.Right && !TableView.IsItemExpanded(Row.Content))
        {
            TableView.SetItemExpanded(Row.Content, true);
            e.Handled = true;
            return;
        }

        if (e.Key is VirtualKey.Left && TableView.IsItemExpanded(Row.Content))
        {
            TableView.SetItemExpanded(Row.Content, false);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        ApplyHierarchyPresentation();

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

    /// <summary>
    /// Applies hierarchy-related visual settings for this cell.
    /// </summary>
    internal void ApplyHierarchyPresentation()
    {
        if (!_isBasePaddingInitialized)
        {
            _basePadding = Padding;
            _isBasePaddingInitialized = true;
        }

        if (TableView is null || Row is null)
        {
            Padding = _basePadding;
            return;
        }

        var hierarchyLevel = TableView.GetHierarchyLevel(Row.Content);
        var indent = hierarchyLevel > 0 ? hierarchyLevel * TableView.HierarchyIndent : 0d;
        var isTreeColumn = TableView.IsHierarchicalEnabled && Index == 0;

        if (isTreeColumn)
        {
            var hasChildren = TableView.HasChildItems(Row.Content);
            var isExpanded = TableView.IsItemExpanded(Row.Content);

            if (_treeExpanderButton is not null)
            {
                _treeExpanderButton.Visibility = Visibility.Visible;
                _treeExpanderButton.Margin = new Thickness(_basePadding.Left + indent, 0, 4, 0);
                _treeExpanderButton.Content = hasChildren ? (isExpanded ? "▼" : "▶") : string.Empty;
                _treeExpanderButton.IsHitTestVisible = hasChildren;
                _treeExpanderButton.Opacity = hasChildren ? 1d : 0d;

                if (hasChildren)
                {
                    var itemLabel = Row.Content?.ToString();
                    if (string.IsNullOrWhiteSpace(itemLabel))
                    {
                        itemLabel = $"row {Row.Index + 1}";
                    }

                    AutomationProperties.SetName(_treeExpanderButton, isExpanded
                        ? $"Collapse {itemLabel}"
                        : $"Expand {itemLabel}");
                    AutomationProperties.SetHelpText(_treeExpanderButton, "Hierarchy expander");
                }
                else
                {
                    AutomationProperties.SetName(_treeExpanderButton, string.Empty);
                    AutomationProperties.SetHelpText(_treeExpanderButton, string.Empty);
                }
            }

            Padding = new Thickness(
                _basePadding.Left + indent + TreeExpanderSlotWidth,
                _basePadding.Top,
                _basePadding.Right,
                _basePadding.Bottom);
            return;
        }

        if (_treeExpanderButton is not null)
        {
            _treeExpanderButton.Visibility = Visibility.Collapsed;
            _treeExpanderButton.Content = "▶";
            _treeExpanderButton.IsHitTestVisible = false;
            _treeExpanderButton.Opacity = 0d;
            AutomationProperties.SetName(_treeExpanderButton, string.Empty);
            AutomationProperties.SetHelpText(_treeExpanderButton, string.Empty);
        }

        Padding = _basePadding;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Column is not null && Row is not null && _contentPresenter is not null && Content is FrameworkElement element)
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

            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var desiredWidth = element.DesiredSize.Width;
            desiredWidth += Padding.Left;
            desiredWidth += Padding.Right;
            desiredWidth += BorderThickness.Left;
            desiredWidth += BorderThickness.Right;
            desiredWidth += _selectionBorder?.BorderThickness.Right ?? 0;
            desiredWidth += _selectionBorder?.BorderThickness.Left ?? 0;
            desiredWidth += _v_gridLine?.ActualWidth ?? 0d;

            Column.DesiredWidth = Math.Max(Column.DesiredWidth, desiredWidth);

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            var contentWidth = Column.ActualWidth;
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
    protected override async void OnTapped(TappedRoutedEventArgs e)
    {
        if (IsTreeExpanderInteraction(e.OriginalSource))
        {
            base.OnTapped(e);
            return;
        }

        base.OnTapped(e);

        if ((TableView?.IsEditing ?? false) &&
             TableView.CurrentCellSlot != Slot &&
             TableView.CurrentCellSlot.HasValue &&
             TableView.GetCellFromSlot(TableView.CurrentCellSlot.Value) is { } currentCell)
        {
            e.Handled = !TableView.EndCellEditing(TableViewEditAction.Commit, currentCell);

            if (e.Handled) return;
        }

        if (TableView?.CurrentCellSlot != Slot)
        {
            MakeSelection();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (IsTreeExpanderInteraction(e.OriginalSource))
        {
            base.OnPointerPressed(e);
            return;
        }

        base.OnPointerPressed(e);

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            TableView.SelectionStartCellSlot = TableView.SelectionUnit is not TableViewSelectionUnit.Row || !IsReadOnly ? Slot : default; ;
            TableView.SelectionStartRowIndex = Index;
            CapturePointer(e.Pointer);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        if (IsTreeExpanderInteraction(e.OriginalSource))
        {
            base.OnPointerReleased(e);
            return;
        }

        base.OnPointerReleased(e);

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            var cell = FindCell(e.GetCurrentPoint(this).Position);
            TableView.SelectionStartCellSlot = TableView.SelectionUnit is not TableViewSelectionUnit.Row || !IsReadOnly ? cell?.Slot : default;
            TableView.SelectionStartRowIndex = cell?.Slot.Row;
        }

        ReleasePointerCaptures();

        e.Handled = true;
    }

    private bool IsTreeExpanderInteraction(object? originalSource)
    {
        if (_treeExpanderButton is null || originalSource is not DependencyObject source)
        {
            return false;
        }

        return ReferenceEquals(source, _treeExpanderButton)
            || ReferenceEquals(source.FindAscendant<Button>(), _treeExpanderButton);
    }

    /// <inheritdoc/>
    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (PointerCaptures?.Any() is true)
        {
            var cell = FindCell(e.Position);

            if (cell is not null && cell.Slot != TableView?.CurrentCellSlot)
            {
                var ctrlKey = KeyboardHelper.IsCtrlKeyDown();
                TableView?.MakeSelection(cell.Slot, true, ctrlKey);
            }
        }
    }

    /// <summary>
    /// Gets the height of the horizontal gridlines/>.
    /// </summary>
    private double GetHorizontalGridlineHeight()
    {
        return TableView?.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
            ? TableView.HorizontalGridLinesStrokeThickness : 0d;
    }

    /// <summary>
    /// Finds the cell at the specified position.
    /// </summary>
    private TableViewCell? FindCell(Point position)
    {
        _scrollViewer ??= TableView?.FindDescendant<ScrollViewer>();
        if (_scrollViewer is null) return null;

        var transformedPoint = TransformToVisual(null).TransformPoint(position);
#if WINDOWS
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, _scrollViewer)
#else
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, _scrollViewer, true)
                               .OfType<ContentPresenter>()
                               .Where(x => x.Name is "Content")
                               .Select(x => x.FindAscendant<TableViewCell>() is { } header ? header : default)
#endif
                               .OfType<TableViewCell>()
                               .FirstOrDefault();
    }

    /// <inheritdoc/>
    protected override async void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        var eventArgs = new TableViewCellDoubleTappedEventArgs(Slot, this, Row?.Content);
        TableView?.OnCellDoubleTapped(eventArgs);
        e.Handled = eventArgs.Handled;

        if (e.Handled) return;

        base.OnDoubleTapped(e);

        if (!IsReadOnly && TableView is not null && !TableView.IsEditing && !Column?.UseSingleElement is true)
        {
            e.Handled = await BeginCellEditing(e);
        }
        else
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Makes a selection based on the current cell.
    /// </summary>
    private void MakeSelection()
    {
        var shiftKey = KeyboardHelper.IsShiftKeyDown();
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();

        if (TableView is null || Column is null)
        {
            return;
        }

        if ((TableView.IsEditing || Column.UseSingleElement) && IsCurrent)
        {
            return;
        }

        if (IsSelected && (ctrlKey || TableView.SelectionMode is ListViewSelectionMode.Multiple) && !shiftKey)
        {
            TableView.DeselectCell(Slot);
        }
        else
        {
            if (Column.UseSingleElement)
            {
                TableView.DeselectCell(Slot);
            }

            TableView.MakeSelection(Slot, shiftKey, ctrlKey);
        }

        TableView.SetIsEditing(false);
        TableView.UpdateCornerButtonState();
    }

    /// <summary>
    /// Initiates editing mode for the current cell, raising the beginning edit event and allowing cancellation.
    /// </summary>
    /// <param name="editingArgs">The event data associated with the editing request. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if cell editing was
    /// successfully started; otherwise, <see langword="false"/> if the operation was canceled.</returns>
    internal async Task<bool> BeginCellEditing(RoutedEventArgs editingArgs)
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
        // Prevent template realization for group header rows; they should have no cell content.
        if (TableView?.IsGroupHeaderItem(Row?.Content) is true)
        {
            Content = null;
            DataContext = null;
            return;
        }

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
    internal async void ApplyCurrentCellState()
    {
        var stateName = IsCurrent ? VisualStates.StateCurrent : VisualStates.StateRegular;
        VisualStates.GoToState(this, false, stateName);

        if (IsCurrent)
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
    public bool IsReadOnly => TableView?.IsReadOnly is true || Column is TableViewTemplateColumn { EditingTemplate: null } or { IsReadOnly: true };

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
        get => _column;
        internal set
        {
            if (_column != value)
            {
                _column = value;
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
}
