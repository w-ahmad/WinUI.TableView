using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace WinUI.TableView;

partial class TableViewHeaderRow
{
    private bool _commandsInitialized;
    private MenuFlyoutItem? _exportAllMenuItem;
    private MenuFlyoutItem? _exportSelectedMenuItem;
    private readonly StandardUICommand _selectAllCommand = new(StandardUICommandKind.SelectAll) { Label = TableViewLocalizedStrings.SelectAll };
    private readonly StandardUICommand _deselectAllCommand = new() { Label = TableViewLocalizedStrings.DeselectAll };
    private readonly StandardUICommand _copyCommand = new(StandardUICommandKind.Copy) { Label = TableViewLocalizedStrings.Copy };
    private readonly StandardUICommand _pasteCommand = new(StandardUICommandKind.Paste) { Label = TableViewLocalizedStrings.Paste };
    private readonly StandardUICommand _copyWithHeadersCommand = new() { Label = TableViewLocalizedStrings.CopyWithHeaders };
    private readonly StandardUICommand _clearSortingCommand = new() { Label = TableViewLocalizedStrings.ClearSorting };
    private readonly StandardUICommand _clearFilterCommand = new() { Label = TableViewLocalizedStrings.ClearFilter };
    private readonly StandardUICommand _exportAllToCSVCommand = new() { Label = TableViewLocalizedStrings.ExportAll };
    private readonly StandardUICommand _exportSelectedToCSVCommand = new() { Label = TableViewLocalizedStrings.ExportSelected };

    /// <summary>
    /// Sets the visibility of the export options.
    /// </summary>
    internal void SetExportOptionsVisibility()
    {
        var visibility = TableView?.ShowExportOptions is true ? Visibility.Visible : Visibility.Collapsed;

        _exportAllMenuItem?.Visibility = visibility;
        _exportSelectedMenuItem?.Visibility = visibility;
    }

    /// <summary>
    /// Sets commands to option menu items.
    /// </summary>
    private void SetOptionCommands()
    {
        InitializeCommands();

        if (GetTemplateChild("SelectAllMenuItem") is MenuFlyoutItem selectAllMenuItem)
            selectAllMenuItem.Command = _selectAllCommand;
        if (GetTemplateChild("ClearSelectionMenuItem") is MenuFlyoutItem clearSelectionMenuItem)
            clearSelectionMenuItem.Command = _deselectAllCommand;
        if (GetTemplateChild("CopyMenuItem") is MenuFlyoutItem copyMenuItem)
            copyMenuItem.Command = _copyCommand;
        if (GetTemplateChild("PasteMenuItem") is MenuFlyoutItem pasteMenuItem)
            pasteMenuItem.Command = _pasteCommand;
        if (GetTemplateChild("CopyWithHeadersMenuItem") is MenuFlyoutItem copyWithHeadersMenuItem)
            copyWithHeadersMenuItem.Command = _copyWithHeadersCommand;
        if (GetTemplateChild("ClearSortingMenuItem") is MenuFlyoutItem clearSortingMenuItem)
            clearSortingMenuItem.Command = _clearSortingCommand;
        if (GetTemplateChild("ClearFilterMenuItem") is MenuFlyoutItem clearFilterMenuItem)
            clearFilterMenuItem.Command = _clearFilterCommand;
        if (GetTemplateChild("ExportAllMenuItem") is MenuFlyoutItem exportAllMenuItem)
        {
            _exportAllMenuItem = exportAllMenuItem;
            exportAllMenuItem.Command = _exportAllToCSVCommand;
        }
        if (GetTemplateChild("ExportSelectedMenuItem") is MenuFlyoutItem exportSelectedMenuItem)
        {
            _exportSelectedMenuItem = exportSelectedMenuItem;
            exportSelectedMenuItem.Command = _exportSelectedToCSVCommand;
        }
    }

