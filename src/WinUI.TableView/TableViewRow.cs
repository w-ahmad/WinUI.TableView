using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView;

public class TableViewRow : Control
{
    private TableView? _tableView;
    private StackPanel? _cellsStackPanel;

    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _cellsStackPanel = GetTemplateChild("PART_StackPanel") as StackPanel;

        if (_tableView is not null)
        {
            _tableView.Columns.CollectionChanged += OnColumnsCollectionChanged;
            GenerateCells(_tableView.Columns);
        }
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> newItems)
        {
            GenerateCells(newItems, e.NewStartingIndex);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> oldItems)
        {
            RemoveCells(oldItems);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && _cellsStackPanel is not null)
        {
            _cellsStackPanel.Children.Clear();
        }
    }

    private void RemoveCells(IEnumerable<TableViewColumn> columns)
    {
        if (_cellsStackPanel is not null)
        {
            foreach (TableViewColumn column in columns)
            {
                var cell = _cellsStackPanel.Children.OfType<TableViewCell>().FirstOrDefault(x => x.Column == column);
                if (cell is not null)
                {
                    _cellsStackPanel.Children.Remove(cell);
                }
            }
        }
    }

    private void GenerateCells(IEnumerable<TableViewColumn> columns, int index = -1)
    {
        if (_cellsStackPanel is not null)
        {
            foreach (TableViewColumn column in columns)
            {
                var cell = new TableViewCell { Column = column, IsTabStop = false, };
                if (index < 0)
                {
                    _cellsStackPanel.Children.Add(cell);
                }
                else
                {
                    index = Math.Min(index, _cellsStackPanel.Children.Count);
                    _cellsStackPanel.Children.Insert(index, cell);
                    index++;
                }
            }
        }
    }

    internal void SelectNextCell(TableViewCell? currentCell)
    {
        _tableView ??= this.FindAscendant<TableView>();

        if (_tableView is not null)
        {
            var nextCellIndex = currentCell is null ? 0 : Cells.IndexOf(currentCell) + 1;
            if (nextCellIndex < Cells.Count)
            {
                var nextCell = Cells[nextCellIndex];
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
            var previousCellIndex = currentCell is null ? Cells.Count - 1 : Cells.IndexOf(currentCell) - 1;
            if (previousCellIndex >= 0)
            {
                var previousCell = Cells[previousCellIndex];
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

    public IList<TableViewCell> Cells => (_cellsStackPanel?.Children.OfType<TableViewCell>() ?? Enumerable.Empty<TableViewCell>()).ToList();
}
