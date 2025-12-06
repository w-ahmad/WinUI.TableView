using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WinUI.TableView;

/// <summary>
/// Represents a collection of columns in a <see cref="WinUI.TableView.TableView"/>, providing functionality to manage and interact with the columns.
/// </summary>
/// <remarks>This interface extends <see cref="IList{T}"/> to provide standard list operations for <see
/// cref="TableViewColumn"/> objects. It also implements <see cref="INotifyCollectionChanged"/> to notify subscribers of
/// changes to the collection, such as additions or removals.</remarks>
public interface ITableViewColumnsCollection : IList<TableViewColumn>, INotifyCollectionChanged
{
    /// <summary>
    /// Occurs when a property of a column changes.
    /// </summary>
    /// <remarks>
    /// This event is triggered to notify subscribers about changes to column properties, such as width, visibility, or other attributes.
    /// Handlers can use the <see cref="TableViewColumnPropertyChangedEventArgs"/> parameter to access details about the specific property that changed.
    /// </remarks>
    event EventHandler<TableViewColumnPropertyChangedEventArgs>? ColumnPropertyChanged;

    /// <summary>
    /// Moves a column from one index to another within the collection.
    /// </summary>
    /// <param name="oldIndex">The zero-based index of the column to move.</param>
    /// <param name="newIndex">The zero-based index to move the column to.</param>
    void Move(int oldIndex, int newIndex);

    /// <summary>
    /// Gets the list of visible <see cref="TableViewColumn"/>s.
    /// </summary>
    /// <remarks>
    /// The result is a list of columns that are currently visible in the table view, meaning their <see cref="TableViewColumn.Visibility"/> is set to <see cref="Visibility.Visible"/>.
    /// The result is also ordered by the <see cref="TableViewColumn.Order"/> property, allowing for a consistent display order of the columns.
    /// </remarks>
    IList<TableViewColumn> VisibleColumns { get; }

    /// <summary>
    /// Gets or sets the <see cref="WinUI.TableView.TableView"/> associated with the collection.
    /// </summary>
    /// <remarks>
    /// This property allows access to the <see cref="WinUI.TableView.TableView"/> that owns this collection of columns.
    /// </remarks>
    TableView? TableView { get; }
}
