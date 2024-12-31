using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that is bound to a data source.
/// </summary>
public abstract class TableViewBoundColumn : TableViewColumn
{
    private Binding _binding = new();

    /// <summary>
    /// Gets or sets the binding for the column.
    /// </summary>
    public virtual Binding Binding
    {
        get => _binding;
        set
        {
            _binding = value;
            if (_binding is not null)
            {
                _binding.Mode = BindingMode.TwoWay;
            }
        }
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
    /// Identifies the CanSort dependency property.
    /// </summary>
    public static readonly DependencyProperty CanSortProperty = DependencyProperty.Register(nameof(CanSort), typeof(bool), typeof(TableViewBoundColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the CanFilter dependency property.
    /// </summary>
    public static readonly DependencyProperty CanFilterProperty = DependencyProperty.Register(nameof(CanFilter), typeof(bool), typeof(TableViewBoundColumn), new PropertyMetadata(true));
}
