using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Partial class for TableView that contains row grouping logic.
/// </summary>
public partial class TableView
{
    /// <summary>
    /// Represents a sentinel item inserted into the display list to render a group header row.
    /// </summary>
    private sealed class GroupHeaderRowItem
    {
        public required object GroupKey { get; init; }

        public required string Header { get; init; }
    }

    /// <summary>
    /// Sentinel object used as a dictionary key when the group property value is null.
    /// </summary>
    private static readonly object NullGroupKey = new();

    private readonly ObservableCollection<object> _displayItems = [];

    // Maps GroupHeaderRowItem sentinels to their display text (e.g., "Engineering (4)").
    private readonly Dictionary<object, string> _groupHeadersByItem = new(ReferenceEqualityComparer.Instance);

    // Maps any item (data or sentinel) to its normalized group key.
    private readonly Dictionary<object, object> _groupKeysByItem = new(ReferenceEqualityComparer.Instance);

    // Maps a group key to its GroupHeaderRowItem sentinel object.
    private readonly Dictionary<object, object> _groupHeaderItemsByKey = [];

    private readonly HashSet<object> _collapsedGroupKeys = [];
    private readonly Dictionary<(Type Type, string Path), Func<object, object?>?> _propertyPathAccessorCache = [];
    private SortDescription? _groupSortDescription;
    private bool _isUpdatingGroupingSortDescription;
    private bool _isDisplayedItemsRebuildQueued;
    private bool _isEnsureGroupingSortQueued;

    /// <summary>
    /// Subscribes to events needed for grouping. Called from the constructor.
    /// </summary>
    private void InitializeGrouping()
    {
        _collectionView.VectorChanged += OnCollectionViewVectorChanged;

        if (SortDescriptions is INotifyCollectionChanged sortDescriptions)
        {
            sortDescriptions.CollectionChanged += OnSortDescriptionsCollectionChanged;
        }
    }

