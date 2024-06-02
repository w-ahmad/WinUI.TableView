using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView;

public class TableViewColumnsCollection : ObservableCollection<TableViewColumn>
{
    private readonly Dictionary<TableViewColumn, long> _callbackMap = new();
    internal event EventHandler<TableViewColumnVisibilityChanged>? ColumnVisibilityChanged;
    internal IList<TableViewColumn> VisibleColumns => Items.Where(x => x.Visibility == Visibility.Visible).ToList();

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (e.NewItems != null)
        {
            var index = e.NewStartingIndex;

            foreach (var column in e.NewItems.OfType<TableViewColumn>())
            {
                var token = column.RegisterPropertyChangedCallback(TableViewColumn.VisibilityProperty, OnColumnVisibilityChanged);
                _callbackMap.Remove(column);
                _callbackMap.Add(column, token);
            }
        }

        if (e.OldItems != null)
        {
            foreach (var column in e.OldItems.OfType<TableViewColumn>())
            {
                var token = _callbackMap[column];
                column.UnregisterPropertyChangedCallback(TableViewColumn.VisibilityProperty, token);
            }
        }
    }

    private void OnColumnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is TableViewColumn column)
        {
            var index = IndexOf(column);
            ColumnVisibilityChanged?.Invoke(this, new TableViewColumnVisibilityChanged(column, index));
        }
    }
}

internal class TableViewColumnVisibilityChanged : EventArgs
{
    public TableViewColumnVisibilityChanged(TableViewColumn column, int index)
    {
        Column = column;
        Index = index;
    }

    public TableViewColumn Column { get; }
    public int Index { get; }
}
