using System.ComponentModel;

namespace WinUI.TableView;

public partial class TableViewColumnHeader
{
    /// <summary>
    /// Represents a filter item used in the options flyout of a TableViewColumnHeader.
    /// </summary>
    private class FilterItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isSelected;
        private readonly OptionsFlyoutViewModel _optionsFlyoutViewModel;

        /// <summary>
        /// Initializes a new instance of the FilterItem class.
        /// </summary>
        /// <param name="isSelected">Indicates whether the filter item is selected.</param>
        /// <param name="value">The value of the filter item.</param>
        /// <param name="optionsFlyoutViewModel">The ViewModel for the options flyout.</param>
        public FilterItem(bool isSelected, object value, OptionsFlyoutViewModel optionsFlyoutViewModel)
        {
            IsSelected = isSelected;
            Value = value;

            _optionsFlyoutViewModel = optionsFlyoutViewModel;
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

                _optionsFlyoutViewModel?.SetSelectAllCheckBoxState();
            }
        }

        /// <summary>
        /// Gets the value of the filter item.
        /// </summary>
        public object Value { get; }
    }
}