    private void OnCollectionViewVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        QueueDisplayedItemsRebuild();
    }

    private void OnSortDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isEnsureGroupingSortQueued || _isUpdatingGroupingSortDescription)
        {
            return;
        }

        _isEnsureGroupingSortQueued = true;

        if (DispatcherQueue is null)
        {
            _isEnsureGroupingSortQueued = false;
            EnsureGroupingSortDescription();
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            _isEnsureGroupingSortQueued = false;
            EnsureGroupingSortDescription();
        });
    }

    private void QueueDisplayedItemsRebuild()
    {
        if (_isDisplayedItemsRebuildQueued)
        {
            return;
        }

        _isDisplayedItemsRebuildQueued = true;

        if (DispatcherQueue is null)
        {
            _isDisplayedItemsRebuildQueued = false;
            RebuildDisplayedItems();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() =>
        {
            _isDisplayedItemsRebuildQueued = false;
            RebuildDisplayedItems();
        });
    }

    /// <summary>
    /// Ensures the group sort description is at index 0 of SortDescriptions.
    /// </summary>
    internal void EnsureGroupingSortDescription()
    {
        if (_isUpdatingGroupingSortDescription)
        {
            return;
        }

        var desiredPath = string.IsNullOrWhiteSpace(GroupByPath) ? null : GroupByPath;

        if (_groupSortDescription is not null && desiredPath is not null
            && _groupSortDescription.PropertyName == desiredPath
            && _groupSortDescription.Direction == GroupSortDirection
            && SortDescriptions.IndexOf(_groupSortDescription) == 0)
        {
            return;
        }

        _isUpdatingGroupingSortDescription = true;

        try
        {
            using var defer = _collectionView.DeferRefresh();

            if (_groupSortDescription is not null)
            {
                SortDescriptions.Remove(_groupSortDescription);
                _groupSortDescription = null;
            }

            if (desiredPath is not null)
            {
                _groupSortDescription = new SortDescription(desiredPath, GroupSortDirection);
                SortDescriptions.Insert(0, _groupSortDescription);
            }
        }
        finally
        {
            _isUpdatingGroupingSortDescription = false;
        }
    }

    /// <summary>
    /// Rebuilds the display items list from the collection view, injecting group header sentinels.
    /// </summary>
    internal void RebuildDisplayedItems()
    {
        BuildGroupHeadersFromCurrentView();

        _displayItems.Clear();

        if (!string.IsNullOrWhiteSpace(GroupByPath) && ShowGroupHeaders)
        {
            object? previousGroupKey = null;
            var hasPreviousGroup = false;

            foreach (var item in _collectionView.OfType<object>())
            {
                var groupKey = GetNormalizedGroupKeyForItem(item);

                if (!hasPreviousGroup || !Equals(previousGroupKey, groupKey))
                {
                    if (_groupHeaderItemsByKey.TryGetValue(groupKey, out var headerItem))
                    {
                        _displayItems.Add(headerItem);
                    }

                    previousGroupKey = groupKey;
                    hasPreviousGroup = true;
                }

                if (!_collapsedGroupKeys.Contains(groupKey))
                {
                    _displayItems.Add(item);
                }
            }

            return;
        }

        foreach (var item in _collectionView.OfType<object>())
        {
            _displayItems.Add(item);
        }
    }

    /// <summary>
    /// Scans the collection view and builds group header sentinels and tracking dictionaries.
    /// </summary>
    private void BuildGroupHeadersFromCurrentView()
    {
        _groupHeadersByItem.Clear();
        _groupKeysByItem.Clear();
        _groupHeaderItemsByKey.Clear();

        if (string.IsNullOrWhiteSpace(GroupByPath))
        {
            return;
        }

        var groupedItems = _collectionView.OfType<object>()
                                          .Select(item => new
                                          {
                                              Item = item,
                                              GroupKey = ResolvePropertyPathValue(item, GroupByPath!)
                                          })
                                          .ToList();

        if (groupedItems.Count == 0)
        {
            return;
        }

        var groupStartIndex = 0;

        for (var index = 1; index <= groupedItems.Count; index++)
        {
            var isBoundary = index == groupedItems.Count
                             || !Equals(groupedItems[index - 1].GroupKey, groupedItems[index].GroupKey);

            if (!isBoundary)
            {
                continue;
            }

            var startItem = groupedItems[groupStartIndex];
            var count = index - groupStartIndex;
            var groupKey = NormalizeGroupKey(startItem.GroupKey);

            for (var groupItemIndex = groupStartIndex; groupItemIndex < index; groupItemIndex++)
            {
                _groupKeysByItem[groupedItems[groupItemIndex].Item] = groupKey;
            }

            if (ShowGroupHeaders)
            {
                var headerItem = new GroupHeaderRowItem
                {
                    GroupKey = groupKey,
                    Header = FormatGroupHeader(startItem.GroupKey, count)
                };

                _groupHeaderItemsByKey[groupKey] = headerItem;
                _groupKeysByItem[headerItem] = groupKey;
                _groupHeadersByItem[headerItem] = headerItem.Header;
            }

            groupStartIndex = index;
        }
    }

    private string FormatGroupHeader(object? groupKey, int count)
    {
        var title = groupKey?.ToString() ?? "(null)";

        if (!ShowGroupItemCount)
        {
            return title;
        }

        return $"{title} ({count})";
    }

    private object? ResolvePropertyPathValue(object item, string propertyPath)
    {
        var key = (item.GetType(), propertyPath);

        if (!_propertyPathAccessorCache.TryGetValue(key, out var accessor))
        {
            accessor = item.GetFuncCompiledPropertyPath(propertyPath);
            _propertyPathAccessorCache[key] = accessor;
        }

        return accessor?.Invoke(item);
    }

    /// <summary>
    /// Tries to get the display text for a group header sentinel.
    /// </summary>
    internal bool TryGetGroupHeader(object? item, out string header)
    {
        if (item is not null && _groupHeadersByItem.TryGetValue(item, out var h))
        {
            header = h;
            return true;
        }

        header = string.Empty;
        return false;
    }

    /// <summary>
    /// Returns true if the item is a group header sentinel, not a data item.
    /// </summary>
    internal bool IsGroupHeaderItem(object? item)
    {
        return item is GroupHeaderRowItem;
    }

    /// <summary>
    /// Returns true if the item is a selectable data item (not a group header).
    /// </summary>
    internal bool IsSelectableItem(object? item)
    {
        return item is not GroupHeaderRowItem;
    }

    /// <summary>
    /// Returns true if the group containing the given item is expanded.
    /// </summary>
    internal bool IsGroupExpanded(object? item)
    {
        if (item is null || !_groupKeysByItem.TryGetValue(item, out var groupKey))
        {
            return true;
        }

        return !_collapsedGroupKeys.Contains(groupKey);
    }

    /// <summary>
    /// Toggles the expand/collapse state of the group containing the given item.
    /// </summary>
    internal void ToggleGroupExpansion(object? item)
    {
        if (item is null || !_groupHeadersByItem.ContainsKey(item) || !_groupKeysByItem.TryGetValue(item, out var groupKey))
        {
            return;
        }

        if (_collapsedGroupKeys.Contains(groupKey))
        {
            _collapsedGroupKeys.Remove(groupKey);
        }
        else
        {
            _collapsedGroupKeys.Add(groupKey);
        }

        RebuildDisplayedItems();
    }

    private object GetNormalizedGroupKeyForItem(object item)
    {
        if (_groupKeysByItem.TryGetValue(item, out var groupKey))
        {
            return groupKey;
        }

        return NormalizeGroupKey(ResolvePropertyPathValue(item, GroupByPath!));
    }

    private static object NormalizeGroupKey(object? groupKey)
    {
        return groupKey ?? NullGroupKey;
    }

    /// <summary>
    /// Finds the next row index that is not a group header, scanning in the given direction.
    /// </summary>
    private int GetNextSelectableRowIndex(int startIndex, int step)
    {
        if (Items.Count == 0)
        {
            return -1;
        }

        step = step == 0 ? 1 : Math.Sign(step);
        var index = Math.Clamp(startIndex, 0, Items.Count - 1);

        for (var count = 0; count < Items.Count; count++)
        {
            if (IsSelectableItem(Items[index]))
            {
                return index;
            }

            index += step;

            if (index < 0)
            {
                index = Items.Count - 1;
            }
            else if (index >= Items.Count)
            {
                index = 0;
            }
        }

        return -1;
    }

    /// <summary>
    /// Clears all grouping state. Called when ItemsSource changes.
    /// </summary>
    private void ClearGroupingState()
    {
        _groupHeadersByItem.Clear();
        _groupKeysByItem.Clear();
        _groupHeaderItemsByKey.Clear();
        _displayItems.Clear();
    }
}
