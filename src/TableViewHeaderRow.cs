using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
    private ColumnDefinition? _cornerButtonColumn;
    private Button? _optionsButton;
    private CheckBox? _selectAllCheckBox;
    private Rectangle? _v_gridLine;
    private Rectangle? _h_gridLine;
    private StackPanel? _headersStackPanel;
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

        _cornerButtonColumn = GetTemplateChild("cornerButtonColumn") as ColumnDefinition;
        _optionsButton = GetTemplateChild("optionsButton") as Button;
        _selectAllCheckBox = GetTemplateChild("selectAllCheckBox") as CheckBox;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;
        _headersStackPanel = GetTemplateChild("HeadersStackPanel") as StackPanel;

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

#if !WINDOWS
        if (GetTemplateChild("cornerButtonColumn") is ColumnDefinition cornerButtonColumn)
        {
            cornerButtonColumn.MinWidth = 20;
        }
#endif

        SetExportOptionsVisibility();
        SetCornerButtonState();
        EnsureGridLines();
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        if (_headersStackPanel is not null && _v_gridLine is not null && TableView is not null)
        {
            var xGridLine = _v_gridLine.Visibility is Visibility.Visible ? _v_gridLine.ActualOffset.X + _v_gridLine.ActualWidth : 0;
            var headersOffset = -TableView.HorizontalOffset + xGridLine;
            var xClip = (headersOffset * -1) + xGridLine;

            _headersStackPanel.Arrange(new Rect(headersOffset, 0, _headersStackPanel.ActualWidth, finalSize.Height));
            _headersStackPanel.Clip = headersOffset >= xGridLine ? null :
                new RectangleGeometry
                {
                    Rect = new Rect(xClip, 0, _headersStackPanel.ActualWidth - xClip, finalSize.Height)
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
        else if (e.Action == NotifyCollectionChangedAction.Reset && _headersStackPanel is not null)
        {
            _headersStackPanel.Children.Clear();
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
        else if (e.PropertyName is nameof(TableViewColumn.Order) && e.Column.Visibility is Visibility.Visible)
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
        if (_headersStackPanel is not null)
        {
            foreach (var column in columns)
            {
                var header = _headersStackPanel.Children.OfType<TableViewColumnHeader>().FirstOrDefault(x => x.Column == column);
                if (header is not null)
                {
                    _headersStackPanel.Children.Remove(header);
                }
            }
        }
    }

    /// <summary>
    /// Adds headers for the specified columns.
    /// </summary>
    private void AddHeaders(IEnumerable<TableViewColumn> columns)
    {
        if (TableView is not null && _headersStackPanel is not null)
        {
            foreach (var column in columns)
            {
                var header = new TableViewColumnHeader { DataContext = column, Column = column };
                column.HeaderControl = header;

                var index = TableView.Columns.VisibleColumns.IndexOf(column);
                index = Math.Min(index, _headersStackPanel.Children.Count);
                index = Math.Max(index, 0); // handles -ve index;
                _headersStackPanel.Children.Insert(index, header);

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
                            _headersStackPanel?.ActualHeight ?? ActualHeight)));
                }
            }

            _calculatingHeaderWidths = false;
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
        else if (TableView?.HeadersVisibility is TableViewHeadersVisibility.None or TableViewHeadersVisibility.Columns)
        {
            stateName = VisualStates.StateNoButton;
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
                _v_gridLine.Fill = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                   ? TableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
                _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
                _v_gridLine.Visibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                         || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                         ? Visibility.Visible : Visibility.Collapsed;
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
    internal TableViewColumnHeader? GetPreviousHeader(TableViewColumnHeader? currentHeader)
    {
        var previousCellIndex = currentHeader is null ? Headers.Count - 1 : Headers.IndexOf(currentHeader) - 1;
        return previousCellIndex >= 0 ? Headers[previousCellIndex] : default;
    }

    /// <summary>
    /// Sets the widths of the row header column.
    /// </summary>
    internal void SetRowHeaderWidth()
    {
        if (_cornerButtonColumn is not null && TableView is not null)
        {
            var headerWidth = TableView.RowHeaderWidth is double.NaN ? TableView.RowHeaderActualWidth : TableView.RowHeaderWidth;

            _cornerButtonColumn.Width = new(headerWidth);
            _cornerButtonColumn.MinWidth = TableView.RowHeaderMinWidth;
            _cornerButtonColumn.MaxWidth = TableView.RowHeaderMaxWidth;
        }
    }

    /// <summary>
    /// Sets the visibility of the row header based on the TableView settings.
    /// </summary>
    internal void SetHeadersVisibility()
    {
        SetCornerButtonState();

        if (_cornerButtonColumn is not null && _v_gridLine is not null && TableView is not null)
        {
            var areColumnHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Columns;
            var areRowHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView.SelectionMode is ListViewSelectionMode.Multiple;

            Visibility = areColumnHeadersVisible ? Visibility.Visible : Visibility.Collapsed;

            if (areRowHeadersVisible || isMultiSelection)
            {
                SetRowHeaderWidth();
                _v_gridLine.Visibility = Visibility.Visible;
            }
            else
            {
                _v_gridLine.Visibility = Visibility.Collapsed;
                _cornerButtonColumn.Width = new(0);
                _cornerButtonColumn.MinWidth = 0;
                _cornerButtonColumn.MaxWidth = 0;
            }
        }
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
    public IList<TableViewColumnHeader> Headers => [.. _headersStackPanel?.Children.OfType<TableViewColumnHeader>() ?? []];

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
