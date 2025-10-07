using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    public static readonly DependencyProperty HeaderRowHeightProperty = DependencyProperty.Register(nameof(HeaderRowHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the HeaderRowMaxHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderRowMaxHeightProperty = DependencyProperty.Register(nameof(HeaderRowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));

    /// <summary>
    /// Identifies the HeaderRowMinHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderRowMinHeightProperty = DependencyProperty.Register(nameof(HeaderRowMinHeight), typeof(double), typeof(TableView), new PropertyMetadata(32d));

    /// <summary>
    /// Identifies the RowHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the RowMaxHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty RowMaxHeightProperty = DependencyProperty.Register(nameof(RowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));

    /// <summary>
    /// Identifies the RowMinHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty RowMinHeightProperty = DependencyProperty.Register(nameof(RowMinHeight), typeof(double), typeof(TableView), new PropertyMetadata(40d));

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
    public static readonly DependencyProperty CornerButtonModeProperty = DependencyProperty.Register(nameof(CornerButtonMode), typeof(TableViewCornerButtonMode), typeof(TableView), new PropertyMetadata(TableViewCornerButtonMode.Options, OnCornerButtonModeChanged));

    /// <summary>
    /// Identifies the CanResizeColumns dependency property.
    /// </summary>
    public static readonly DependencyProperty CanResizeColumnsProperty = DependencyProperty.Register(nameof(CanResizeColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the CanSortColumns dependency property.
    /// </summary>
    public static readonly DependencyProperty CanSortColumnsProperty = DependencyProperty.Register(nameof(CanSortColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true));

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
    /// Identifies the ColumnHeaderStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnHeaderStyleProperty = DependencyProperty.Register(nameof(ColumnHeaderStyle), typeof(Style), typeof(TableView), new PropertyMetadata(null, OnColumnHeaderStyleChanged));

    /// <summary>
    /// Identifies the CellStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty CellStyleProperty = DependencyProperty.Register(nameof(CellStyle), typeof(Style), typeof(TableView), new PropertyMetadata(null, OnCellStyleChanged));

    /// <summary>
    /// Identifies the CurrentCellSlot dependency property.
    /// </summary>
    public static readonly DependencyProperty CurrentCellSlotProperty = DependencyProperty.Register(nameof(CurrentCellSlot), typeof(TableViewCellSlot?), typeof(TableView), new PropertyMetadata(default, OnCurrentCellSlotChanged));

    /// <summary>
    /// Identifies the UseRightClickForColumnFilter dependency property.
    /// </summary>
    public static readonly DependencyProperty UseRightClickForColumnFilterProperty = DependencyProperty.Register(nameof(UseRightClickForColumnFilter), typeof(bool), typeof(TableView), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the VerticalOffset dependency property.
    /// </summary>
    public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(TableView), new PropertyMetadata(0d));

    /// <summary>
    /// Identifies the HorizontalOffset dependency property.
    /// </summary>
    public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(TableView), new PropertyMetadata(0.0, OnHorizontalOffsetChanged));

    /// <summary>
    /// Identifies the RowHeaderActualWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeaderActualWidthProperty = DependencyProperty.Register(nameof(RowHeaderActualWidth), typeof(double), typeof(TableView), new PropertyMetadata(0d, OnRowHeaderWidthChanged));

    /// <summary>
    /// Identifies the RowHeaderWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeaderWidthProperty = DependencyProperty.Register(nameof(RowHeaderWidth), typeof(double), typeof(TableView), new PropertyMetadata(double.NaN, OnRowHeaderWidthChanged));

    /// <summary>
    /// Identifies the RowHeaderMinWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeaderMinWidthProperty = DependencyProperty.Register(nameof(RowHeaderMinWidth), typeof(double), typeof(TableView), new PropertyMetadata(16d, OnRowHeaderWidthChanged));

    /// <summary>
    /// Identifies the RowHeaderMaxWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeaderMaxWidthProperty = DependencyProperty.Register(nameof(RowHeaderMaxWidth), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity, OnRowHeaderWidthChanged));

    /// <summary>
    /// Identifies the HeadersVisibility dependency property.
    /// </summary>
    public static readonly DependencyProperty HeadersVisibilityProperty = DependencyProperty.Register(nameof(HeadersVisibility), typeof(TableViewHeadersVisibility), typeof(TableView), new PropertyMetadata(TableViewHeadersVisibility.All, OnRowHeadersVisibilityChanged));

    /// <summary>
    /// Identifies the RowHeaderContent dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeaderTemplateProperty = DependencyProperty.Register(nameof(RowHeaderTemplate), typeof(DataTemplate), typeof(TableView), new PropertyMetadata(null, OnRowHeaderTemplateChanged));

    /// <summary>
    /// Identifies the RowHeaderTemplateSelector dependency property.
    /// </summary>
    public static readonly DependencyProperty RowHeaderTemplateSelectorProperty = DependencyProperty.Register(nameof(RowHeaderTemplateSelector), typeof(DataTemplateSelector), typeof(TableView), new PropertyMetadata(null, OnRowHeaderTemplateChanged));

    /// <summary>
    /// Identifies the FrozenColumnCount dependency property.
    /// </summary>
    public static readonly DependencyProperty FrozenColumnCountProperty = DependencyProperty.Register(nameof(FrozenColumnCount), typeof(int), typeof(TableView), new PropertyMetadata(0, OnFrozenColumnCountChanged));

    /// <summary>
    /// Gets or sets a value indicating whether opening the column filter over header right-click is enabled.
    /// </summary>
    public bool UseRightClickForColumnFilter
    {
        get => (bool)GetValue(UseRightClickForColumnFilterProperty);
        set => SetValue(UseRightClickForColumnFilterProperty, value);
    }

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
    /// Gets or sets the current cell slot associated with the table view.
    /// </summary>
    public TableViewCellSlot? CurrentCellSlot
    {
        get => (TableViewCellSlot?)GetValue(CurrentCellSlotProperty);
        set => SetValue(CurrentCellSlotProperty, value);
    }

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
    internal HashSet<TableViewCellSlot> SelectedCells { get; set; } = [];

    /// <summary>
    /// Gets the selected cell ranges.
    /// </summary>
    internal HashSet<HashSet<TableViewCellSlot>> SelectedCellRanges { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the TableView is in editing mode.
    /// </summary>
    internal bool IsEditing { get; private set; }

    /// <summary>
    /// Gets or sets the filter handler for the TableView.
    /// </summary>
    public IColumnFilterHandler FilterHandler { get; set; }

    /// <summary>
    /// Gets a value indicating whether the TableView items are filtered.
    /// </summary>
    public bool IsFiltered => FilterDescriptions.Count > 0 || Columns.Any(x => x.IsFiltered) is true;

    /// <summary>
    /// Gets a value indicating whether the TableView items are sorted.
    /// </summary>
    public bool IsSorted => SortDescriptions.Count > 0 || Columns.Any(x => x.SortDirection is not null) is true;

    /// <summary>
    /// Gets the collection of columns in the TableView.
    /// </summary>
    public ITableViewColumnsCollection Columns { get; }

    /// <summary>
    /// Gets or sets the height of the header row.
    /// </summary>
    public double HeaderRowHeight
    {
        get => (double)GetValue(HeaderRowHeightProperty);
        set => SetValue(HeaderRowHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the max height of the header row.
    /// </summary>
    public double HeaderRowMaxHeight
    {
        get => (double)GetValue(HeaderRowMaxHeightProperty);
        set => SetValue(HeaderRowMaxHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the min height of the header row.
    /// </summary>
    public double HeaderRowMinHeight
    {
        get => (double)GetValue(HeaderRowMinHeightProperty);
        set => SetValue(HeaderRowMinHeightProperty, value);
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
    /// Gets or sets the minimum height of the rows.
    /// </summary>
    public double RowMinHeight
    {
        get => (double)GetValue(RowMinHeightProperty);
        set => SetValue(RowMinHeightProperty, value);
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
    /// Gets or sets the style applied to all column headers.
    /// </summary>
    public Style ColumnHeaderStyle
    {
        get => (Style)GetValue(ColumnHeaderStyleProperty);
        set => SetValue(ColumnHeaderStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style applied to all cells.
    /// </summary>
    public Style CellStyle
    {
        get => (Style)GetValue(CellStyleProperty);
        set => SetValue(CellStyleProperty, value);
    }

    /// <summary>
    /// Gets the vertical offset for the TableView.
    /// </summary>
    public double VerticalOffset => (double)GetValue(VerticalOffsetProperty);

    /// <summary>
    /// Gets the horizontal offset for the TableView.
    /// </summary>
    public double HorizontalOffset => (double)GetValue(HorizontalOffsetProperty);

    /// <summary>
    /// Gets the actual width of the row header.
    /// </summary>
    public double RowHeaderActualWidth => (double)GetValue(RowHeaderActualWidthProperty);

    /// <summary>
    /// Gets or sets the width of the row header.
    /// </summary>
    public double RowHeaderWidth
    {
        get => (double)GetValue(RowHeaderWidthProperty);
        set => SetValue(RowHeaderWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum width of the row header.
    /// </summary>
    public double RowHeaderMinWidth
    {
        get => (double)GetValue(RowHeaderMinWidthProperty);
        set => SetValue(RowHeaderMinWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum width of the row header.
    /// </summary>
    public double RowHeaderMaxWidth
    {
        get => (double)GetValue(RowHeaderMaxWidthProperty);
        set => SetValue(RowHeaderMaxWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the visibility of the row and column headers.
    /// </summary>
    public TableViewHeadersVisibility HeadersVisibility
    {
        get => (TableViewHeadersVisibility)GetValue(HeadersVisibilityProperty);
        set => SetValue(HeadersVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template for the row header.
    /// </summary>
    public DataTemplate? RowHeaderTemplate
    {
        get => (DataTemplate?)GetValue(RowHeaderTemplateProperty);
        set => SetValue(RowHeaderTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template selector for the row header.
    /// </summary>
    public DataTemplateSelector RowHeaderTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(RowHeaderTemplateSelectorProperty);
        set => SetValue(RowHeaderTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of columns that stays in view on horizontal scroll.
    /// </summary>
    public int FrozenColumnCount
    {
        get => (int)GetValue(FrozenColumnCountProperty);
        set => SetValue(FrozenColumnCountProperty, value);
    }

    /// <summary>
    /// Handles changes to the ItemsSource property.
    /// </summary>
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.ItemsSourceChanged(e);
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
                    tableView.SelectedCellRanges.Add([tableView.CurrentCellSlot.Value]);
                }

                tableView.OnCellSelectionChanged();
            }

            tableView.UpdateBaseSelectionMode();
            tableView.UpdateCornerButtonState();
        }
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
                tableView.EnsureAutoColumns(true);
            }
            else
            {
                tableView.RemoveAutoGeneratedColumns();
            }
        }
    }

    /// <summary>
    /// Handles changes to the CornerButtonMode property.
    /// </summary>
    private static void OnCornerButtonModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.UpdateCornerButtonState();
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
                tableView.CurrentCellSlot = null;
            }
        }
    }

    /// <summary>
    /// Handles changes to the CanFilterColumns property.
    /// </summary>
    private static void OnCanFilterColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView && tableView._headerRow is not null)
        {
            foreach (var header in tableView._headerRow.Headers)
            {
                header.SetFilterButtonVisibility();
            }
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
            tableView.UpdateCornerButtonState();
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
    /// Handles changes to the ColumnHeaderStyle property.
    /// </summary>
    private static void OnColumnHeaderStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.EnsureColumnHeadersStyle();
        }
    }

    /// <summary>
    /// Handles changes to the CellStyle property.
    /// </summary>
    private static void OnCellStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.EnsureCellsStyle();
        }
    }

    private static async void OnCurrentCellSlotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TableView tableView) return;

        tableView.CurrentCellChanged?.Invoke(d, e);

        var oldSlot = e.OldValue as TableViewCellSlot?;
        var newSlot = e.NewValue as TableViewCellSlot?;

        await tableView.OnCurrentCellChanged(oldSlot, newSlot);
    }

    /// <summary>
    /// Handles changes to the HorizontalOffset property.
    /// </summary>
    private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView { _headerRow: { } } tableView)
        {
            tableView._headerRow.InvalidateArrange();

            foreach (var row in tableView._rows)
            {
                row?.RowPresenter?.InvalidateArrange();
            }
        }
    }

    /// <summary>
    /// Handles changes to the RowHeaderActualWidth, RowHeaderWidth, RowHeaderMinWidth, RowHeaderMaxWidth properties.
    /// </summary>
    private static async void OnRowHeaderWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            await Task.Yield();

            tableView._headerRow?.SetRowHeaderWidth();

            foreach (var row in tableView._rows)
            {
                row.RowPresenter?.SetRowHeaderWidth();
            }
        }
    }

    /// <summary>
    /// Handles changes to the HeadersVisibility property.
    /// </summary>
    private static void OnRowHeadersVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.SetHeadersVisibility();
            tableView.UpdateHorizontalScrollBarMargin();
        }
    }

    /// <summary>
    /// Handles changes to the RowHeaderTemplate and RowHeaderTemplateSelector properties.
    /// </summary>
    private static void OnRowHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.SetValue(RowHeaderActualWidthProperty, 0d);

            foreach (var row in tableView._rows)
            {
                row.RowPresenter?.SetRowHeaderTemplate();
            }
        }
    }

    /// <summary>
    /// Handles changes to the FrozenColumnCount property.
    /// </summary>
    private static void OnFrozenColumnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.SetValue(HorizontalOffsetProperty, 0d);
            tableView.UpdateHorizontalScrollBarMargin();

            if (tableView.Columns is TableViewColumnsCollection columnsCollection)
            {
                columnsCollection.UpdateFrozenColumns();
            }
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
