using System.ComponentModel;

namespace WinUI.TableView;

public partial class TableViewColumnHeader
{
    private class FilterItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool isSelected;
        private readonly OptionsFlyoutViewModel _optionsFlyoutViewModel;

        public FilterItem(bool isSelected, object value, OptionsFlyoutViewModel optionsFlyoutViewModel)
        {
            IsSelected = isSelected;
            Value = value;

            _optionsFlyoutViewModel = optionsFlyoutViewModel;
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));

                _optionsFlyoutViewModel?.SetSelectAllCheckBoxState();
            }
        }

        public object Value { get; }
    }
}
