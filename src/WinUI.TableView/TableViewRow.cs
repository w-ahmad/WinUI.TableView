using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace WinUI.TableView;
public class TableViewRow : ListViewItem
{
    private TableViewCellPresenter? _cellPresenter;
    private ListViewItemPresenter _itemPresenter = null!;

    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemPresenter = (ListViewItemPresenter)GetTemplateChild("Root");
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        if (TableView is null)
        {
            return;
        }

        if (_cellPresenter is null)
        {
            _cellPresenter = ContentTemplateRoot as TableViewCellPresenter;
            if (_cellPresenter is not null)
            {
                _cellPresenter.Children.Clear();
                AddCells(TableView.Columns);
            }
        }
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> newItems)
        {
            AddCells(newItems.Where(x => x.Visibility == Visibility.Visible), e.NewStartingIndex);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> oldItems)
        {
            RemoveCells(oldItems);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && _cellPresenter is not null)
        {
            _cellPresenter.Children.Clear();
        }
    }

    private void OnColumnPropertyChanged(object? sender, TableViewColumnPropertyChanged e)
    {
        if (e.PropertyName is nameof(TableViewColumn.Visibility))
        {
            if (e.Column.Visibility == Visibility.Visible)
            {
                AddCells(new[] { e.Column }, e.Index);
            }
            else
            {
                RemoveCells(new[] { e.Column });
            }
        }
        else if (e.PropertyName is nameof(TableViewColumn.ActualWidth))
        {
            if (Cells.FirstOrDefault(x => x.Column == e.Column) is { } cell)
            {
                cell.Width = e.Column.ActualWidth;
            }
        }
    }

    private void RemoveCells(IEnumerable<TableViewColumn> columns)
    {
        if (_cellPresenter is not null)
        {
            foreach (var column in columns)
            {
                var cell = _cellPresenter.Children.OfType<TableViewCell>().FirstOrDefault(x => x.Column == column);
                if (cell is not null)
                {
                    _cellPresenter.Children.Remove(cell);
                }
            }
        }
    }

    private void AddCells(IEnumerable<TableViewColumn> columns, int index = -1)
    {
        if (_cellPresenter is not null)
        {
            foreach (var column in columns)
            {
                var cell = new TableViewCell { Column = column, Row = this, TableView = TableView!, Index = TableView.Columns.IndexOf(column), Width = column.ActualWidth };
                if (index < 0)
                {
                    _cellPresenter.Children.Add(cell);
                }
                else
                {
                    index = Math.Min(index, _cellPresenter.Children.Count);
                    _cellPresenter.Children.Insert(index, cell);
                    index++;
                }
            }
        }
    }

    internal void SelectNextCell(TableViewCell? currentCell)
    {
        if (TableView is not null)
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
                TableView.SelectNextRow();
            }
        }
    }

    internal void SelectPreviousCell(TableViewCell? currentCell)
    {
        if (TableView is not null)
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
                TableView.SelectPreviousRow();
            }
        }
    }

    private static void OnTableViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TableViewRow row)
        {
            return;
        }

        if (e.NewValue is TableView newTableView && newTableView.Columns is not null)
        {
            newTableView.Columns.CollectionChanged += row.OnColumnsCollectionChanged;
            newTableView.Columns.ColumnPropertyChanged += row.OnColumnPropertyChanged;
            newTableView.SelectedCellsChanged += row.OnCellSelectionChanged;
        }

        if (e.OldValue is TableView oldTableView && oldTableView.Columns is not null)
        {
            oldTableView.Columns.CollectionChanged -= row.OnColumnsCollectionChanged;
            oldTableView.Columns.ColumnPropertyChanged -= row.OnColumnPropertyChanged;
            oldTableView.SelectedCellsChanged -= row.OnCellSelectionChanged;
        }
    }

    private static void OnIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as TableViewRow)?.ApplyCellsSelectionState();
    }

    private void OnCellSelectionChanged(object? sender, CellSelectionChangedEvenArgs e)
    {
        if (e.OldSelection.Any(x => x.Row == Index) ||
           e.NewSelection.Any(x => x.Row == Index))
        {
            ApplyCellsSelectionState();
        }
    }

    internal void ApplyCellsSelectionState()
    {
        foreach (var cell in Cells)
        {
            cell.ApplySelectionState();
        }
    }

    internal IList<TableViewCell> Cells => _cellPresenter?.Cells ?? new List<TableViewCell>();

    public int Index => (int)GetValue(IndexProperty);

    public TableView TableView
    {
        get => (TableView)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(TableViewRow), new PropertyMetadata(-1, OnIndexChanged));
    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewRow), new PropertyMetadata(default, OnTableViewChanged));
}

public class TableViewCellPresenter : StackPanel
{
    public TableViewCellPresenter()
    {
        Orientation = Orientation.Horizontal;
    }

    public IList<TableViewCell> Cells => Children.OfType<TableViewCell>().ToList().AsReadOnly();
}