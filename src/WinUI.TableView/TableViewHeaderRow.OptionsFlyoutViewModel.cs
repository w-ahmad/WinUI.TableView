using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using WinUIEx;

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
            DeselectAllCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.A,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
            });
            DeselectAllCommand.ExecuteRequested += delegate { TableView.DeselectAll(); };

            CopyCommand.Description = "Copy the selected row's content to clipboard.";
            CopyCommand.ExecuteRequested += delegate { TableView.CopyToClipboard(false); };

            CopyWithHeadersCommand.Description = "Copy the selected row's content including column headers to clipboard.";
            CopyWithHeadersCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.C,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
            });
            CopyWithHeadersCommand.ExecuteRequested += delegate { TableView.CopyToClipboard(true); };

            ClearSortingCommand.ExecuteRequested += delegate { ClearSorting(); };
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.CollectionView.SortDescriptions.Count > 0;

            ClearFilterCommand.ExecuteRequested += delegate { ClearFilters(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.ActiveFilters.Count > 0;

            ExportAllToCSVCommand.ExecuteRequested += delegate { ExportAllToCSV(); };

            ExportSelectedToCSVCommand.ExecuteRequested += delegate { ExportSelectedToCSV(); };
            ExportSelectedToCSVCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.SelectedItems.Count > 0;
        }

        private async void ExportSelectedToCSV()
        {
            try
            {
                var hWnd = HwndExtensions.GetActiveWindow();
                if (await GetStorageFile(hWnd) is not { } file)
                {
                    return;
                }

                var content = TableView.GetSelectedRowsContent(true, ',');
                using var stream = await file.OpenStreamForWriteAsync();
                stream.SetLength(0);

                using var tw = new StreamWriter(stream);
                await tw.WriteAsync(content);
            }
            catch { }
        }

        private async void ExportAllToCSV()
        {
            try
            {
                var hWnd = HwndExtensions.GetActiveWindow();
                if (await GetStorageFile(hWnd) is not { } file)
                {
                    return;
                }

                var content = TableView.GetAllRowsContent(true, ',');
                using var stream = await file.OpenStreamForWriteAsync();
                stream.SetLength(0);

                using var tw = new StreamWriter(stream);
                await tw.WriteAsync(content);
            }
            catch { }
        }

        private static async Task<StorageFile> GetStorageFile(IntPtr hWnd)
        {
            var savePicker = new FileSavePicker();
            InitializeWithWindow.Initialize(savePicker, hWnd);
            savePicker.FileTypeChoices.Add("CSV (Comma delimited)", new List<string>() { ".csv" });

            return await savePicker.PickSaveFileAsync();
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
