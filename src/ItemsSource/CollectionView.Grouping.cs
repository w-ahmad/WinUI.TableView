using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WinUI.TableView;

/// <summary>
/// Grouping support for <see cref="CollectionView"/>.
/// </summary>
/// <remarks>
/// Grouping is expressed entirely as spans over the flat item view: <c>_view</c> stays a flat list of data items
/// ordered by group key and then by the sort descriptions, and <see cref="Groups"/> describes where each group
/// starts and how many items it covers. Item indexes therefore remain contiguous within a group and every
/// index-based API (cell slots, selection ranges, the clipboard) keeps working unchanged.
/// </remarks>
partial class CollectionView
{
    private readonly ObservableCollection<GroupDescription> _groupDescriptions = [];
    private readonly List<TableViewGroup> _groups = [];
    private bool _suspendGroupMaintenance;

    /// <summary>
    /// Gets the collection of group descriptions applied to the items, outermost level first.
    /// </summary>
    public IList<GroupDescription> GroupDescriptions => _groupDescriptions;

    /// <summary>
    /// Gets the groups in document order: for every group, at every level, the span of items it covers.
    /// </summary>
    /// <remarks>
    /// Ordered so that a group always appears before the groups and items it contains, which is the order group
    /// header rows appear in.
    /// </remarks>
    internal IReadOnlyList<TableViewGroup> Groups => _groups;

    /// <summary>
    /// Gets a value indicating whether any grouping is applied.
    /// </summary>
    public bool IsGrouped => _groupDescriptions.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the view has to be ordered. Group keys order items just as sort
    /// descriptions do, so either one requires the ordering pass.
    /// </summary>
    private bool NeedsSorting => _sortDescriptions.Count > 0 || _groupDescriptions.Count > 0;

    /// <summary>
    /// Re-evaluates the grouping applied to the view.
    /// </summary>
    public void RefreshGrouping()
    {
        HandleSourceChanged();
    }

    /// <summary>
    /// Handles changes to the group descriptions collection.
    /// </summary>
    private void OnGroupDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_deferCounter > 0) return;

        HandleSourceChanged();
    }

    /// <summary>
    /// Rebuilds <see cref="Groups"/> from the current view in a single pass.
    /// </summary>
    private void RebuildGroups()
    {
        _groups.Clear();

        var levels = _groupDescriptions.Count;

        if (levels is 0 || _view.Count is 0)
        {
            return;
        }

        var openGroups = new TableViewGroup?[levels];

        for (var index = 0; index < _view.Count; index++)
        {
            var item = _view[index];
            var startedNewGroup = false;

            for (var level = 0; level < levels; level++)
            {
                var key = _groupDescriptions[level].GetPropertyValue(item);

                // A new group at one level forces a new group at every deeper level, even when the deeper key
                // happens to repeat the previous group's key.
                if (!startedNewGroup && openGroups[level] is { } openGroup && Equals(openGroup.Key, key))
                {
                    continue;
                }

                startedNewGroup = true;
                openGroups[level] = new TableViewGroup(key, level, index, level is 0 ? null : openGroups[level - 1]);
                _groups.Add(openGroups[level]!);
            }

            for (var level = 0; level < levels; level++)
            {
                openGroups[level]!.ItemCount++;
            }
        }
    }

    /// <summary>
    /// Grows the groups covering a newly inserted item, or rebuilds when the item starts a new group.
    /// </summary>
    /// <param name="viewIndex">The index the item was inserted at; the view already contains it.</param>
    private void OnItemAddedToGroups(int viewIndex)
    {
        if (_groupDescriptions.Count is 0 || _suspendGroupMaintenance)
        {
            return;
        }

        // The neighbour whose groups the new item could join. Group spans are still expressed in pre-insert
        // indexes at this point, and the neighbour's pre-insert index is the one below.
        var neighbourIndex = viewIndex > 0 ? viewIndex - 1 : 1;

        if (neighbourIndex >= _view.Count || !HasSameGroupKeys(_view[viewIndex], _view[neighbourIndex]))
        {
            RebuildGroups();
            return;
        }

        var anchor = viewIndex > 0 ? viewIndex - 1 : 0;

        foreach (var group in _groups)
        {
            if (group.FirstItemIndex > anchor)
            {
                group.FirstItemIndex++;
            }
            else if (group.LastItemIndex >= anchor)
            {
                group.ItemCount++;
            }
        }
    }

    /// <summary>
    /// Shrinks the groups that covered a removed item, dropping any that became empty.
    /// </summary>
    /// <param name="viewIndex">The index the item was removed from; the view no longer contains it.</param>
    private void OnItemRemovedFromGroups(int viewIndex)
    {
        if (_groupDescriptions.Count is 0 || _suspendGroupMaintenance)
        {
            return;
        }

        for (var i = _groups.Count - 1; i >= 0; i--)
        {
            var group = _groups[i];

            if (group.FirstItemIndex > viewIndex)
            {
                group.FirstItemIndex--;
            }
            else if (group.LastItemIndex >= viewIndex)
            {
                group.ItemCount--;

                if (group.ItemCount is 0)
                {
                    _groups.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether two items belong in the same group at every level.
    /// </summary>
    private bool HasSameGroupKeys(object? x, object? y)
    {
        foreach (var description in _groupDescriptions)
        {
            if (!Equals(description.GetPropertyValue(x), description.GetPropertyValue(y)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a property change on an item can move it between groups.
    /// </summary>
    private bool AffectsGrouping(string propertyName)
    {
        foreach (var description in _groupDescriptions)
        {
            if (string.IsNullOrEmpty(description.PropertyName) || description.PropertyName == propertyName)
            {
                return true;
            }
        }

        return false;
    }
}
