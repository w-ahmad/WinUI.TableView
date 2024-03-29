using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Linq;

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
            SelectAllCommand.ExecuteRequested += delegate { TableView.SelectAllSafe(); };

            DeselectAllCommand.Description = "Deselect all rows.";
            DeselectAllCommand.ExecuteRequested += delegate { TableView.DeselectAll(); };

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

            CopyWithHeadersCommand.Description = "Copy the selected row's content including column headers to clipboard.";
            CopyWithHeadersCommand.ExecuteRequested += delegate { TableView.CopyToClipboardInternal(true); };

            ClearSortingCommand.ExecuteRequested += delegate { ClearSorting(); };
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.CollectionView.SortDescriptions.Count > 0;

            ClearFilterCommand.ExecuteRequested += delegate { ClearFilters(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.ActiveFilters.Count > 0;

            ExportAllToCSVCommand.ExecuteRequested += delegate { TableView.ExportAllToCSV(); };

            ExportSelectedToCSVCommand.ExecuteRequested += delegate { TableView.ExportSelectedToCSV(); };
            ExportSelectedToCSVCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0;
        }

        private void ClearSorting()
        {
            TableView.CollectionView.SortDescriptions.Clear();

            foreach (var header in TableView.Columns.Select(x => x.HeaderControl))
            {
                if (header is not null)
                {
                    header.SortDirection = null;
                }
            }
        }

        private void ClearFilters()
        {
            TableView.ActiveFilters.Clear();
            TableView.CollectionView.RefreshFilter();

            foreach (var header in TableView.Columns.Select(x => x.HeaderControl))
            {
                if (header is not null)
                {
                    header.IsFiltered = false;
                }
            }
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
