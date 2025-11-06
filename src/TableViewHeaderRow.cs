using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using WinUI.TableView.Converters;

namespace WinUI.TableView;

/// <summary>
/// Represents the header row in a TableView.
/// </summary>
[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateNoButton, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllButton, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllButtonDisabled, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllCheckBox, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllCheckBoxDisabled, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateOptionsButton, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateOptionsButtonDisabled, GroupName = VisualStates.GroupCornerButton)]
public partial class TableViewHeaderRow : Control
{
    private Panel? _cornerButtonPanel;
    private Button? _optionsButton;
    private CheckBox? _selectAllCheckBox;
    private Rectangle? _v_gridLine;
    private Rectangle? _h_gridLine;
    private StackPanel? _frozenHeadersPanel;
    private StackPanel? _scrollableHeadersPanel;
    private Border? _columnDropIndicator;
    private TranslateTransform? _columnDropIndicatorTransform;
    private bool _calculatingHeaderWidths;
    private DispatcherTimer? _timer;
    private readonly Dictionary<DependencyProperty, long> _callbackTokens = [];

    /// <summary>
    /// Initializes a new instance of the TableViewHeaderRow class.
    /// </summary>
    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _cornerButtonPanel = GetTemplateChild("CornerButtonPanel") as Panel;
        _optionsButton = GetTemplateChild("optionsButton") as Button;
        _selectAllCheckBox = GetTemplateChild("selectAllCheckBox") as CheckBox;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;
        _frozenHeadersPanel = GetTemplateChild("FrozenHeadersPanel") as StackPanel;
        _scrollableHeadersPanel = GetTemplateChild("ScrollableHeadersPanel") as StackPanel;
        _columnDropIndicator = GetTemplateChild("ColumnDropIndicator") as Border;
        _columnDropIndicatorTransform = GetTemplateChild("ColumnDropIndicatorTransform") as TranslateTransform;

        if (TableView is null)
        {
            return;
        }

        if (_optionsButton is not null)
        {
            _optionsButton.DataContext = new OptionsFlyoutViewModel(TableView);
        }

        if (GetTemplateChild("selectAllButton") is Button selectAllButton)
        {
            selectAllButton.Tapped += OnSelectAllButtonClicked;
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Checked += OnSelectAllCheckBoxChecked;
            _selectAllCheckBox.Unchecked += OnSelectAllCheckBoxUnchecked;
        }

        if (TableView.Columns is not null)
        {
            AddHeaders(TableView.Columns.VisibleColumns);
        }

        _cornerButtonPanel?.SetBinding(WidthProperty, new Binding
        {
            Source = TableView,
            Path = new PropertyPath(nameof(TableView.CellsHorizontalOffset))
        });

        SetExportOptionsVisibility();
        SetCornerButtonState();
        SetHeadersVisibility();
        EnsureGridLines();
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        if (_scrollableHeadersPanel is not null && _frozenHeadersPanel is not null && TableView is not null && _scrollableHeadersPanel.ActualWidth > 0)
        {
            var frozenOffset = _frozenHeadersPanel.ActualOffset.X + _frozenHeadersPanel.ActualWidth;
            var headersOffset = -TableView.HorizontalOffset + frozenOffset;
            var xClip = (headersOffset * -1) + frozenOffset;

            _scrollableHeadersPanel.Arrange(new Rect(headersOffset, 0, _scrollableHeadersPanel.ActualWidth, _scrollableHeadersPanel.ActualHeight));
            _scrollableHeadersPanel.Clip = headersOffset >= frozenOffset ? null :
                new RectangleGeometry
                {
                    Rect = new Rect(xClip, 0, _scrollableHeadersPanel.ActualWidth - xClip, finalSize.Height)
                };
        }

