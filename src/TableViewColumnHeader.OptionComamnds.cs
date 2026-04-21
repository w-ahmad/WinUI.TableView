using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUI.TableView.Extensions;
using SD = WinUI.TableView.SortDirection;

namespace WinUI.TableView;

partial class TableViewColumnHeader
{
    private readonly StandardUICommand _sortAscendingCommand = new() { Label = TableViewLocalizedStrings.SortAscending };
    private readonly StandardUICommand _sortDescendingCommand = new() { Label = TableViewLocalizedStrings.SortDescending };
    private readonly StandardUICommand _clearSortingCommand = new() { Label = TableViewLocalizedStrings.ClearSorting };
    private readonly StandardUICommand _clearFilterCommand = new() { Label = TableViewLocalizedStrings.ClearFilter };
    private readonly StandardUICommand _okCommand = new() { Label = TableViewLocalizedStrings.Ok };
    private readonly StandardUICommand _cancelCommand = new() { Label = TableViewLocalizedStrings.Cancel };

    /// <summary>
    /// Sets commands to option menu items.
    /// </summary>
    private void SetOptionCommands()
    {
        InitializeCommands();

        if (GetTemplateChild("SortAscendingMenuItem") is MenuFlyoutItem sortAscendingMenuItem)
            sortAscendingMenuItem.Command = _sortAscendingCommand;
        if (GetTemplateChild("SortDescendingMenuItem") is MenuFlyoutItem sortDescendingMenuItem)
            sortDescendingMenuItem.Command = _sortDescendingCommand;
        if (GetTemplateChild("ClearSortingMenuItem") is MenuFlyoutItem clearSortingMenuItem)
            clearSortingMenuItem.Command = _clearSortingCommand;
        if (GetTemplateChild("ClearFilterMenuItem") is MenuFlyoutItem clearFilterMenuItem)
            clearFilterMenuItem.Command = _clearFilterCommand;
        if (GetTemplateChild("ActionButtonsMenuItem") is MenuFlyoutItem actionButtonsMenuItem)
        {
            actionButtonsMenuItem.ApplyTemplate();

            if (actionButtonsMenuItem.FindDescendant<Button>(b => b.Name is "OkButton") is { } okButton)
                okButton.Command = _okCommand;
            if (actionButtonsMenuItem.FindDescendant<Button>(b => b.Name is "CancelButton") is { } cancelButton)
                cancelButton.Command = _cancelCommand;
        }
    }

    /// <summary>
    /// Initializes the commands.
    /// </summary>
    private void InitializeCommands()
    {
        _sortAscendingCommand.ExecuteRequested += delegate { DoSort(SD.Ascending); };
        _sortAscendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = CanSort && Column?.SortDirection != SD.Ascending;

        _sortDescendingCommand.ExecuteRequested += delegate { DoSort(SD.Descending); };
        _sortDescendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = CanSort && Column?.SortDirection != SD.Descending;

        _clearSortingCommand.ExecuteRequested += delegate { ClearSortingWithEvent(); };
        _clearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = Column?.SortDirection is not null;

        _clearFilterCommand.ExecuteRequested += delegate { ClearFilter(); };
        _clearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = Column?.IsFiltered is true;

        _okCommand.ExecuteRequested += delegate { ExecuteOkCommand(); };

        _cancelCommand.ExecuteRequested += delegate { HideFlyout(); };
    }

    internal void ExecuteOkCommand()
    {
        HideFlyout();
        ApplyFilter();
    }
}
