using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

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
            SelectAllCommand.CanExecuteRequested += CanExecuteSelectAllCommand;

            DeselectAllCommand.Description = "Deselect all rows.";
            DeselectAllCommand.ExecuteRequested += delegate { TableView.DeselectAll(); };
            DeselectAllCommand.CanExecuteRequested += CanExecuteDeselectAllCommand;

            CopyCommand.Description = "Copy the selected row's content to clipboard.";
            CopyCommand.ExecuteRequested += ExecuteCopyCommand;

            CopyCommand.CanExecuteRequested += CanExecuteCopyCommand;

            CopyWithHeadersCommand.Description = "Copy the selected row's content including column headers to clipboard.";
            CopyWithHeadersCommand.ExecuteRequested += delegate { TableView.CopyToClipboardInternal(true); };
            CopyWithHeadersCommand.CanExecuteRequested += CanExecuteCopyWithHeadersCommand;

            ClearSortingCommand.ExecuteRequested += delegate { TableView.ClearAllSortingWithEvent(); };
            ClearSortingCommand.CanExecuteRequested += CanExecuteClearSortingCommand;

            ClearFilterCommand.ExecuteRequested += delegate { TableView.FilterHandler.ClearFilter(default); };
            ClearFilterCommand.CanExecuteRequested += CanExecuteClearFilterCommand;

            ExportAllToCSVCommand.ExecuteRequested += delegate { TableView.ExportAllToCSV(); };

            ExportSelectedToCSVCommand.ExecuteRequested += delegate { TableView.ExportSelectedToCSV(); };
            ExportSelectedToCSVCommand.CanExecuteRequested += CanExecuteExportSelectedToCSVCommand;
        }

        private void CanExecuteSelectAllCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = !TableView.IsEditing && TableView.SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended;
        }

        private void CanExecuteDeselectAllCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = !TableView.IsEditing && (TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0);
        }

        private void ExecuteCopyCommand(XamlUICommand sender, ExecuteRequestedEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement(TableView.XamlRoot);
            if (focusedElement is FrameworkElement { Parent: TableViewCell })
            {
                return;
            }

            TableView.CopyToClipboardInternal(false);
        }

        private void CanExecuteCopyCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue;
        }

        private void CanExecuteCopyWithHeadersCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue;
        }

        private void CanExecuteClearSortingCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = !TableView.IsEditing && TableView.IsSorted;
        }

        private void CanExecuteClearFilterCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = !TableView.IsEditing && TableView.IsFiltered;
        }

        private void CanExecuteExportSelectedToCSVCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
        {
            e.CanExecute = !TableView.IsEditing && (TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue);
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
