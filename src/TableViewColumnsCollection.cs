using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace WinUI.TableView;

/// <summary>
/// Represents a collection of columns in a TableView.
/// </summary>
public partial class TableViewColumnsCollection : ObservableCollection<TableViewColumn>
{
    /// <summary>
    /// Occurs when a property of a column in the collection changes.
    /// </summary>
    internal event EventHandler<TableViewColumnPropertyChangedEventArgs>? ColumnPropertyChanged;

    /// <summary>
    /// Gets the list of visible columns in the collection.
    /// </summary>
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

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    /// <param name="column">The column that changed.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    internal void HandleColumnPropertyChanged(TableViewColumn column, string propertyName)
    {
        if (Items.Contains(column))
        {
            var index = IndexOf(column);
            ColumnPropertyChanged?.Invoke(this, new TableViewColumnPropertyChangedEventArgs(column, propertyName, index));
        }
    }

    /// <summary>
    /// Gets or sets the TableView associated with the collection.
    /// </summary>
    public TableView? TableView { get; internal set; }
}
