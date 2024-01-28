using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinUI3.TableView;

public abstract class TableViewBoundColumn : TableViewColumn
{
    private Binding _binding = new();

    public Binding Binding
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

    public bool CanSort
    {
        get => (bool)GetValue(CanSortProperty);
        set => SetValue(CanSortProperty, value);
    }

    public static readonly DependencyProperty CanSortProperty = DependencyProperty.Register(nameof(CanSort), typeof(bool), typeof(TableViewBoundColumn), new PropertyMetadata(true));
}
