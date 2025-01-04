using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace WinUI.TableView;

public partial class TableViewHeaderRow
{
    /// <summary>
    /// ViewModel for the options flyout in the TableViewHeaderRow.
    /// </summary>
    private class OptionsFlyoutViewModel
    {
        /// <summary>
        /// Initializes a new instance of the OptionsFlyoutViewModel class.
        /// </summary>
        /// <param name="_tableView">The TableView associated with the ViewModel.</param>
        public OptionsFlyoutViewModel(TableView _tableView)
        {
            InitializeCommands();
            TableView = _tableView;
        }

        /// <summary>
        /// Initializes the commands for the ViewModel.
        /// </summary>
        private void InitializeCommands()
        {
            SelectAllCommand.Description = "Select all rows.";
            SelectAllCommand.ExecuteRequested += delegate { TableView.SelectAll(); };
            SelectAllCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended;

            DeselectAllCommand.Description = "Deselect all rows.";
            DeselectAllCommand.ExecuteRequested += delegate { TableView.DeselectAll(); };
            DeselectAllCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0;

            CopyCommand.Description = "Copy the selected row's content to clipboard.";
            CopyCommand.ExecuteRequested += delegate
            {
                var focusedElement = FocusManager.GetFocusedElement(TableView.XamlRoot);
                if (focusedElement is FrameworkElement { Parent: TableViewCell })
                {
                    return;
                }

                TableView.CopyToClipboardInternal(false);
            };
            CopyCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue;

            CopyWithHeadersCommand.Description = "Copy the selected row's content including column headers to clipboard.";
            CopyWithHeadersCommand.ExecuteRequested += delegate { TableView.CopyToClipboardInternal(true); };
            CopyWithHeadersCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue;

            ClearSortingCommand.ExecuteRequested += delegate { TableView.ClearSorting(); };
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SortDescriptions.Count > 0;

            ClearFilterCommand.ExecuteRequested += delegate { TableView.ClearFilters(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.FilterDescriptions.Count > 0;

            ExportAllToCSVCommand.ExecuteRequested += delegate { TableView.ExportAllToCSV(); };

            ExportSelectedToCSVCommand.ExecuteRequested += delegate { TableView.ExportSelectedToCSV(); };
            ExportSelectedToCSVCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue;
        }

        /// <summary>
        /// Gets the command to select all rows.
        /// </summary>
        public StandardUICommand SelectAllCommand { get; } = new(StandardUICommandKind.SelectAll);

        /// <summary>
        /// Gets the command to deselect all rows.
        /// </summary>
        public StandardUICommand DeselectAllCommand { get; } = new() { Label = "Deselect All" };

        /// <summary>
        /// Gets the command to copy the selected row's content to the clipboard.
        /// </summary>
        public StandardUICommand CopyCommand { get; } = new(StandardUICommandKind.Copy);

        /// <summary>
        /// Gets the command to copy the selected row's content including column headers to the clipboard.
        /// </summary>
        public StandardUICommand CopyWithHeadersCommand { get; } = new() { Label = "Copy with Headers" };

        /// <summary>
        /// Gets the command to clear sorting.
        /// </summary>
        public StandardUICommand ClearSortingCommand { get; } = new() { Label = "Clear Sorting" };

        /// <summary>
        /// Gets the command to clear filters.
        /// </summary>
        public StandardUICommand ClearFilterCommand { get; } = new() { Label = "Clear Filter" };

        /// <summary>
        /// Gets the command to export all content to a CSV file.
        /// </summary>
        public StandardUICommand ExportAllToCSVCommand { get; } = new() { Label = "Export All to CSV" };

        /// <summary>
        /// Gets the command to export selected content to a CSV file.
        /// </summary>
        public StandardUICommand ExportSelectedToCSVCommand { get; } = new() { Label = "Export Selected to CSV" };

        /// <summary>
        /// Gets the TableView associated with the ViewModel.
        /// </summary>
        public TableView TableView { get; }
    }
}
