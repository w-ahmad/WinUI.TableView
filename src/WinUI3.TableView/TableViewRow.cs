using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace WinUI3.TableView;

public class TableViewRow : ItemsControl
{
    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);
    }

    public IEnumerable<TableViewCell> GetCells()
    {
        return this.FindDescendant<ItemsStackPanel>()!.Children.Select(x => x.FindDescendantOrSelf<TableViewCell>()!);
    }

    internal void SelectNextCell(TableViewCell? currentCell)
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
            nextCell.PrepareForEdit();
        }
        else
        {
            TableView.SelectNextRow();
        }
    }

    internal void SelectPreviousCell(TableViewCell? currentCell)
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
            TableView.SelectPreviousRow();
        }
    }

    public TableView TableView
    {
        get => (TableView)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewRow), new PropertyMetadata(null));
}
