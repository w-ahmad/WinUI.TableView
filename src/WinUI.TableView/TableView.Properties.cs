using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Partial class for TableView that contains dependency properties and related methods.
/// </summary>
public partial class TableView
{
    /// <summary>
    /// Identifies the ItemsSource dependency property.
    /// </summary>
    public static readonly new DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(TableView), new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>
    /// Identifies the SelectionMode dependency property.
    /// </summary>
    public static readonly new DependencyProperty SelectionModeProperty = DependencyProperty.Register(nameof(SelectionMode), typeof(ListViewSelectionMode), typeof(TableView), new PropertyMetadata(ListViewSelectionMode.Extended, OnSelectionModeChanged));

    /// <summary>
    /// Identifies the HeaderRowHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderRowHeightProperty = DependencyProperty.Register(nameof(HeaderRowHeight), typeof(double), typeof(TableView), new PropertyMetadata(32d, OnHeaderRowHeightChanged));

    /// <summary>
    /// Identifies the RowHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(TableView), new PropertyMetadata(40d));

    /// <summary>
    /// Identifies the RowMaxHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty RowMaxHeightProperty = DependencyProperty.Register(nameof(RowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));

    /// <summary>
    /// Identifies the ShowExportOptions dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowExportOptionsProperty = DependencyProperty.Register(nameof(ShowExportOptions), typeof(bool), typeof(TableView), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the AutoGenerateColumns dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoGenerateColumnsProperty = DependencyProperty.Register(nameof(AutoGenerateColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true, OnAutoGenerateColumnsChanged));

    /// <summary>
    /// Identifies the IsReadOnly dependency property.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TableView), new PropertyMetadata(false, OnIsReadOnlyChanged));

    /// <summary>
    /// Identifies the CornerButtonMode dependency property.
    /// </summary>
    public static readonly DependencyProperty CornerButtonModeProperty = DependencyProperty.Register(nameof(CornerButtonMode), typeof(TableViewCornerButtonMode), typeof(TableView), new PropertyMetadata(TableViewCornerButtonMode.Options));

