using System.Linq;
using WinUI.TableView.Extensions;
using SD = WinUI.TableView.SortDirection;

namespace WinUI.TableView;

partial class TableViewColumn
{
    /// <summary>
    /// Gets a value indicating whether the column can be sorted right now: both the column and its
    /// <see cref="TableView"/> allow it.
    /// </summary>
    private bool IsSortingAllowed => TableView?.CanSortColumns is true && CanSort;

    /// <summary>
    /// Sorts the owning <see cref="TableView"/> by this column, as clicking the column's header does.
    /// </summary>
    /// <param name="direction">The direction to sort in, or <see langword="null"/> to remove this column's sort.</param>
    /// <param name="clearOtherSorting">
    /// <see langword="true"/> to replace every other column's sort, as a plain header click does;
    /// <see langword="false"/> to sort by this column in addition to them, as a Ctrl+click does.
    /// </param>
    /// <returns>
    /// <see langword="false"/> if the column can't be sorted (<see cref="CanSort"/> or
    /// <see cref="TableView.CanSortColumns"/> is off, or it isn't in a <see cref="TableView"/>) or a
    /// <see cref="TableView.Sorting"/> handler handled the request; otherwise <see langword="true"/>.
    /// </returns>
    public bool Sort(SD? direction, bool clearOtherSorting = true)
    {
        if (!IsSortingAllowed || TableView is not { CollectionView: CollectionView { } collectionView } tableView)
        {
            return false;
        }

        var eventArgs = new TableViewSortingEventArgs(this);
        tableView.OnSorting(eventArgs);

        if (eventArgs.Handled) return false;

        using var defer = collectionView.DeferRefresh();
        var sortDescription = collectionView.SortDescriptions
            .FirstOrDefault(x => (x is ColumnSortDescription columnSort && columnSort.Column == this) ||
                                 (x is ColumnGroupDescription columnGroup && columnGroup.Column == this));

        if (clearOtherSorting)
        {
            tableView.ClearAllSortingWithEvent();
        }
        else
        {
            ClearSortingWithEvent();
        }

        if (direction is not null)
        {
#if WINDOWS
            if (sortDescription is ColumnSortDescription && IsGrouped)
            {
                var groupDescription = collectionView.GroupDescriptions
                    .FirstOrDefault(x => x is ColumnGroupDescription columnGroup && columnGroup.Column == this);

                groupDescription?.Direction = direction.Value;
            }
#endif

            SortDirection = direction;

            // Prefer explicit SortMemberPath if provided, otherwise use bound column's property path
            var sortPath = SortMemberPath ?? (this as TableViewBoundColumn)?.PropertyPath;
            sortDescription ??= new ColumnSortDescription(this, sortPath);
            sortDescription.Direction = direction.Value;

            collectionView.SortDescriptions.Add(sortDescription);
        }

        return true;
    }

    /// <summary>
    /// Removes this column's sort, raising <see cref="TableView.ClearSorting"/> first.
    /// </summary>
    internal void ClearSortingWithEvent()
    {
        var eventArgs = new TableViewClearSortingEventArgs();
        TableView?.OnClearSorting(eventArgs);

        if (eventArgs.Handled)
        {
            return;
        }

        if (IsSortingAllowed && TableView?.CollectionView is CollectionView { } collectionView)
        {
            using var defer = collectionView.DeferRefresh();
            TableView.DeselectAll();

            SortDirection = null;
            collectionView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && columnSort.Column == this);
        }
    }

#if WINDOWS
    /// <summary>
    /// Gets a value indicating whether the column can be grouped right now: both the column and its
    /// <see cref="TableView"/> allow it.
    /// </summary>
    internal bool IsGroupingAllowed => TableView?.CanGroupColumns is true && CanGroup;

    /// <summary>
    /// Gets a value indicating whether the owning <see cref="TableView"/> is grouped by this column.
    /// </summary>
    public bool IsGrouped =>
        TableView?.CollectionView is CollectionView { } collectionView &&
        collectionView.GroupDescriptions.Any(x => x is ColumnGroupDescription columnGroup && columnGroup.Column == this);

    /// <summary>
    /// Groups the owning <see cref="TableView"/> by this column, as the header's Group command does. When the
    /// table is already grouped, this column's groups nest inside the existing ones.
    /// </summary>
    /// <remarks>
    /// Grouping by a column, unlike adding a <see cref="GroupDescription"/> yourself, ties the grouping to the
    /// column: its header shows the group order and offers Ungroup and Sort Groups by Count.
    /// </remarks>
    /// <returns>
    /// <see langword="false"/> if the column can't be grouped (<see cref="CanGroup"/> or
    /// <see cref="TableView.CanGroupColumns"/> is off, or it isn't in a <see cref="TableView"/>), is already
    /// grouped, or a <see cref="TableView.Grouping"/> handler handled the request; otherwise <see langword="true"/>.
    /// </returns>
    public bool Group()
    {
        if (!IsGroupingAllowed || IsGrouped || TableView is not { CollectionView: CollectionView { } collectionView } tableView)
        {
            return false;
        }

        var eventArgs = new TableViewGroupingEventArgs(this);
        tableView.OnGrouping(eventArgs);

        if (eventArgs.Handled) return false;

        // Prefer explicit SortMemberPath if provided, otherwise use bound column's property path
        var sortPath = SortMemberPath ?? (this as TableViewBoundColumn)?.PropertyPath;

        using var defer = collectionView.DeferRefresh();

        var direction = SortDirection ?? SD.Ascending;

        var groupDescription = new ColumnGroupDescription(this, sortPath, direction);
        collectionView.GroupDescriptions.Add(groupDescription);

        if (SortDirection is null)
        {
            collectionView.SortDescriptions.Add(groupDescription);
            SortDirection = direction;
        }

        return true;
    }

    /// <summary>
    /// Removes the owning <see cref="TableView"/>'s grouping by this column, as the header's Ungroup command does.
    /// A sort the column had before it was grouped stays; the grouping's own order goes.
    /// </summary>
    /// <returns><see langword="false"/> if the table wasn't grouped by this column; otherwise <see langword="true"/>.</returns>
    public bool Ungroup()
    {
        if (!IsGrouped || TableView is not { CollectionView: CollectionView { } collectionView })
        {
            return false;
        }

        using var defer = collectionView.DeferRefresh();
        collectionView.GroupDescriptions.RemoveWhere(x => x is ColumnGroupDescription columnGroup && columnGroup.Column == this);
        collectionView.SortDescriptions.RemoveWhere(x => x is ColumnGroupDescription columnSort && columnSort.Column == this);

        var sortDescription = collectionView.SortDescriptions.FirstOrDefault(x => x is ColumnSortDescription columnSort && columnSort.Column == this);
        SortDirection = sortDescription?.Direction;

        return true;
    }
#endif
}
