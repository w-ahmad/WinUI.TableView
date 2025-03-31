using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Represents a filter item used in the options flyout of a TableViewColumnHeader.
/// </summary>
public  partial class TableViewFilterItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the FilterItem class.
    /// </summary>
    /// <param name="isSelected">Indicates whether the filter item is selected.</param>
    /// <param name="value">The value of the filter item.</param>
    public TableViewFilterItem(bool isSelected, object value)
    {
        IsSelected = isSelected;
        Value = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the filter item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    /// <summary>
    /// Gets the value of the filter item.
    /// </summary>
    public object Value { get; }
}