    /// <summary>
    /// Identifies the CanResizeColumns dependency property.
    /// </summary>
    public static readonly DependencyProperty CanResizeColumnsProperty = DependencyProperty.Register(nameof(CanResizeColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the CanSortColumns dependency property.
    /// </summary>
    public static readonly DependencyProperty CanSortColumnsProperty = DependencyProperty.Register(nameof(CanSortColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true, OnCanSortColumnsChanged));

    /// <summary>
    /// Identifies the CanFilterColumns dependency property.
    /// </summary>
    public static readonly DependencyProperty CanFilterColumnsProperty = DependencyProperty.Register(nameof(CanFilterColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true, OnCanFilterColumnsChanged));

    /// <summary>
    /// Identifies the MinColumnWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(nameof(MinColumnWidth), typeof(double), typeof(TableView), new PropertyMetadata(50d, OnMinColumnWidthChanged));

    /// <summary>
    /// Identifies the MaxColumnWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxColumnWidthProperty = DependencyProperty.Register(nameof(MaxColumnWidth), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity, OnMaxColumnWidthChanged));

    /// <summary>
    /// Identifies the SelectionUnit dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectionUnitProperty = DependencyProperty.Register(nameof(SelectionUnit), typeof(TableViewSelectionUnit), typeof(TableView), new PropertyMetadata(TableViewSelectionUnit.CellOrRow, OnSelectionUnitChanged));

    /// <summary>
    /// Identifies the HeaderGridLinesVisibility dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderGridLinesVisibilityProperty = DependencyProperty.Register(nameof(HeaderGridLinesVisibility), typeof(TableViewGridLinesVisibility), typeof(TableView), new PropertyMetadata(TableViewGridLinesVisibility.All, OnGridLinesPropertyChanged));

    /// <summary>
    /// Identifies the GridLinesVisibility dependency property.
    /// </summary>
    public static readonly DependencyProperty GridLinesVisibilityProperty = DependencyProperty.Register(nameof(GridLinesVisibility), typeof(TableViewGridLinesVisibility), typeof(TableView), new PropertyMetadata(TableViewGridLinesVisibility.All, OnGridLinesPropertyChanged));

    /// <summary>
    /// Identifies the HorizontalGridLinesStrokeThickness dependency property.
    /// </summary>
    public static readonly DependencyProperty HorizontalGridLinesStrokeThicknessProperty = DependencyProperty.Register(nameof(HorizontalGridLinesStrokeThickness), typeof(double), typeof(TableView), new PropertyMetadata(1d, OnGridLinesPropertyChanged));

    /// <summary>
    /// Identifies the VerticalGridLinesStrokeThickness dependency property.
    /// </summary>
    public static readonly DependencyProperty VerticalGridLinesStrokeThicknessProperty = DependencyProperty.Register(nameof(VerticalGridLinesStrokeThickness), typeof(double), typeof(TableView), new PropertyMetadata(1d, OnGridLinesPropertyChanged));

    /// <summary>
    /// Identifies the HorizontalGridLinesStroke dependency property.
    /// </summary>
    public static readonly DependencyProperty HorizontalGridLinesStrokeProperty = DependencyProperty.Register(nameof(HorizontalGridLinesStroke), typeof(Brush), typeof(TableView), new PropertyMetadata(default, OnGridLinesPropertyChanged));

    /// <summary>
    /// Identifies the VerticalGridLinesStroke dependency property.
    /// </summary>
    public static readonly DependencyProperty VerticalGridLinesStrokeProperty = DependencyProperty.Register(nameof(VerticalGridLinesStroke), typeof(Brush), typeof(TableView), new PropertyMetadata(default, OnGridLinesPropertyChanged));

    /// <summary>
    /// Identifies the AlternateRowForeground dependency property.
    /// </summary>
    public static readonly DependencyProperty AlternateRowForegroundProperty = DependencyProperty.Register(nameof(AlternateRowForeground), typeof(Brush), typeof(TableView), new PropertyMetadata(null, OnAlternateRowColorChanged));

    /// <summary>
    /// Identifies the AlternateRowBackground dependency property.
    /// </summary>
    public static readonly DependencyProperty AlternateRowBackgroundProperty = DependencyProperty.Register(nameof(AlternateRowBackground), typeof(Brush), typeof(TableView), new PropertyMetadata(null, OnAlternateRowColorChanged));

    /// <summary>
    /// Identifies the RowContextFlyout dependency property.
    /// </summary>
    public static readonly DependencyProperty RowContextFlyoutProperty = DependencyProperty.Register(nameof(RowContextFlyout), typeof(FlyoutBase), typeof(TableView), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the CellContextFlyout dependency property.
    /// </summary>
    public static readonly DependencyProperty CellContextFlyoutProperty = DependencyProperty.Register(nameof(CellContextFlyout), typeof(FlyoutBase), typeof(TableView), new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection view associated with the TableView.
    /// </summary>
    public ICollectionView CollectionView => _collectionView;

    /// <summary>
    /// Gets the collection of sort descriptions applied to the items.
    /// </summary>
    public IList<SortDescription> SortDescriptions => _collectionView.SortDescriptions;

    /// <summary>
    /// Gets the collection of filter descriptions applied to the items.
    /// </summary>
    public IList<FilterDescription> FilterDescriptions => _collectionView.FilterDescriptions;

    /// <summary>
    /// Gets or sets the last selection unit used.
    /// </summary>
    internal TableViewSelectionUnit LastSelectionUnit { get; set; }

    /// <summary>
    /// Gets or sets the current cell slot.
    /// </summary>
    internal TableViewCellSlot? CurrentCellSlot { get; set; }

    /// <summary>
    /// Gets or sets the selection start cell slot.
    /// </summary>
    internal TableViewCellSlot? SelectionStartCellSlot { get; set; }

    /// <summary>
    /// Gets or sets the selection start row index.
    /// </summary>
    internal int? SelectionStartRowIndex { get; set; }

    /// <summary>
    /// Gets or sets the current row index.
    /// </summary>
    internal int? CurrentRowIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected cells.
    /// </summary>
    internal HashSet<TableViewCellSlot> SelectedCells { get; set; } = new HashSet<TableViewCellSlot>();

    /// <summary>
    /// Gets the selected cell ranges.
    /// </summary>
    internal HashSet<HashSet<TableViewCellSlot>> SelectedCellRanges { get; } = new HashSet<HashSet<TableViewCellSlot>>();

    /// <summary>
    /// Gets or sets a value indicating whether the TableView is in editing mode.
    /// </summary>
    internal bool IsEditing { get; set; }

    /// <summary>
    /// Gets the width of the selection indicator.
    /// </summary>
    internal int SelectionIndicatorWidth => SelectionMode is ListViewSelectionMode.Multiple ? 44 : 16;

    /// <summary>
    /// Gets the collection of columns in the TableView.
    /// </summary>
    public TableViewColumnsCollection Columns { get; } = new();

    /// <summary>
    /// Gets or sets the height of the header row.
    /// </summary>
    public double HeaderRowHeight
    {
        get => (double)GetValue(HeaderRowHeightProperty);
        set => SetValue(HeaderRowHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the rows.
    /// </summary>
    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum height of the rows.
    /// </summary>
    public double RowMaxHeight
    {
        get => (double)GetValue(RowMaxHeightProperty);
        set => SetValue(RowMaxHeightProperty, value);
    }

    /// <summary>
    ///  Gets or sets an object source used to generate the content of the TableView.
    /// </summary>
    public new IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection mode for the TableView.
    /// </summary>
    public new ListViewSelectionMode SelectionMode
    {
        get => (ListViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show export options.
    /// </summary>
    public bool ShowExportOptions
    {
        get => (bool)GetValue(ShowExportOptionsProperty);
        set => SetValue(ShowExportOptionsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-generate columns.
    /// </summary>
    public bool AutoGenerateColumns
    {
        get => (bool)GetValue(AutoGenerateColumnsProperty);
        set => SetValue(AutoGenerateColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the TableView is read-only. This will override what is set on individual column.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Gets or sets the mode of the corner button.
    /// </summary>
    public TableViewCornerButtonMode CornerButtonMode
    {
        get => (TableViewCornerButtonMode)GetValue(CornerButtonModeProperty);
        set => SetValue(CornerButtonModeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether columns can be resized. This will override what is set on individual column.
    /// </summary>
    public bool CanResizeColumns
    {
        get => (bool)GetValue(CanResizeColumnsProperty);
        set => SetValue(CanResizeColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether columns can be sorted. This will overridden by individual column.
    /// </summary>
    public bool CanSortColumns
    {
        get => (bool)GetValue(CanSortColumnsProperty);
        set => SetValue(CanSortColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether columns can be filtered. This will overridden by the individual column.
    /// </summary>
    public bool CanFilterColumns
    {
        get => (bool)GetValue(CanFilterColumnsProperty);
        set => SetValue(CanFilterColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum width of columns. This can be overridden by the individual column.
    /// </summary>
    public double MinColumnWidth
    {
        get => (double)GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum width of columns. This can be override by setting MaxWidth on individual column.
    /// </summary>
    public double MaxColumnWidth
    {
        get => (double)GetValue(MaxColumnWidthProperty);
        set => SetValue(MaxColumnWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the selection unit for the TableView.
    /// </summary>
    public TableViewSelectionUnit SelectionUnit
    {
        get => (TableViewSelectionUnit)GetValue(SelectionUnitProperty);
        set => SetValue(SelectionUnitProperty, value);
    }

    /// <summary>
    /// Gets or sets the visibility of grid lines in the header row.
    /// </summary>
    public TableViewGridLinesVisibility HeaderGridLinesVisibility
    {
        get => (TableViewGridLinesVisibility)GetValue(HeaderGridLinesVisibilityProperty);
        set => SetValue(HeaderGridLinesVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets the visibility of grid lines in the body rows.
    /// </summary>
    public TableViewGridLinesVisibility GridLinesVisibility
    {
        get => (TableViewGridLinesVisibility)GetValue(GridLinesVisibilityProperty);
        set => SetValue(GridLinesVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets the thickness of vertical grid lines.
    /// </summary>
    public double VerticalGridLinesStrokeThickness
    {
        get => (double)GetValue(VerticalGridLinesStrokeThicknessProperty);
        set => SetValue(VerticalGridLinesStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the thickness of horizontal grid lines.
    /// </summary>
    public double HorizontalGridLinesStrokeThickness
    {
        get => (double)GetValue(HorizontalGridLinesStrokeThicknessProperty);
        set => SetValue(HorizontalGridLinesStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to draw vertical grid lines.
    /// </summary>
    public Brush VerticalGridLinesStroke
    {
        get => (Brush)GetValue(VerticalGridLinesStrokeProperty);
        set => SetValue(VerticalGridLinesStrokeProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to draw horizontal grid lines.
    /// </summary>
    public Brush HorizontalGridLinesStroke
    {
        get => (Brush)GetValue(HorizontalGridLinesStrokeProperty);
        set => SetValue(HorizontalGridLinesStrokeProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for alternate rows.
    /// </summary>
    public Brush AlternateRowBackground
    {
        get => (Brush)GetValue(AlternateRowBackgroundProperty);
        set => SetValue(AlternateRowBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush for alternate rows.
    /// </summary>
    public Brush AlternateRowForeground
    {
        get => (Brush)GetValue(AlternateRowForegroundProperty);
        set => SetValue(AlternateRowForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the context flyout for rows.
    /// </summary>
    public FlyoutBase? RowContextFlyout
    {
        get => (FlyoutBase?)GetValue(RowContextFlyoutProperty);
        set => SetValue(RowContextFlyoutProperty, value);
    }

    /// <summary>
    /// Gets or sets the context flyout for cells.
    /// </summary>
    public FlyoutBase? CellContextFlyout
    {
        get => (FlyoutBase?)GetValue(CellContextFlyoutProperty);
        set => SetValue(CellContextFlyoutProperty, value);
    }

    /// <summary>
    /// Handles changes to the ItemsSource property.
    /// </summary>
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.OnItemsSourceChanged(e);
            tableView.SelectedCellRanges.Clear();
            tableView.OnCellSelectionChanged();
        }
    }

    /// <summary>
    /// Handles changes to the SelectionMode property.
    /// </summary>
    private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            if (tableView.SelectionMode is ListViewSelectionMode.Single or ListViewSelectionMode.None)
            {
                var currentCell = tableView.CurrentCellSlot.HasValue ? tableView.GetCellFromSlot(tableView.CurrentCellSlot.Value) : default;
                currentCell?.ApplyCurrentCellState();
                tableView.SelectedCellRanges.Clear();

                if (tableView.SelectionMode is ListViewSelectionMode.Single && tableView.CurrentCellSlot.HasValue)
                {
                    tableView.SelectedCellRanges.Add(new() { tableView.CurrentCellSlot.Value });
                }

                tableView.OnCellSelectionChanged();
            }

            tableView.UpdateBaseSelectionMode();
        }
    }

    /// <summary>
    /// Handles changes to the HeaderRowHeight property.
    /// </summary>
    private static void OnHeaderRowHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as TableView)?.UpdateVerticalScrollBarMargin();
    }

    /// <summary>
    /// Handles changes to the AutoGenerateColumns property.
    /// </summary>
    private static void OnAutoGenerateColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            if (tableView.AutoGenerateColumns)
            {
                tableView.GenerateColumns();
            }
            else
            {
                tableView.RemoveAutoGeneratedColumns();
            }
        }
    }

    /// <summary>
    /// Handles changes to the IsReadOnly property.
    /// </summary>
    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.IsReadOnlyChanged?.Invoke(d, e);

            if ((tableView.SelectionMode is ListViewSelectionMode.None
                || tableView.SelectionUnit is TableViewSelectionUnit.Row)
                && tableView.IsReadOnly)
            {
                tableView.SetCurrentCell(null);
            }
        }
    }

    /// <summary>
    /// Handles changes to the CanSortColumns property.
    /// </summary>
    private static void OnCanSortColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView && e.NewValue is false)
        {
            tableView.ClearSorting();
        }
    }

    /// <summary>
    /// Handles changes to the CanFilterColumns property.
    /// </summary>
    private static void OnCanFilterColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView && e.NewValue is false)
        {
            tableView.ClearFilters();
        }
    }

    /// <summary>
    /// Handles changes to the MinColumnWidth property.
    /// </summary>
    private static void OnMinColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView table && table._headerRow is not null)
        {
            table._headerRow.CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Handles changes to the MaxColumnWidth property.
    /// </summary>
    private static void OnMaxColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView table && table._headerRow is not null)
        {
            table._headerRow.CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Handles changes to the SelectionUnit property.
    /// </summary>
    private static void OnSelectionUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            if (tableView.SelectionUnit is TableViewSelectionUnit.Row)
            {
                tableView.SelectedCellRanges.Clear();
                tableView.OnCellSelectionChanged();
            }

            tableView.UpdateBaseSelectionMode();
        }
    }

    /// <summary>
    /// Handles changes to the grid lines properties.
    /// </summary>
    private static void OnGridLinesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.EnsureGridLines();
        }
    }

    /// <summary>
    /// Handles changes to the alternate row color properties.
    /// </summary>
    private static void OnAlternateRowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.EnsureAlternateRowColors();
        }
    }

    /// <summary>
    /// Throws an exception if the base ItemsSource property is set directly.
    /// </summary>
    private void OnBaseItemsSourceChanged(DependencyObject sender, DependencyProperty dp)
    {
        throw new InvalidOperationException("Setting this property directly is not allowed. Use TableView.ItemsSource instead.");
    }

    /// <summary>
    /// Throws an exception if the base SelectionMode property is set directly.
    /// </summary>
    private void OnBaseSelectionModeChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (!_shouldThrowSelectionModeChangedException)
        {
            throw new InvalidOperationException("Setting this property directly is not allowed. Use TableView.SelectionMode instead.");
        }
    }
}
