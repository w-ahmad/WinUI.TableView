using Microsoft.UI.Xaml;
using SD = WinUI.TableView.SortDirection;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView.
/// </summary>
[StyleTypedProperty(Property = nameof(HeaderStyle), StyleTargetType = typeof(TableViewColumnHeader))]
[StyleTypedProperty(Property = nameof(CellStyle), StyleTargetType = typeof(TableViewCell))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public abstract partial class TableViewColumn : DependencyObject
{
    private TableViewColumnHeader? _headerControl;
    private double _desiredWidth;
    private SD? _sortDirection;
    private bool _isFiltered;

    /// <summary>
    /// Generates a display element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A FrameworkElement representing the display element.</returns>
    public abstract FrameworkElement GenerateElement(TableViewCell cell, object? dataItem);

    /// <summary>
    /// Generates an editing element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A FrameworkElement representing the editing element.</returns>
    public abstract FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem);

    /// <summary>
    /// Refreshes the display element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is refreshed.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    public virtual void RefreshElement(TableViewCell cell, object? dataItem) { }

    /// <summary>
    /// Updates the state of the element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element state is updated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    public virtual void UpdateElementState(TableViewCell cell, object? dataItem) { }

    /// <summary>
    /// Sets the owning collection for the column.
    /// </summary>
    /// <param name="collection">The owning collection.</param>
    internal void SetOwningCollection(TableViewColumnsCollection collection)
    {
        OwningCollection = collection;
    }

    /// <summary>
    /// Sets the owning TableView for the column.
    /// </summary>
    /// <param name="tableView">The owning TableView.</param>
    internal void SetOwningTableView(TableView tableView)
    {
        TableView = tableView;
    }

    /// <summary>
    /// Gets the content of the cell for the specified data item.
    /// </summary>
    /// <param name="dataItem">The data item.</param>
    /// <returns>The content of the cell.</returns>
    public virtual object? GetCellContent(object? dataItem)
    {
        return default;
    }

    /// <summary>
    /// Gets or sets the header content of the column.
    /// </summary>
    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the column.
    /// </summary>
    public GridLength Width
    {
        get => (GridLength)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum width of the column.
    /// </summary>
    public double? MinWidth
    {
        get => (double?)GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum width of the column.
    /// </summary>
    public double? MaxWidth
    {
        get => (double?)GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the actual width of the column.
    /// </summary>
    public double ActualWidth
    {
        get => (double)GetValue(ActualWidthProperty);
        set => SetValue(ActualWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column can be resized.
    /// </summary>
    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Gets or sets the style that is used when rendering the column header.
    /// </summary>
    public Style? HeaderStyle
    {
        get => (Style?)GetValue(HeaderStyleProperty);
        set => SetValue(HeaderStyleProperty, value);
    }


    /// <summary>
    /// Gets or sets the style that is used to render cells in the column.
    /// </summary>
    public Style CellStyle
    {
        get => (Style)GetValue(CellStyleProperty);
        set => SetValue(CellStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the header control for the column.
    /// </summary>
    public TableViewColumnHeader? HeaderControl
    {
        get => _headerControl;
        internal set
        {
            _headerControl = value;
            EnsureHeaderStyle();
        }
    }

    /// <summary>
    /// Gets or sets the visibility of the column.
    /// </summary>
    public Visibility Visibility
    {
        get => (Visibility)GetValue(VisibilityProperty);
        set => SetValue(VisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets a custom tag object for the column.
    /// </summary>
    public object Tag
    {
        get => GetValue(TagProperty);
        set => SetValue(TagProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column can be sorted. This can be overridden by the TableView.
    /// </summary>
    public bool CanSort
    {
        get => (bool)GetValue(CanSortProperty);
        set => SetValue(CanSortProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column can be filtered.
    /// </summary>
    public bool CanFilter
    {
        get => (bool)GetValue(CanFilterProperty);
        set => SetValue(CanFilterProperty, value);
    }

    /// <summary>
    /// Gets or sets the order in which this column should be displayed.
    /// </summary>
    public int? Order
    {
        get => (int?)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }

    internal TableViewColumnsCollection? OwningCollection { get; set; }

    /// <summary>
    /// Gets or sets the desired width of the column.
    /// </summary>
    internal double DesiredWidth
    {
        get => _desiredWidth;
        set
        {
            if (_desiredWidth != value)
            {
                _desiredWidth = value;
                OwningCollection?.HandleColumnPropertyChanged(this, nameof(DesiredWidth));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column is auto-generated.
    /// </summary>
    public bool IsAutoGenerated { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column uses a single element for display and editing.
    /// </summary>
    public bool UseSingleElement { get; set; }

    /// <summary>
    /// Gets or sets the sort direction for the column.
    /// </summary>
    public SD? SortDirection
    {
        get => _sortDirection;
        set
        {
            _sortDirection = value;
            OnSortDirectionChanged();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column is filtered.
    /// </summary>
    public bool IsFiltered
    {
        get => _isFiltered;
        set
        {
            _isFiltered = value;
            OnIsFilteredChanged();
        }
    }

    /// <summary>
    /// Gets the owning TableView for the column.
    /// </summary>
    protected internal TableView? TableView { get; private set; }

    /// <summary>
    /// Handles changes to the SortDirection property.
    /// </summary>
    private void OnSortDirectionChanged()
    {
        HeaderControl?.OnSortDirectionChanged();
    }

    /// <summary>
    /// Handles changes to the IsFiltered property.
    /// </summary>
    private void OnIsFilteredChanged()
    {
        HeaderControl?.OnIsFilteredChanged();
    }

    /// <summary>
    /// Ensures the header style is applied to the header control.
    /// </summary>
    internal void EnsureHeaderStyle()
    {
        if (_headerControl is not null)
        {
            _headerControl.Style = HeaderStyle ?? TableView?.ColumnHeaderStyle;
        }
    }

    /// <summary>
    /// Handles changes to the Width property.
    /// </summary>
    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn { OwningCollection: { } } column)
        {
            if (e.Property == CellStyleProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(CellStyle));
            else if (e.Property == WidthProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(Width));
            else if (e.Property == MinWidthProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(MinWidth));
            else if (e.Property == MaxWidthProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(MaxWidth));
            else if (e.Property == ActualWidthProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(ActualWidth));
            else if (e.Property == IsReadOnlyProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(IsReadOnly));
            else if (e.Property == VisibilityProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(Visibility));
            else if (e.Property == OrderProperty)
                column.OwningCollection.HandleColumnPropertyChanged(column, nameof(Order));
        }
    }

    /// <summary>
    /// Handles changes to the CanFilter property.
    /// </summary>
    private static void OnCanFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column.HeaderControl is not null)
        {
            column.HeaderControl.SetFilterButtonVisibility();
        }
    }

    /// <summary>
    /// Handles changes to the HeaderStyle property.
    /// </summary>
    private static void OnHeaderStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column)
        {
            column.EnsureHeaderStyle();
        }
    }

    /// <summary>
    /// Identifies the HeaderStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderStyleProperty = DependencyProperty.Register(nameof(HeaderStyle), typeof(Style), typeof(TableViewColumn), new PropertyMetadata(null, OnHeaderStyleChanged));

    /// <summary>
    /// Identifies the CellStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty CellStyleProperty = DependencyProperty.Register(nameof(CellStyle), typeof(Style), typeof(TableViewColumn), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object), typeof(TableViewColumn), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the Width dependency property.
    /// </summary>
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(nameof(Width), typeof(GridLength), typeof(TableViewColumn), new PropertyMetadata(GridLength.Auto, OnPropertyChanged));

    /// <summary>
    /// Identifies the MinWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(nameof(MinWidth), typeof(double?), typeof(TableViewColumn), new PropertyMetadata(default, OnPropertyChanged));

    /// <summary>
    /// Identifies the MaxWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(nameof(MaxWidth), typeof(double?), typeof(TableViewColumn), new PropertyMetadata(default, OnPropertyChanged));

    /// <summary>
    /// Identifies the ActualWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty ActualWidthProperty = DependencyProperty.Register(nameof(ActualWidth), typeof(double), typeof(TableViewColumn), new PropertyMetadata(0d, OnPropertyChanged));

    /// <summary>
    /// Identifies the CanResize dependency property.
    /// </summary>
    public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(nameof(CanResize), typeof(bool), typeof(TableViewColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the IsReadOnly dependency property.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TableViewColumn), new PropertyMetadata(false, OnPropertyChanged));

    /// <summary>
    /// Identifies the Visibility dependency property.
    /// </summary>
    public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register(nameof(Visibility), typeof(Visibility), typeof(TableViewColumn), new PropertyMetadata(Visibility.Visible, OnPropertyChanged));

    /// <summary>
    /// Identifies the Tag dependency property.
    /// </summary>
    public static readonly DependencyProperty TagProperty = DependencyProperty.Register(nameof(Tag), typeof(object), typeof(TableViewColumn), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the CanSort dependency property.
    /// </summary>
    public static readonly DependencyProperty CanSortProperty = DependencyProperty.Register(nameof(CanSort), typeof(bool), typeof(TableViewColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the CanFilter dependency property.
    /// </summary>
    public static readonly DependencyProperty CanFilterProperty = DependencyProperty.Register(nameof(CanFilter), typeof(bool), typeof(TableViewColumn), new PropertyMetadata(true, OnCanFilterChanged));

    /// <summary>
    /// Identifies the Order dependency property.
    /// </summary>
    public static readonly DependencyProperty OrderProperty = DependencyProperty.Register(nameof(Order), typeof(int?), typeof(TableViewColumn), new PropertyMetadata(null, OnPropertyChanged));
}