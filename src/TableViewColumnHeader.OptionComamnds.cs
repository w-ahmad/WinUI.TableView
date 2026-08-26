using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SD = WinUI.TableView.SortDirection;

namespace WinUI.TableView;

partial class TableViewColumnHeader
{
    private bool _commandsInitialized;
#if WINDOWS
    private readonly StandardUICommand _groupCommand = new() { Label = TableViewLocalizedStrings.Group };
    private readonly StandardUICommand _sortGroupsByCountCommand = new() { Label = TableViewLocalizedStrings.SortGroupsByCount };
#endif
    private readonly StandardUICommand _sortAscendingCommand = new() { Label = TableViewLocalizedStrings.SortAscending };
    private readonly StandardUICommand _sortDescendingCommand = new() { Label = TableViewLocalizedStrings.SortDescending };
    private readonly StandardUICommand _clearSortingCommand = new() { Label = TableViewLocalizedStrings.ClearSorting };
    private readonly StandardUICommand _clearFilterCommand = new() { Label = TableViewLocalizedStrings.ClearFilter };

    /// <summary>
    /// Sets commands to option menu items.
    /// </summary>
    private void SetOptionCommands()
    {
        InitializeCommands();

#if WINDOWS
        if (GetTemplateChild("GroupMenuItem") is MenuFlyoutItem groupMenuItem)
            groupMenuItem.Command = _groupCommand;
        if (GetTemplateChild("SortGroupsByCountMenuItem") is MenuFlyoutItem sortGroupsByCountMenuItem)
            sortGroupsByCountMenuItem.Command = _sortGroupsByCountCommand;
#endif
        if (GetTemplateChild("SortAscendingMenuItem") is MenuFlyoutItem sortAscendingMenuItem)
            sortAscendingMenuItem.Command = _sortAscendingCommand;
        if (GetTemplateChild("SortDescendingMenuItem") is MenuFlyoutItem sortDescendingMenuItem)
            sortDescendingMenuItem.Command = _sortDescendingCommand;
        if (GetTemplateChild("ClearSortingMenuItem") is MenuFlyoutItem clearSortingMenuItem)
            clearSortingMenuItem.Command = _clearSortingCommand;
        if (GetTemplateChild("ClearFilterMenuItem") is MenuFlyoutItem clearFilterMenuItem)
            clearFilterMenuItem.Command = _clearFilterCommand;
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

#if WINDOWS
        _groupCommand.ExecuteRequested += delegate { Group(); };
        _groupCommand.CanExecuteRequested += (_, e) =>
        {
            e.CanExecute = CanGroup;
            _groupCommand.Label = IsGrouped ? TableViewLocalizedStrings.Ungroup : TableViewLocalizedStrings.Group;
        };

        _sortGroupsByCountCommand.ExecuteRequested += delegate
        {
            if (_tableView?.CollectionView is CollectionView { } collectionView)
            {
                ToggleGroupSortMode(collectionView);
            }
        };
        _sortGroupsByCountCommand.CanExecuteRequested += (_, e) =>
        {
            e.CanExecute = IsGrouped;
            _sortGroupsByCountCommand.Label = IsGroupSortedByCount
                ? TableViewLocalizedStrings.SortGroupsByValue
                : TableViewLocalizedStrings.SortGroupsByCount;
        };
#endif

        _sortAscendingCommand.ExecuteRequested += delegate { DoSort(SD.Ascending); };
        _sortAscendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = CanSort && Column?.SortDirection != SD.Ascending;

        _sortDescendingCommand.ExecuteRequested += delegate { DoSort(SD.Descending); };
        _sortDescendingCommand.CanExecuteRequested += (_, e) => e.CanExecute = CanSort && Column?.SortDirection != SD.Descending;

        _clearSortingCommand.ExecuteRequested += delegate { ClearSortingWithEvent(); };
        _clearSortingCommand.CanExecuteRequested += (_, e) =>
        {
            // A grouped column with no independent sort left over is driven entirely by its group -
            // "clear sorting" isn't offered for it; ungroup instead. Only three-state (asc -> desc ->
            // clear) sorting is disabled, not the asc/desc toggle itself (handled by DoSort/SortGroupDescription).
            // While a leftover ColumnSortDescription from before grouping is still mirroring the group's
            // order, clearing it is allowed and hands ordering fully over to the group description.
            var canClear = Column?.SortDirection is not null;
#if WINDOWS
            canClear = canClear && (!IsGrouped || HasGroupSortCompanion);
#endif
            e.CanExecute = canClear;
        };

        _clearFilterCommand.ExecuteRequested += delegate { ClearFilter(); };
        _clearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = Column?.IsFiltered is true;

        _commandsInitialized = true;
    }
}
