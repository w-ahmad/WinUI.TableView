using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView;

public class TableViewRow : Control
{
    private TableView? _tableView;
    private StackPanel? _stackPanel;

    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _stackPanel = GetTemplateChild("PART_StackPanel") as StackPanel;

        if (_tableView is not null)
        {
            _tableView.Columns.CollectionChanged += OnColumnsCollectionChanged;
        }

        GenerateCells();
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GenerateCells();
    }

    private void GenerateCells()
    {
        if (_tableView is null || _stackPanel is null)
        {
            return;
        }

        _stackPanel.Children.Clear();

        foreach (var column in _tableView.Columns)
        {
            _stackPanel.Children.Add(new TableViewCell { Column = column });
        }
    }

    public IEnumerable<TableViewCell> GetCells()
    {
        return _stackPanel?.Children.OfType<TableViewCell>() ?? Enumerable.Empty<TableViewCell>();
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
