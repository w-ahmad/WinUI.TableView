using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePointerOver, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateRegular, GroupName = VisualStates.GroupCurrent)]
[TemplateVisualState(Name = VisualStates.StateCurrent, GroupName = VisualStates.GroupCurrent)]
[TemplateVisualState(Name = VisualStates.StateSelected, GroupName = VisualStates.GroupSelection)]
[TemplateVisualState(Name = VisualStates.StateUnselected, GroupName = VisualStates.GroupSelection)]
public class TableViewCell : ContentControl
{
    private TableViewColumn? _column;
    private ScrollViewer? _scrollViewer;
    private ContentPresenter? _contentPresenter;
    private Border? _selectionBorder;
    private Rectangle? _v_gridLine;

    public TableViewCell()
    {
        DefaultStyleKey = typeof(TableViewCell);
        ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        Loaded += OnLoaded;
        ContextRequested += OnContextRequested;
    }

    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (TableView is not null && args.TryGetPosition(sender, out var position))
        {
            TableView.ShowCellContext(this, position);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InvalidateMeasure();
        ApplySelectionState();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPresenter = GetTemplateChild("Content") as ContentPresenter;
        _selectionBorder = GetTemplateChild("SelectionBorder") as Border;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;

        EnsureGridLines();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if ((Content ?? ContentTemplateRoot) is FrameworkElement element)
        {
            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            if (Column is TableViewTemplateColumn)
            {
                if (element is ContentControl contentControl &&
                   (contentControl.Content ?? contentControl.ContentTemplateRoot) is FrameworkElement contentElement)
                {
                    element = contentElement;
                }
                else
                {
                    return base.MeasureOverride(availableSize);
                }
            }

            var v_GridLineStrokeThickness = TableView?.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                            || TableView?.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                            ? TableView.VerticalGridLinesStrokeThickness : 0;

            var contentWidth = Column?.ActualWidth ?? 0d;
            contentWidth -= element.Margin.Left;
            contentWidth -= element.Margin.Right;
            contentWidth -= Padding.Left;
            contentWidth -= Padding.Right;
            contentWidth -= BorderThickness.Left;
            contentWidth -= BorderThickness.Right;
            contentWidth -= _selectionBorder?.BorderThickness.Right ?? 0;
            contentWidth -= _selectionBorder?.BorderThickness.Left ?? 0;
            contentWidth -= v_GridLineStrokeThickness;

            element.MaxWidth = double.PositiveInfinity;
            #endregion
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (Column is not null)
            {
                var desiredWidth = element.DesiredSize.Width;
                desiredWidth += Padding.Left;
                desiredWidth += Padding.Right;
                desiredWidth += BorderThickness.Left;
                desiredWidth += BorderThickness.Right;
                desiredWidth += _selectionBorder?.BorderThickness.Right ?? 0;
                desiredWidth += _selectionBorder?.BorderThickness.Left ?? 0;
                desiredWidth += v_GridLineStrokeThickness;

                Column.DesiredWidth = Math.Max(Column.DesiredWidth, desiredWidth);
            }

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            if (_contentPresenter is not null)
            {
                if (contentWidth < 0)
                {
                    _contentPresenter.Visibility = Visibility.Collapsed;
                }
                else
                {
                    element.MaxWidth = contentWidth;
                    _contentPresenter.Visibility = Visibility.Visible;
                }
            }
            #endregion
        }

        return base.MeasureOverride(availableSize);
    }

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

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

        MakeSelection();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            TableView.SelectionStartCellSlot = TableView.SelectionUnit is not TableViewSelectionUnit.Row || !IsReadOnly ? Slot : default; ;
            TableView.SelectionStartRowIndex = Index;
            CapturePointer(e.Pointer);
        }
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
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

    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (PointerCaptures?.Any() is true)
        {
            var cell = FindCell(e.Position);

            if (cell is not null && cell != this)
            {
                var ctrlKey = KeyboardHelper.IsCtrlKeyDown();
                TableView?.MakeSelection(cell.Slot, true, ctrlKey);
            }
        }
    }

    private TableViewCell? FindCell(Point position)
    {
        _scrollViewer ??= TableView?.FindDescendant<ScrollViewer>();

        if (_scrollViewer is { })
        {
            var transform = _scrollViewer.TransformToVisual(this).Inverse;
            var point = transform.TransformPoint(position);
            var transformedPoint = _scrollViewer.TransformToVisual(null).TransformPoint(point);
            return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, _scrollViewer)
                                   .OfType<TableViewCell>()
                                   .FirstOrDefault();
        }

        return null;
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        if (!IsReadOnly && TableView is not null && !TableView.IsEditing && !Column?.UseSingleElement is true)
        {
            PrepareForEdit();

            TableView.IsEditing = true;
        }
    }

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

            TableView.IsEditing = false;
            TableView.MakeSelection(Slot, shiftKey, ctrlKey);
        }
    }

    internal async void PrepareForEdit()
    {
        SetEditingElement();

        await Task.Delay(20);

        if ((Content ?? ContentTemplateRoot) is UIElement editingElement)
        {
            editingElement.Focus(FocusState.Programmatic);
        }
    }

    internal void SetElement()
    {
        Content = Column?.GenerateElement(this, Row?.Content);
    }

    private void SetEditingElement()
    {
        if (Column?.UseSingleElement is false)
        {
            Content = Column.GenerateEditingElement(this, Row?.Content);
        }

        if (TableView is not null)
        {
            TableView.IsEditing = true;
        }
    }

    internal void RefreshElement()
    {
        Column?.RefreshElement(this, Content);
    }

    internal void ApplySelectionState()
    {
        var stateName = IsSelected ? VisualStates.StateSelected : VisualStates.StateUnselected;
        VisualStates.GoToState(this, false, stateName);
    }

    internal void ApplyCurrentCellState()
    {
        var stateName = IsCurrent ? VisualStates.StateCurrent : VisualStates.StateRegular;
        VisualStates.GoToState(this, false, stateName);

        if (IsCurrent)
        {
            Focus(FocusState.Programmatic);

            if ((Content ?? ContentTemplateRoot) is UIElement { IsHitTestVisible: true, IsTabStop: true } element)
            {
                element.Focus(FocusState.Programmatic);
            }
        }
    }

    internal void UpdateElementState()
    {
        Column?.UpdateElementState(this, Row?.Content);
    }

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

    internal void EnsureGridLines()
    {
        if (_v_gridLine is not null && TableView is not null)
        {
            _v_gridLine.Fill = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                               ? TableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
            _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
            _v_gridLine.Visibility = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                     || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                     ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsReadOnly => TableView?.IsReadOnly is true || Column is TableViewTemplateColumn { EditingTemplate: null } or { IsReadOnly: true };

    internal TableViewCellSlot Slot => new(Row?.Index ?? -1, Index);

    internal int Index { get; set; }

    public bool IsSelected => TableView?.SelectedCells.Contains(Slot) is true;
    public bool IsCurrent => TableView?.CurrentCellSlot == Slot;

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

    public TableViewRow? Row { get; internal set; }

    public TableView? TableView { get; internal set; }

}
