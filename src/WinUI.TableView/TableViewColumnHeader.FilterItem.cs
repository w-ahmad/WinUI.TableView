using System.ComponentModel;

namespace WinUI.TableView;

public partial class TableViewColumnHeader
{
    private class FilterItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isSelected;
        private readonly OptionsFlyoutViewModel _optionsFlyoutViewModel;

        public FilterItem(bool isSelected, object value, OptionsFlyoutViewModel optionsFlyoutViewModel)
        {
            IsSelected = isSelected;
            Value = value;

            _optionsFlyoutViewModel = optionsFlyoutViewModel;
        }

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

        public object Value { get; }
    }
}