        return finalSize;
    }

    /// <summary>
    /// Handles the collection changed event for the columns.
    /// </summary>
    private void OnTableViewColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> newItems)
        {
            AddHeaders(newItems.Where(x => x.Visibility == Visibility.Visible));
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> oldItems)
        {
            RemoveHeaders(oldItems);
        }
        else if (e.Action == NotifyCollectionChangedAction.Move && e.NewItems?.Count > 0)
        {
            MoveHeaders(e.NewItems.OfType<TableViewColumn>().First(), e.NewStartingIndex);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && _scrollableHeadersPanel is not null)
        {
            ClearHeaders();
        }
    }

    /// <summary>
    /// Moves the header associated with the specified column to a new index.
    /// </summary>
    /// <param name="column">The column associated with the header to move.</param>
    /// <param name="newIndex">The new index to move the header to.</param>

    private void MoveHeaders(TableViewColumn column, int newIndex)
    {
        if (Headers.FirstOrDefault(h => h.Column == column) is { } header)
        {
            RemoveHeader(header);
            InsertHeader(header);
        }

        if (newIndex >= 0 && newIndex < TableView?.FrozenColumnCount &&
            _frozenHeadersPanel?.Children.OfType<TableViewColumnHeader>().LastOrDefault() is { } frozenHeader)
        {
            RemoveHeader(frozenHeader);
            InsertHeader(frozenHeader);
        }
    }

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    private void OnColumnPropertyChanged(object? sender, TableViewColumnPropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TableViewColumn.Visibility))
        {
            if (e.Column.Visibility == Visibility.Visible)
            {
                AddHeaders([e.Column]);
            }
            else
            {
                RemoveHeaders([e.Column]);
            }
        }
        else if ((e.PropertyName is nameof(TableViewColumn.Order) ||
            e.PropertyName is nameof(TableViewColumn.IsFrozen)) &&
            e.Column.Visibility is Visibility.Visible)
        {
            RemoveHeaders([e.Column]);
            AddHeaders([e.Column]);
        }
        else if (e.PropertyName is nameof(TableViewColumn.Width) or
                                   nameof(TableViewColumn.MinWidth) or
                                   nameof(TableViewColumn.MaxWidth))
        {
            if (!_calculatingHeaderWidths)
            {
                CalculateHeaderWidths();
            }
        }
        else if (e.PropertyName is nameof(TableViewColumn.DesiredWidth))
        {
            if (_timer is null)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                _timer.Tick += OnTimerTick;
            }

            _timer.Stop();
            _timer.Start();
        }
    }

    /// <summary>
    /// Handles the timer tick event.
    /// </summary>
    private void OnTimerTick(object? sender, object e)
    {
        if (!_calculatingHeaderWidths)
        {
            CalculateHeaderWidths();
        }

        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    /// <summary>
    /// Removes headers for the specified columns.
    /// </summary>
    private void RemoveHeaders(IEnumerable<TableViewColumn> columns)
    {
        foreach (var column in columns)
        {
            var header = Headers.FirstOrDefault(x => x.Column == column);
            if (header is not null)
            {
                RemoveHeader(header);
            }
        }
    }

    /// <summary>
    /// Adds headers for the specified columns.
    /// </summary>
    private void AddHeaders(IEnumerable<TableViewColumn> columns)
    {
        if (TableView is not null && _scrollableHeadersPanel is not null)
        {
            foreach (var column in columns)
            {
                var header = new TableViewColumnHeader { DataContext = column, Column = column };
                column.HeaderControl = header;

                InsertHeader(header);

                header.SetBinding(ContentControl.ContentProperty,
                                  new Binding { Path = new PropertyPath(nameof(TableViewColumn.Header)) });
            }

            CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Calculates the widths of the headers.
    /// </summary>
    internal void CalculateHeaderWidths()
    {
        if (TableView?.ActualWidth > 0)
        {
            _calculatingHeaderWidths = true;

            var allColumns = TableView.Columns.VisibleColumns.ToList();
            var starColumns = allColumns.Where(x => x.Width.IsStar).ToList();
            var autoColumns = allColumns.Where(x => x.Width.IsAuto).ToList();
            var absoluteColumns = allColumns.Where(x => x.Width.IsAbsolute).ToList();

            var height = ActualHeight;
            var availableWidth = TableView.ActualWidth - 32;
            var starUnitWeight = starColumns.Select(x => x.Width.Value).Sum();
            var fixedWidth = autoColumns.Select(x =>
            {
                if (x.HeaderControl is { } header)
                {
                    header.Measure(new Size(double.PositiveInfinity, height));
                    return Math.Max(x.DesiredWidth, header.DesiredSize.Width);
                }

                return x.DesiredWidth;
            }).Sum();
            fixedWidth += absoluteColumns.Select(x => x.ActualWidth).Sum();

            availableWidth -= fixedWidth;
            var starUnitWidth = starUnitWeight > 0 ? availableWidth / starUnitWeight : 0;

            var starWidthAdjusted = false;
            var startColumnsToAdjust = starColumns.ToList();
            do
            {
                starWidthAdjusted = false;

                foreach (var column in startColumnsToAdjust)
                {
                    if (column.HeaderControl is { } header)
                    {
                        var width = starUnitWidth * column.Width.Value;
                        var minWidth = column.MinWidth ?? TableView.MinColumnWidth;
                        var maxWidth = column.MaxWidth ?? TableView.MaxColumnWidth;

                        width = width < minWidth ? minWidth : width;
                        width = width > maxWidth ? maxWidth : width;

                        if (width != starUnitWidth * column.Width.Value)
                        {
                            availableWidth -= width;
                            starUnitWeight -= column.Width.Value;
                            starUnitWidth = starUnitWeight > 0 ? availableWidth / starUnitWeight : 0;
                            startColumnsToAdjust.Remove(column);
                            starWidthAdjusted = true;
                            break;
                        }
                    }
                }

            } while (starWidthAdjusted);


            foreach (var column in allColumns)
            {
                if (column.HeaderControl is { } header)
                {
                    var width = column.Width.IsStar
                                ? starUnitWidth * column.Width.Value
                                : column.Width.IsAbsolute ? column.Width.Value
                                : Math.Max(header.DesiredSize.Width, column.DesiredWidth);

                    var minWidth = column.MinWidth ?? TableView.MinColumnWidth;
                    var maxWidth = column.MaxWidth ?? TableView.MaxColumnWidth;

                    width = width < minWidth ? minWidth : width;
                    width = width > maxWidth ? maxWidth : width;
                    header.Width = width;
                    header.MaxWidth = width;

                    DispatcherQueue.TryEnqueue(() =>
                        header.Measure(
                            new Size(header.Width,
                            _scrollableHeadersPanel?.ActualHeight ?? ActualHeight)));
                }
            }

            _calculatingHeaderWidths = false;

            TableView.UpdateHorizontalScrollBarMargin();
        }
    }

    /// <summary>
    /// Sets the visibility of the export options.
    /// </summary>
    private void SetExportOptionsVisibility()
    {
        var binding = new Binding
        {
            Path = new PropertyPath(nameof(TableView.ShowExportOptions)),
            Source = TableView,
            Converter = new BoolToVisibilityConverter()
        };

        if (GetTemplateChild("ExportAllMenuItem") is MenuFlyoutItem exportAll)
        {
            exportAll.SetBinding(VisibilityProperty, binding);
        }

        if (GetTemplateChild("ExportSelectedMenuItem") is MenuFlyoutItem exportSelected)
        {
            exportSelected.SetBinding(VisibilityProperty, binding);
        }
    }

    /// <summary>
    /// Handles the selection changed event for the TableView.
    /// </summary>
    private void OnTableViewSelectionChanged()
    {
        if (TableView is not null && _selectAllCheckBox is not null)
        {
            if (TableView.Items.Count == 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = false;
            }
            else if (TableView.SelectedItems.Count == TableView.Items.Count)
            {
                _selectAllCheckBox.IsChecked = true;
                _selectAllCheckBox.IsEnabled = true;
            }
            else if (TableView.SelectedItems.Count > 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = true;
            }
            else
            {
                _selectAllCheckBox.IsChecked = false;
                _selectAllCheckBox.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// Sets the state of the select all button.
    /// </summary>
    internal void SetCornerButtonState()
    {
        var stateName = VisualStates.StateNoButton;

        if (TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple })
        {
            stateName = TableView.IsEditing ? VisualStates.StateSelectAllCheckBoxDisabled : VisualStates.StateSelectAllCheckBox;
        }
        else if (TableView is { CornerButtonMode: TableViewCornerButtonMode.Options })
        {
            stateName = TableView.IsEditing ? VisualStates.StateOptionsButtonDisabled : VisualStates.StateOptionsButton;
        }
        else if (TableView is { CornerButtonMode: TableViewCornerButtonMode.SelectAll })
        {
            stateName = TableView.IsEditing ? VisualStates.StateSelectAllButtonDisabled : VisualStates.StateSelectAllButton;
        }

        VisualStates.GoToState(this, false, stateName);
    }

    /// <summary>
    /// Handles the SelectAllButton clicked event.
    /// </summary>
    private void OnSelectAllButtonClicked(object sender, TappedRoutedEventArgs e)
    {
        TableView?.SelectAll();
    }

    /// <summary>
    /// Handles the Checked event for the select all checkbox.
    /// </summary>
    private void OnSelectAllCheckBoxChecked(object sender, RoutedEventArgs e)
    {
        TableView?.SelectAll();
    }

    /// <summary>
    /// Handles the Unchecked event for the select all checkbox.
    /// </summary>
    private void OnSelectAllCheckBoxUnchecked(object sender, RoutedEventArgs e)
    {
        TableView?.DeselectAll();
    }

    /// <summary>
    /// Ensures grid lines are applied to the header row.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (_h_gridLine is not null && TableView is not null)
        {
            _h_gridLine.Fill = TableView.HorizontalGridLinesStroke;
            _h_gridLine.Height = TableView.HorizontalGridLinesStrokeThickness;
            _h_gridLine.Visibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
                                     ? Visibility.Visible : Visibility.Collapsed;

            if (_v_gridLine is not null)
            {
                var vGridLinesVisibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                           || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical;
                var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
                var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
                var isDetailsToggleButtonVisible = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded
                                                    && (TableView.RowDetailsTemplate is not null || TableView.RowDetailsTemplateSelector is not null);


                _v_gridLine.Fill = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                   ? TableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
                _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
                _v_gridLine.Visibility = vGridLinesVisibility && (areHeadersVisible || isMultiSelection || isDetailsToggleButtonVisible) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        foreach (var header in Headers)
        {
            header.EnsureGridLines();
        }

        if (!_calculatingHeaderWidths)
        {
            CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Gets the previous header in the header row.
    /// </summary>
    internal TableViewColumnHeader? GetPreviousHeader(TableViewColumnHeader currentHeader)
    {
        if (TableView is { Columns: { } } && currentHeader is { Column: { } })
        {
            var index = TableView.Columns.VisibleColumns.IndexOf(currentHeader.Column);

            if (index > 0)
            {
                return TableView.Columns.VisibleColumns[index - 1].HeaderControl;
            }
        }

        return default;
    }

    /// <summary>
    /// Sets the visibility of the row header based on the TableView settings.
    /// </summary>
    internal void SetHeadersVisibility()
    {
        if (_cornerButtonPanel is not null && TableView is not null)
        {
            var areRowHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
            var isRowDetailExpandable = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded;

            _cornerButtonPanel.Visibility = areRowHeadersVisible || isMultiSelection || isRowDetailExpandable
                ? Visibility.Visible : Visibility.Collapsed;
        }

        SetCornerButtonState();
        EnsureGridLines();
    }

    /// <summary>
    /// Shows the column drop indicator at the specified position.
    /// </summary>
    internal void ShowColumnDropIndicator(double position, RenderTargetBitmap headerVisuals)
    {
        if (_columnDropIndicator is not null && FindHeader(new(position, ActualHeight / 2)) is { Column: { } dropColumn } dropHeader)
        {
            var dropColumnIndex = TableView?.Columns.VisibleColumns.IndexOf(dropColumn) ?? 0;
            var transform = dropHeader.TransformToVisual(this);
            var dropHeaderX = transform.TransformPoint(new Point(0, 0)).X;
            var midPoint = dropHeaderX + (dropHeader.ActualWidth / 2);
            var x = dropHeaderX;
            x += midPoint < position ? dropHeader.ActualWidth : 0d;
            x -= _columnDropIndicator.ActualWidth / 2;
            dropColumnIndex += midPoint > position ? -1 : 0;

            _columnDropIndicator.DataContext = new DragIndicatorData(dropColumnIndex, headerVisuals);
            _columnDropIndicator.Visibility = Visibility.Visible;

            if (_columnDropIndicatorTransform is not null)
            {
                _columnDropIndicatorTransform.X = x;
            }
        }
    }

    internal void ColumnDropCompleted(TableViewColumn column)
    {
        if (_columnDropIndicator is { DataContext: DragIndicatorData data } && TableView is not null)
        {
            _columnDropIndicator.Visibility = Visibility.Collapsed;

            var sourceIndex = TableView.Columns.VisibleColumns.IndexOf(column);
            var dropIndex = sourceIndex > data.DropIndex ? data.DropIndex + 1 : data.DropIndex;
            dropIndex = Math.Clamp(dropIndex, 0, TableView.Columns.VisibleColumns.Count - 1);

            var reorderingArgs = new TableViewColumnReorderingEventArgs(column, dropIndex);
            TableView.OnColumnReordering(reorderingArgs);

            if (reorderingArgs.Cancel) return;

            TableView.DeselectAll();
            TableView.Columns.Move(sourceIndex, dropIndex);

            var reorderedArgs = new TableViewColumnReorderedEventArgs(column, dropIndex);
            TableView.OnColumnReordered(reorderedArgs);
        }
    }

    /// <summary>
    /// Finds the header at the specified position.
    /// </summary>
    private TableViewColumnHeader? FindHeader(Point position)
    {
        var transformedPoint = TransformToVisual(null).TransformPoint(position);
#if WINDOWS
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, this)
#else
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, TableView, true)
                               .OfType<ContentPresenter>()
                               .Where(x => x.Name is "ContentPresenter")
                               .Select(x => x.FindAscendant<TableViewColumnHeader>() is { } header ? header : default)
#endif
                               .OfType<TableViewColumnHeader>()
                               .FirstOrDefault();
    }

    /// <summary>
    /// Inserts a header at the specified index.
    /// </summary>
    /// <param name="header">The header to insert.</param>
    public void InsertHeader(TableViewColumnHeader header)
    {
        if (TableView is null || header is not { Column: { } column }) return;

        var _frozenColumns = TableView.Columns.VisibleColumns.Where(x => x.IsFrozen).ToList();
        var _scrollableColumns = TableView.Columns.VisibleColumns.Where(x => !x.IsFrozen).ToList();

        if (header is { Column.IsFrozen: true } && _frozenHeadersPanel is not null)
        {
            var index = _frozenColumns.IndexOf(column);
            index = Math.Min(index, _frozenColumns.Count);
            index = Math.Max(index, 0); // handles -ve index;

            _frozenHeadersPanel.Children.Insert(index, header);
        }
        else if (_scrollableHeadersPanel is not null)
        {
            var index = _scrollableColumns.IndexOf(column);
            index = Math.Min(index, _scrollableColumns.Count);
            index = Math.Max(index, 0); // handles -ve index;

            _scrollableHeadersPanel.Children.Insert(index, header);
        }
    }

    /// <summary>
    /// Removes a header from the header row.
    /// </summary>
    /// <param name="header">The header to remove.</param>
    public void RemoveHeader(TableViewColumnHeader header)
    {
        if (_frozenHeadersPanel?.Children.Contains(header) ?? false)
        {
            _frozenHeadersPanel.Children.Remove(header);
        }
        else if (_scrollableHeadersPanel?.Children.Contains(header) ?? false)
        {
            _scrollableHeadersPanel.Children.Remove(header);
        }
    }

    /// <summary>
    /// Clears all headers from the header row.
    /// </summary>
    public void ClearHeaders()
    {
        _frozenHeadersPanel?.Children.Clear();
        _scrollableHeadersPanel?.Children.Clear();
    }

    /// <summary>
    /// Handles the SizeChanged event for the TableView.
    /// </summary>
    private void OnTableViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        CalculateHeaderWidths();
    }

    /// <summary>
    /// Handles changes to the TableView property.
    /// </summary>
    private static void OnTableViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewHeaderRow headerRow)
        {
            headerRow.OnTableViewChanged(e);
        }
    }

    /// <summary>
    /// Handles changes to the TableView property.
    /// </summary>
    private void OnTableViewChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TableView oldTableView)
        {
            oldTableView.SizeChanged -= OnTableViewSizeChanged;
            oldTableView.SelectionChanged -= delegate { OnTableViewSelectionChanged(); };
            oldTableView.Items.VectorChanged -= delegate { OnTableViewSelectionChanged(); };
            oldTableView.Columns.CollectionChanged -= OnTableViewColumnsCollectionChanged;
            oldTableView.Columns.ColumnPropertyChanged -= OnColumnPropertyChanged;

            oldTableView.UnregisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, _callbackTokens[ListViewBase.SelectionModeProperty]);
            oldTableView.UnregisterPropertyChangedCallback(TableView.CornerButtonModeProperty, _callbackTokens[TableView.CornerButtonModeProperty]);
            oldTableView.UnregisterPropertyChangedCallback(TableView.ItemsSourceProperty, _callbackTokens[TableView.ItemsSourceProperty]);
        }

        if (e.NewValue is TableView newTableView)
        {
            newTableView.SizeChanged += OnTableViewSizeChanged;
            newTableView.SelectionChanged += delegate { OnTableViewSelectionChanged(); };
            newTableView.Items.VectorChanged += delegate { OnTableViewSelectionChanged(); };
            newTableView.Columns.CollectionChanged += OnTableViewColumnsCollectionChanged;
            newTableView.Columns.ColumnPropertyChanged += OnColumnPropertyChanged;

            _callbackTokens[TableView.ItemsSourceProperty] =
                newTableView.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, delegate { OnTableViewSelectionChanged(); });
        }
    }

    /// <summary>
    /// Gets the list of headers in the header row.
    /// </summary>
    public IReadOnlyList<TableViewColumnHeader> Headers =>
        [.. _frozenHeadersPanel?.Children.OfType<TableViewColumnHeader>() ?? [],
         .. _scrollableHeadersPanel?.Children.OfType<TableViewColumnHeader>() ?? []];

    /// <summary>
    /// Gets or sets the TableView associated with the header row.
    /// </summary>
    public TableView? TableView
    {
        get => (TableView?)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    /// <summary>
    /// Identifies the TableView dependency property.
    /// </summary>
    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewHeaderRow), new PropertyMetadata(null, OnTableViewChanged));
}

/// <summary>
/// Provides data for the drag indicator during column reordering.
/// </summary>
/// <param name="DropIndex">The index where the column is dropped.</param>
/// <param name="Visuals">The visuals associated with the drag indicator.</param>
file record struct DragIndicatorData(int DropIndex, RenderTargetBitmap Visuals);
