using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace WinUI3.TableView;

public class TableViewRowPresenter : StackPanel
{
    private TableView? _tableView;

    public IEnumerable<TableViewCell> GetCells()
    {
        return Children.Select(x => x.FindDescendantOrSelf<TableViewCell>()!);
    }

    internal void SelectNextCell(TableViewCell? currentCell)
    {
        _tableView ??= this.FindAscendant<TableView>();

        if (_tableView is not null)
        {
            var cells = GetCells().ToList();
            var nextCellIndex = currentCell is null ? 0 : cells.IndexOf(currentCell) + 1;
            if (nextCellIndex < cells.Count)
            {
                var nextCell = cells[nextCellIndex];
                if (nextCell.IsReadOnly)
                {
                    SelectNextCell(nextCell);
                }
                else
                {
                    nextCell.PrepareForEdit();
                }
            }
            else
            {
                _tableView.SelectNextRow();
            }
        }
    }

    internal void SelectPreviousCell(TableViewCell? currentCell)
    {
        _tableView ??= this.FindAscendant<TableView>();

        if (_tableView is not null)
        {
            var cells = GetCells().ToList();
            var previousCellIndex = currentCell is null ? cells.Count - 1 : cells.IndexOf(currentCell) - 1;
            if (previousCellIndex >= 0)
            {
                var previousCell = cells[previousCellIndex];
                if (previousCell.IsReadOnly)
                {
                    SelectPreviousCell(previousCell);
                }
                previousCell.PrepareForEdit();
            }
            else
            {
                _tableView.SelectPreviousRow();
            }
        }
    }
}
