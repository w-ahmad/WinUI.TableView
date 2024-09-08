using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView;

public class TableViewColumnsCollection : ObservableCollection<TableViewColumn>
{
    internal event EventHandler<TableViewColumnPropertyChanged>? ColumnPropertyChanged;
    internal IList<TableViewColumn> VisibleColumns => Items.Where(x => x.Visibility == Visibility.Visible).ToList();

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (e.NewItems != null)
        {
            foreach (var column in e.NewItems.OfType<TableViewColumn>())
            {
                column.SetOwningCollection(this);
                column.SetOwningTableView(TableView!);
            }
        }

        if (e.OldItems != null)
        {
            foreach (var column in e.OldItems.OfType<TableViewColumn>())
            {
                column.SetOwningCollection(null!);
                column.SetOwningTableView(null!);
            }
        }
    }

    internal void HandleColumnPropertyChanged(TableViewColumn column, string propertyName)
    {
        if (Items.Contains(column))
        {
            var index = IndexOf(column);
            ColumnPropertyChanged?.Invoke(this, new TableViewColumnPropertyChanged(column, propertyName, index));
        }
    }

    public TableView? TableView { get; internal set; }
}

internal class TableViewColumnPropertyChanged : EventArgs
{
    public TableViewColumnPropertyChanged(TableViewColumn column, string propertyName, int index)
    {
        Column = column;
        PropertyName = propertyName;
        Index = index;
    }

    public TableViewColumn Column { get; }
    public string PropertyName { get; }
    public int Index { get; }
}
