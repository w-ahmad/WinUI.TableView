using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SD = CommunityToolkit.WinUI.Collections.SortDirection;

namespace WinUI.TableView;

public partial class TableViewColumnHeader
{
    /// <summary>
    /// ViewModel for the options flyout in the TableViewColumnHeader.
    /// </summary>
    private class OptionsFlyoutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private string? _filterText;
        private List<FilterItem> _filterItems = new();
        private bool _canSetState = true;

        /// <summary>
        /// Initializes a new instance of the OptionsFlyoutViewModel class.
        /// </summary>
        /// <param name="tableView">The TableView associated with the ViewModel.</param>
        /// <param name="columnHeader">The TableViewColumnHeader associated with the ViewModel.</param>
        public OptionsFlyoutViewModel(TableView tableView, TableViewColumnHeader columnHeader)
        {
            TableView = tableView;
            ColumnHeader = columnHeader;
            InitializeCommands();
        }

        /// <summary>
        /// Initializes the commands for the ViewModel.
        /// </summary>
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

                if (ColumnHeader!._selectAllCheckBox!.IsChecked is true && string.IsNullOrEmpty(FilterText))
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

        /// <summary>
        /// Gets the TableView associated with the ViewModel.
        /// </summary>
        public TableView TableView { get; }

        /// <summary>
        /// Gets the TableViewColumnHeader associated with the ViewModel.
        /// </summary>
        public TableViewColumnHeader ColumnHeader { get; }

        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the filter items.
        /// </summary>
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

        /// <summary>
        /// Gets the selected values for the filter.
        /// </summary>
        public List<object> SelectedValues { get; private set; } = new();

        /// <summary>
        /// Sets the state of the select all checkbox.
        /// </summary>
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

        /// <summary>
        /// Sets the state of the filter items.
        /// </summary>
        /// <param name="isSelected">The state to set.</param>
        internal void SetFilterItemsState(bool isSelected)
        {
            _canSetState = false;
            FilterItems.ForEach(x => x.IsSelected = isSelected);
            _canSetState = true;
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = default)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets the command to sort in ascending order.
        /// </summary>
        public StandardUICommand SortAscendingCommand { get; } = new() { Label = "Sort Ascending" };

        /// <summary>
        /// Gets the command to sort in descending order.
        /// </summary>
        public StandardUICommand SortDescendingCommand { get; } = new() { Label = "Sort Descending" };

        /// <summary>
        /// Gets the command to clear sorting.
        /// </summary>
        public StandardUICommand ClearSortingCommand { get; } = new() { Label = "Clear Sorting" };

        /// <summary>
        /// Gets the command to clear the filter.
        /// </summary>
        public StandardUICommand ClearFilterCommand { get; } = new() { Label = "Clear Filter" };

        /// <summary>
        /// Gets the command to confirm the filter.
        /// </summary>
        public StandardUICommand OkCommand { get; } = new() { Label = "OK" };

        /// <summary>
        /// Gets the command to cancel the filter.
        /// </summary>
        public StandardUICommand CancelCommand { get; } = new() { Label = "Cancel" };
    }
}
