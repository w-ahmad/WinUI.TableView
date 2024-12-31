using Microsoft.UI.Xaml;

namespace WinUI.TableView;

/// Represents a column in a TableView.
/// </summary>
[StyleTypedProperty(Property = nameof(HeaderStyle), StyleTargetType = typeof(TableViewColumnHeader))]
public abstract class TableViewColumn : DependencyObject
{
    protected TableView? TableView { get; private set; }
    private TableViewColumnsCollection? _owningCollection;
    private TableViewColumnHeader? _headerControl;
    private double _desiredWidth;

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
    /// <param name="tableViewCell">The cell for which the element is refreshed.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    public virtual void RefreshElement(TableViewCell tableViewCell, object? dataItem) { }

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
        _owningCollection = collection;
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
    /// Gets or sets the style for the column header.
    /// </summary>
    public Style? HeaderStyle
    {
        get => (Style?)GetValue(HeaderStyleProperty);
        set => SetValue(HeaderStyleProperty, value);
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
                _owningCollection?.HandleColumnPropertyChanged(this, nameof(DesiredWidth));
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
    /// Ensures the header style is applied to the header control.
    /// </summary>
    private void EnsureHeaderStyle()
    {
        if (_headerControl is not null && HeaderStyle is not null)
        {
            _headerControl.Style = HeaderStyle;
        }
    }

    /// <summary>
    /// Handles changes to the Width property.
    /// </summary>
    private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column._owningCollection is { })
        {
            column._owningCollection.HandleColumnPropertyChanged(column, nameof(Width));
        }
    }

    /// <summary>
    /// Handles changes to the MinWidth property.
    /// </summary>
    private static void OnMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column._owningCollection is { })
        {
            column._owningCollection.HandleColumnPropertyChanged(column, nameof(MinWidth));
        }
    }

    /// <summary>
    /// Handles changes to the MaxWidth property.
    /// </summary>
    private static void OnMaxWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column._owningCollection is { })
        {
            column._owningCollection.HandleColumnPropertyChanged(column, nameof(MaxWidth));
        }
    }

    /// <summary>
    /// Handles changes to the ActualWidth property.
    /// </summary>
    private static void OnActualWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column._owningCollection is { })
        {
            column._owningCollection.HandleColumnPropertyChanged(column, nameof(ActualWidth));
        }
    }

    /// <summary>
    /// Handles changes to the IsReadOnly property.
    /// </summary>
    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column._owningCollection is { })
        {
            column._owningCollection.HandleColumnPropertyChanged(column, nameof(IsReadOnly));
        }
    }

    /// <summary>
    /// Handles changes to the Visibility property.
    /// </summary>
    private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column._owningCollection is { })
        {
            column._owningCollection.HandleColumnPropertyChanged(column, nameof(Visibility));
        }
    }

    public static readonly DependencyProperty HeaderStyleProperty = DependencyProperty.Register(nameof(HeaderStyle), typeof(Style), typeof(TableViewColumn), new PropertyMetadata(null, (d, _) => ((TableViewColumn)d).EnsureHeaderStyle()));
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object), typeof(TableViewColumn), new PropertyMetadata(null));
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(nameof(Width), typeof(GridLength), typeof(TableViewColumn), new PropertyMetadata(GridLength.Auto, OnWidthChanged));
    public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(nameof(MinWidth), typeof(double?), typeof(TableViewColumn), new PropertyMetadata(default, OnMinWidthChanged));
    public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(nameof(MaxWidth), typeof(double?), typeof(TableViewColumn), new PropertyMetadata(default, OnMaxWidthChanged));
    public static readonly DependencyProperty ActualWidthProperty = DependencyProperty.Register(nameof(ActualWidth), typeof(double), typeof(TableViewColumn), new PropertyMetadata(0d, OnActualWidthChanged));
    public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(nameof(CanResize), typeof(bool), typeof(TableViewColumn), new PropertyMetadata(true));
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TableViewColumn), new PropertyMetadata(false, OnIsReadOnlyChanged));
    public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register(nameof(Visibility), typeof(Visibility), typeof(TableViewColumn), new PropertyMetadata(Visibility.Visible, OnVisibilityChanged));
    public static readonly DependencyProperty TagProperty = DependencyProperty.Register(nameof(Tag), typeof(object), typeof(TableViewColumn), new PropertyMetadata(null));
}