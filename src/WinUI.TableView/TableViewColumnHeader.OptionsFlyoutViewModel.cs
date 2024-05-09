using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SD = CommunityToolkit.WinUI.Collections.SortDirection;

namespace WinUI.TableView;

public partial class TableViewColumnHeader
{
    private class OptionsFlyoutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private string? _filterText;
        private List<FilterItem> _filterItems = new();
        private bool _canSetState = true;

        public OptionsFlyoutViewModel(TableView tableView, TableViewColumnHeader columnHeader)
        {
            TableView = tableView;
            ColumnHeader = columnHeader;
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            SortAscendingCommand.ExecuteRequested += delegate { ColumnHeader.DoSort(SD.Ascending); };
            SortAscendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.CanSort && ColumnHeader.SortDirection != SD.Ascending;

            SortDescendingCommand.ExecuteRequested += delegate { ColumnHeader.DoSort(SD.Descending); };
            SortDescendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.CanSort && ColumnHeader.SortDirection != SD.Descending;

            ClearSortingCommand.ExecuteRequested += delegate { ColumnHeader.ClearSorting(); };
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.SortDirection is not null;

            ClearFilterCommand.ExecuteRequested += delegate { ColumnHeader.ClearFilter(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = ColumnHeader.IsFiltered;

            OkCommand.ExecuteRequested += delegate
            {
                ColumnHeader.HideFlyout();

                if (ColumnHeader!._selectAllCheckBox!.IsChecked is true)
                {
                    ColumnHeader.ClearFilter();
                }
                else
                {
                    SelectedValues = FilterItems.Where(x => x.IsSelected).Select(x => x.Value).ToList();
                    ColumnHeader.ApplyFilter();
                }
            };

            CancelCommand.ExecuteRequested += delegate { ColumnHeader.HideFlyout(); };
        }

        public TableView TableView { get; }
        public TableViewColumnHeader ColumnHeader { get; }
        public string? FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                ColumnHeader.PrepareFilterItems(_filterText);
                OnPropertyChanged();
            }
        }

        public List<FilterItem> FilterItems
        {
            get => _filterItems;
            set
            {
                _filterItems = value;
                SetSelectAllCheckBoxState();
                OnPropertyChanged();
            }
        }

        public List<object> SelectedValues { get; private set; } = new();

        internal void SetSelectAllCheckBoxState()
        {
            if (ColumnHeader._selectAllCheckBox is null || !_canSetState)
            {
                return;
            }

            ColumnHeader._selectAllCheckBox.IsChecked = _filterItems.All(x => x.IsSelected)
                                                        ? true
                                                        : _filterItems.All(x => !x.IsSelected)
                                                        ? false
                                                        : null;
        }

        internal void SetFilterItemsState(bool isSelected)
        {
            _canSetState = false;
            FilterItems.ForEach(x => x.IsSelected = isSelected);
            _canSetState = true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = default)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public StandardUICommand SortAscendingCommand { get; } = new() { Label = "Sort Ascending" };
        public StandardUICommand SortDescendingCommand { get; } = new() { Label = "Sort Descending" };
        public StandardUICommand ClearSortingCommand { get; } = new() { Label = "Clear Sorting" };
        public StandardUICommand ClearFilterCommand { get; } = new() { Label = "Clear Filter" };
        public StandardUICommand OkCommand { get; } = new() { Label = "OK" };
        public StandardUICommand CancelCommand { get; } = new() { Label = "Cancel" };
    }
}
