using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Represents a filter item used in the options flyout of a TableViewColumnHeader.
/// </summary>
public partial class TableViewFilterItem : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the FilterItem class.
    /// </summary>
    /// <param name="isSelected">Indicates whether the filter item is selected.</param>
    /// <param name="value">The value of the filter item.</param>
    /// <param name="count">The count of occurrences for the filter item.</param>
    /// <param name="showCount">Indicates whether to show the count of occurrences.</param>
    public TableViewFilterItem(bool isSelected, object? value, int count = 1, bool showCount = false)
    {
        IsSelected = isSelected;
        Value = value;
        Count = count;
        ShowCount = showCount;
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
    public object? Value { get; }

    /// <summary>
    /// Gets the text representation of the filter item's value, or a localized string for blank values if the value is null.
    /// </summary>
    public string ValueText => Value?.ToString() ?? TableViewLocalizedStrings.BlankFilterValue;

    /// <summary>
    /// Gets or sets the count of occurrences for the filter item.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets a value indicating whether to show the count for the filter item.
    /// </summary>
    public bool ShowCount { get; }
}