    /// <summary>
    /// Initializes the commands.
    /// </summary>
    private void InitializeCommands()
    {
        if (_commandsInitialized)
        {
            return;
        }

        _selectAllCommand.Description = TableViewLocalizedStrings.SelectAllCommandDescription;
        _selectAllCommand.ExecuteRequested += delegate { TableView?.SelectAll(); };
        _selectAllCommand.CanExecuteRequested += CanExecuteSelectAllCommand;

        _deselectAllCommand.Description = TableViewLocalizedStrings.DeselectAllCommandDescription;
        _deselectAllCommand.ExecuteRequested += delegate { TableView?.DeselectAll(); };
        _deselectAllCommand.CanExecuteRequested += CanExecuteDeselectAllCommand;

        _copyCommand.Description = TableViewLocalizedStrings.CopyCommandDescription;
        _copyCommand.ExecuteRequested += ExecuteCopyCommand;
        _copyCommand.CanExecuteRequested += CanExecuteCopyCommand;

        _pasteCommand.Description = TableViewLocalizedStrings.PasteCommandDescription;
        _pasteCommand.ExecuteRequested += delegate { TableView?.TryStartPasteFromClipboard(); };
        _pasteCommand.CanExecuteRequested += CanExecutePasteCommand;

        _copyWithHeadersCommand.Description = TableViewLocalizedStrings.CopyWithHeadersCommandDescription;
        _copyWithHeadersCommand.ExecuteRequested += delegate { TableView?.CopyToClipboardInternal(true); };
        _copyWithHeadersCommand.CanExecuteRequested += CanExecuteCopyCommand;

        _clearSortingCommand.ExecuteRequested += delegate { TableView?.ClearAllSortingWithEvent(); };
        _clearSortingCommand.CanExecuteRequested += CanExecuteClearSortingCommand;

        _clearFilterCommand.ExecuteRequested += delegate { TableView?.FilterHandler.ClearFilter(default); };
        _clearFilterCommand.CanExecuteRequested += CanExecuteClearFilterCommand;

        _exportAllToCSVCommand.ExecuteRequested += delegate { TableView?.ExportAllToCSV(); };

        _exportSelectedToCSVCommand.ExecuteRequested += delegate { TableView?.ExportSelectedToCSV(); };
        _exportSelectedToCSVCommand.CanExecuteRequested += CanExecuteExportSelectedToCSVCommand;
        _commandsInitialized = true;
    }

    private void CanExecuteSelectAllCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = TableView?.IsEditing is false && TableView.SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended;
    }

    private void CanExecuteDeselectAllCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = TableView?.IsEditing is false && (TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0);
    }

    private void ExecuteCopyCommand(XamlUICommand sender, ExecuteRequestedEventArgs e)
    {
        var focusedElement = FocusManager.GetFocusedElement(TableView?.XamlRoot!);
        if (focusedElement is FrameworkElement { Parent: TableViewCell })
        {
            return;
        }

        TableView?.CopyToClipboardInternal(false);
    }

    private void CanExecuteCopyCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = TableView?.CanCopy is true
            && (TableView?.SelectedItems.Count > 0 || TableView?.SelectedCells.Count > 0 || TableView?.CurrentCellSlot.HasValue is true);
    }

    private void CanExecutePasteCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = false;
        var canExecute = TableView?.CanPaste is true && TableView?.IsEditing is false && TableView?.IsReadOnly is false;

        if (canExecute)
        {
            var content = Clipboard.GetContent();
            e.CanExecute = canExecute && content.Contains(StandardDataFormats.Text);
        }
    }

    private void CanExecuteClearSortingCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = TableView?.IsEditing is false && TableView.IsSorted;
    }

    private void CanExecuteClearFilterCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = TableView?.IsEditing is false && TableView.IsFiltered;
    }

    private void CanExecuteExportSelectedToCSVCommand(XamlUICommand sender, CanExecuteRequestedEventArgs e)
    {
        e.CanExecute = TableView?.IsEditing is false && (TableView.SelectedItems.Count > 0 || TableView.SelectedCells.Count > 0 || TableView.CurrentCellSlot.HasValue);
    }
}
