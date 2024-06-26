﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace WinUI.TableView;

public partial class TableViewHeaderRow
{
    private class OptionsFlyoutViewModel
    {
        public OptionsFlyoutViewModel(TableView _tableView)
        {
            InitializeCommands();
            TableView = _tableView;
        }

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
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.CollectionView.SortDescriptions.Count > 0;

            ClearFilterCommand.ExecuteRequested += delegate { TableView.ClearFilters(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.ActiveFilters.Count > 0;

            ExportAllToCSVCommand.ExecuteRequested += delegate { TableView.ExportAllToCSV(); };

            ExportSelectedToCSVCommand.ExecuteRequested += delegate { TableView.ExportSelectedToCSV(); };
            ExportSelectedToCSVCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue;
        }

        public StandardUICommand SelectAllCommand { get; } = new(StandardUICommandKind.SelectAll);
        public StandardUICommand DeselectAllCommand { get; } = new() { Label = "Deselect All" };
        public StandardUICommand CopyCommand { get; } = new(StandardUICommandKind.Copy);
        public StandardUICommand CopyWithHeadersCommand { get; } = new() { Label = "Copy with Headers" };
        public StandardUICommand ClearSortingCommand { get; } = new() { Label = "Clear Sorting" };
        public StandardUICommand ClearFilterCommand { get; } = new() { Label = "Clear Filter" };
        public StandardUICommand ExportAllToCSVCommand { get; } = new() { Label = "Export All to CSV" };
        public StandardUICommand ExportSelectedToCSVCommand { get; } = new() { Label = "Export Selected to CSV" };
        public TableView TableView { get; }
    }
}
