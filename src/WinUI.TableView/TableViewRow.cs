using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView;

public class TableViewRow : ListViewItem
{
    private TableViewCellsPresenter? _cellPresenter;
    private bool _ensureCells = true;

    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);
        SizeChanged += OnSizeChanged;
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (_ensureCells)
        {
            EnsureCells();
        }
        else
        {
            foreach (var cell in Cells)
            {
                cell.SetElement();
            }
        }
    }

    private void EnsureCells()
    {
        if (TableView is null)
        {
            return;
        }

        _cellPresenter = ContentTemplateRoot as TableViewCellsPresenter;
        if (_cellPresenter is not null)
        {
            _cellPresenter.Children.Clear();
            AddCells(TableView.Columns.VisibleColumns);
        }

        _ensureCells = false;
    }

    private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (TableView.CurrentCellSlot?.Row == Index)
        {
            _ = await TableView.ScrollCellIntoView(TableView.CurrentCellSlot.Value);
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
                var cell = new TableViewCell
                {
                    Row = this,
                    Column = column,
                    TableView = TableView!,
                    Index = TableView.Columns.VisibleColumns.IndexOf(column),
                    Width = column.ActualWidth,
                };

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
            newTableView.CurrentCellChanged += row.OnCurrentCellChanged;
        }

        if (e.OldValue is TableView oldTableView && oldTableView.Columns is not null)
        {
            oldTableView.Columns.CollectionChanged -= row.OnColumnsCollectionChanged;
            oldTableView.Columns.ColumnPropertyChanged -= row.OnColumnPropertyChanged;
            oldTableView.SelectedCellsChanged -= row.OnCellSelectionChanged;
            oldTableView.CurrentCellChanged -= row.OnCurrentCellChanged;
        }
    }

    private void OnCellSelectionChanged(object? sender, TableViewCellSelectionChangedEvenArgs e)
    {
        if (e.OldSelection.Any(x => x.Row == Index) ||
            e.NewSelection.Any(x => x.Row == Index))
        {
            ApplyCellsSelectionState();
        }
    }

    private void OnCurrentCellChanged(object? sender, TableViewCurrentCellChangedEventArgs e)
    {
        if (e.OldSlot?.Row == Index)
        {
            ApplyCurrentCellState(e.OldSlot.Value);
        }

        if (e.NewSlot?.Row == Index)
        {
            ApplyCurrentCellState(e.NewSlot.Value);
        }
    }

    internal void ApplyCurrentCellState(TableViewCellSlot slot)
    {
        if (slot.Column >= 0 && slot.Column < Cells.Count)
        {
            var cell = Cells[slot.Column];
            cell.ApplyCurrentCellState();
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

    public int Index => TableView.IndexFromContainer(this);

    public TableView TableView
    {
        get => (TableView)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewRow), new PropertyMetadata(default, OnTableViewChanged));
}
