using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI.TableView.Collections;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// A collection view implementation that supports filtering, sorting, and incremental loading.
/// </summary>
internal partial class CollectionView : ICollectionView, ISupportIncrementalLoading, INotifyPropertyChanging, INotifyPropertyChanged, IComparer<object?>
{
    private const int MaxIncrementalSyncChanges = 100;
    private bool _isSourceDetached;
    private List<(object Item, PropertyChangedEventArgs Args)>? _deferredItemChanges;
    private List<object?[]>? _detachedGroupKeys; // The grouping keys of the items when the source was detached.
    private object[] _itemsCopy = []; // In case the source is ICollection, keep a copy of the items to keep track of removed items.
    private readonly List<object?> _view = [];
    // The full filtered+sorted set, in source order, regardless of grouping. _view is pruned down to just
    // the items currently visible under grouping (every leaf-level, expanded group's items, flattened in
    // group order) - the two only ever match when nothing is grouped.
    private readonly List<object?> _groupingSourceItems = [];
    private readonly ObservableCollection<FilterDescription> _filterDescriptions = [];
    private readonly ObservableCollection<SortDescription> _sortDescriptions = [];
    private readonly ObservableCollection<GroupDescription> _groupDescriptions = [];
    private readonly Dictionary<GroupDescription, Dictionary<object[], bool>> _groupExpandedStates = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionView"/> class.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <param name="liveShapingEnabled">Indicates whether live shaping is enabled.</param>
    public CollectionView(IEnumerable? source = null, bool liveShapingEnabled = true)
    {
        _filterDescriptions.CollectionChanged += OnFilterDescriptionsCollectionChanged;
        _sortDescriptions.CollectionChanged += OnSortDescriptionsCollectionChanged;
        _groupDescriptions.CollectionChanged += OnGroupDescriptionsCollectionChanged;

        AllowLiveShaping = liveShapingEnabled;
        Source = source ?? new List<object>();
    }

    /// <summary>
    /// Handles changes to the filter descriptions collection.
    /// </summary>
    private void OnFilterDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_deferCounter > 0) return;

        if (e.Action == NotifyCollectionChangedAction.Reset)
            HandleSourceChanged();
        else
            HandleFilterChanged();
    }

    /// <summary>
    /// Handles changes to the sort descriptions collection.
    /// </summary>
    private void OnSortDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_deferCounter > 0) return;

        if (e.Action == NotifyCollectionChangedAction.Reset)
            HandleSourceChanged();
        else
            HandleSortChanged();
    }

    /// <summary>
    /// Handles changes to the group descriptions collection. Also drops any recorded expanded/collapsed
    /// overrides for a description once it's removed - it no longer groups anything, and re-adding the same
    /// column later creates a brand new <see cref="GroupDescription"/> instance that should start fresh.
    /// </summary>
    private void OnGroupDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionGroups = GroupDescriptions.Count > 0 ? new ObservableVector<object>() : null;

        if (_deferCounter > 0) return;

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _groupExpandedStates.Clear();
            HandleSourceChanged();
        }
        else
        {
            if (e.OldItems is not null)
            {
                foreach (var removed in e.OldItems.Cast<GroupDescription>())
                {
                    _groupExpandedStates.Remove(removed);
                }
            }

            HandleGroupChanged();
        }
    }

    /// <summary>
    /// Attaches collection changed handlers to the source collection.
    /// </summary>
    private void AttachCollectionChangedHandlers(IEnumerable source)
    {
        if (source is INotifyCollectionChanged sourceNcc)
        {
            sourceNcc.CollectionChanged += OnSourceCollectionChanged;
        }
        else if (source is ICollectionView sourceCV)
        {
            sourceCV.VectorChanged += OnSourceVectorChanged;
        }
    }

    /// <summary>
    /// Detaches collection changed handlers from the source collection.
    /// </summary>
    private void DetachCollectionChangedHandlers(IEnumerable source)
    {
        if (source is INotifyCollectionChanged sourceNcc)
        {
            sourceNcc.CollectionChanged -= OnSourceCollectionChanged;
        }
        else if (source is ICollectionView sourceCV)
        {
            sourceCV.VectorChanged -= OnSourceVectorChanged;
        }
    }

    /// <summary>
    /// Stops listening to the source collection and its items while keeping the current view, so that
    /// the source no longer keeps this view (and its owner) alive or drives it.
    /// </summary>
    internal void DetachFromSource()
    {
        if (_isSourceDetached) return;

        DetachCollectionChangedHandlers(Source);
        DetachPropertyChangedHandlers(Source);
        _isSourceDetached = true;
        _detachedGroupKeys = GetGroupKeys();
    }

    /// <summary>
    /// Starts listening to the current source collection after <see cref="DetachFromSource"/>. Unless
    /// <paramref name="alwaysReset"/> is set, the view is left alone if the source still produces the items
    /// the view holds, and a small difference is applied item by item, which keeps the selection of the
    /// other items. Only otherwise is the view reset.
    /// </summary>
    /// <param name="alwaysReset">Resets the view even if its items are unchanged, e.g. for a new source.</param>
    /// <returns><see langword="true"/> if the view has been reset.</returns>
    internal bool AttachToSource(bool alwaysReset = false)
    {
        if (!_isSourceDetached) return false;

        _isSourceDetached = false;
        AttachCollectionChangedHandlers(Source);
        AttachPropertyChangedHandlers(Source);
        CreateItemsCopy(Source);

        var detachedGroupKeys = _detachedGroupKeys;
        _detachedGroupKeys = null;

        if (!alwaysReset)
        {
            var items = GetFilteredSortedItems();

            if (_groupingSourceItems.SequenceEqual(items, ItemIdentityComparer.Instance))
            {
                // The same items in the same order can still belong to other groups than before.
                if (HaveSameGroupKeys(detachedGroupKeys, GetGroupKeys()))
                {
                    return false;
                }
            }
            else if (TrySyncViewIncrementally(items))
            {
                return false;
            }
        }

        HandleSourceChanged();
        return true;
    }

    /// <summary>
    /// Turns the view into <paramref name="items"/> by removing and inserting single items instead of
    /// resetting it. Not done for a grouped view, for duplicate items or for too many changes.
    /// </summary>
    /// <returns><see langword="true"/> if the view now holds <paramref name="items"/>.</returns>
    private bool TrySyncViewIncrementally(List<object?> items)
    {
        if (GroupDescriptions.Count > 0) return false;

        var comparer = ItemIdentityComparer.Instance;
        var oldIndexes = new Dictionary<object, int>(comparer!);

        for (var index = 0; index < _view.Count; index++)
        {
            if (_view[index] is not { } item || !oldIndexes.TryAdd(item, index)) return false;
        }

        // The items that stay where they are: the ones found in the same relative order in both lists.
        var kept = new HashSet<object>(comparer!);
        var nextOldIndex = 0;

        foreach (var item in items)
        {
            if (item is null) return false;

            if (oldIndexes.TryGetValue(item, out var oldIndex) && oldIndex >= nextOldIndex)
            {
                if (!kept.Add(item)) return false;
                nextOldIndex = oldIndex + 1;
            }
        }

        if (_view.Count - kept.Count + items.Count - kept.Count > MaxIncrementalSyncChanges) return false;

        for (var index = _view.Count - 1; index >= 0; index--)
        {
            if (!kept.Contains(_view[index]!))
            {
                RemoveFromView(index, _view[index]);
            }
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (index < _view.Count && comparer.Equals(_view[index], items[index])) continue;

            _groupingSourceItems.Insert(index, items[index]);
            _view.Insert(index, items[index]);

            if (index <= CurrentPosition)
            {
                CurrentPosition++;
            }

            OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted, index, items[index]));
        }

        return true;
    }

    /// <summary>
    /// Gets the grouping keys of every filtered and sorted item, or null if the view is not grouped.
    /// </summary>
    private List<object?[]>? GetGroupKeys()
    {
        if (GroupDescriptions.Count == 0) return null;

        return [.. _groupingSourceItems.Select(item => GroupDescriptions.Select(description => description.GetPropertyValue(item)).ToArray())];
    }

    private static bool HaveSameGroupKeys(List<object?[]>? x, List<object?[]>? y)
    {
        if (x is null || y is null) return x is null && y is null;

        return x.Count == y.Count && x.Zip(y).All(keys => keys.First.SequenceEqual(keys.Second));
    }

    /// <summary>
    /// Compares items the way the view identifies them: reference types by reference, and value types,
    /// which are boxed anew every time the source is enumerated, by value.
    /// </summary>
    private sealed class ItemIdentityComparer : IEqualityComparer<object?>
    {
        public static readonly ItemIdentityComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y) || (x is ValueType && x.Equals(y));
        }

        public int GetHashCode(object? obj)
        {
            return obj is ValueType ? obj.GetHashCode() : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Attaches property changed handlers to the items in the collection.
    /// </summary>
    /// <param name="items">The items to attach handlers to.</param>
    private void AttachPropertyChangedHandlers(IEnumerable? items)
    {
        // Nothing is subscribed while detached from the source; AttachToSource subscribes to all of its items.
        if (!AllowLiveShaping || _isSourceDetached || items is null) return;

        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// Detaches property changed handlers from the items in the collection.
    /// </summary>
    /// <param name="items">The items to detach handlers from.</param>
    private void DetachPropertyChangedHandlers(IEnumerable? items)
    {
        if (items is null) return;

        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// Handles changes to the source vector.
    /// </summary>
    private void OnSourceVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        var index = (int)args.Index;

        switch (args.CollectionChange)
        {
            case CollectionChange.ItemInserted:
                if (_deferCounter <= 0)
                {
                    if (index < Count)
                    {
                        var item = sender[index];
                        AttachPropertyChangedHandlers(new object[] { item });
                        HandleItemAdded(index, item);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case CollectionChange.ItemRemoved:
                if (_deferCounter <= 0)
                {
                    if (index < _itemsCopy.Length)
                    {
                        var item = _itemsCopy[index];
                        DetachPropertyChangedHandlers(new object[] { item });
                        HandleItemRemoved(index, item);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case CollectionChange.ItemChanged:
            case CollectionChange.Reset:
                if (_deferCounter <= 0)
                {
                    HandleSourceChanged();
                }

                DetachPropertyChangedHandlers(_itemsCopy);
                AttachPropertyChangedHandlers(Source);

                break;
        }

        CreateItemsCopy(Source);
    }

    /// <summary>
    /// Creates a copy of the items from the collection if it implements ICollectionView.
    /// </summary>
    private void CreateItemsCopy(IEnumerable source)
    {
        if (source is ICollectionView collectionView)
        {
            _itemsCopy = new object[collectionView.Count];
            collectionView.CopyTo(_itemsCopy, 0);
        }
    }

    /// <summary>
    /// Handles changes to the source collection.
    /// </summary>
    private void OnSourceCollectionChanged(object? arg1, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AttachPropertyChangedHandlers(e.NewItems);
                if (_deferCounter <= 0)
                {
                    if (e.NewItems?.Count == 1)
                    {
                        HandleItemAdded(e.NewStartingIndex, e.NewItems[0]);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                DetachPropertyChangedHandlers(e.OldItems);
                if (_deferCounter <= 0)
                {
                    if (e.OldItems?.Count == 1)
                    {
                        HandleItemRemoved(e.OldStartingIndex, e.OldItems[0]);
                    }
                    else
                    {
                        HandleSourceChanged();
                    }
                }

                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                if (_deferCounter <= 0)
                {
                    HandleSourceChanged();
                }

                DetachPropertyChangedHandlers(e.OldItems);
                AttachPropertyChangedHandlers(Source);

                break;
        }
    }

    /// <summary>
    /// Handles property changed events for items in the collection.
    /// </summary>
    private void OnItemPropertyChanged(object? item, PropertyChangedEventArgs e)
    {
        ItemPropertyChanged?.Invoke(item, e);

        if (!AllowLiveShaping || item is null || string.IsNullOrEmpty(e.PropertyName))
        {
            return;
        }

        if (_deferredItemChanges is not null)
        {
            _deferredItemChanges.Add((item, e));
            return;
        }

        ApplyItemChange(item, e);
    }

    /// <summary>
    /// Holds back the view's re-sorting and re-filtering of items whose properties change, until
    /// <see cref="ApplyDeferredItemChanges"/>. For changes made where the view must not move, such as in the
    /// middle of a layout pass.
    /// </summary>
    internal void DeferItemChanges()
    {
        _deferredItemChanges ??= [];
    }

    /// <summary>
    /// Applies the item changes held back since <see cref="DeferItemChanges"/>.
    /// </summary>
    internal void ApplyDeferredItemChanges()
    {
        var changes = _deferredItemChanges;
        _deferredItemChanges = null;

        if (changes is null)
        {
            return;
        }

        foreach (var (item, args) in changes)
        {
            ApplyItemChange(item, args);
        }
    }

    /// <summary>
    /// Moves an item to where its changed property puts it under the current filters and sorting.
    /// </summary>
    private void ApplyItemChange(object item, PropertyChangedEventArgs e)
    {
        if (FilterDescriptions.Any(fd => string.IsNullOrEmpty(fd.PropertyName) || fd.PropertyName == e.PropertyName))
        {
            var filterResult = FilterDescriptions.All(x => x.Predicate(item));
            var sourceIndex = _groupingSourceItems.IndexOf(item);

            if (sourceIndex != -1 && !filterResult)
            {
                RemoveFromView(sourceIndex, item);
            }
            else if (sourceIndex == -1 && filterResult)
            {
                var index = Source.IndexOf(item);

                if (index >= 0)
                {
                    HandleItemAdded(index, item);
                }
            }
        }

        if (SortDescriptions.Any(sd => string.IsNullOrEmpty(sd.PropertyName) || sd.PropertyName == e.PropertyName))
        {
            var oldIndex = _groupingSourceItems.IndexOf(item);

            // Check if item is in the source set:
            if (oldIndex < 0)
            {
                return;
            }

            _groupingSourceItems.RemoveAt(oldIndex);
            var targetIndex = _groupingSourceItems.BinarySearch(item, this);
            if (targetIndex < 0)
            {
                targetIndex = ~targetIndex;
            }

            _groupingSourceItems.Insert(targetIndex, item);

            if (GroupDescriptions.Count > 0)
            {
                HandleGroupChanged();
                return;
            }

            // Only trigger expensive UI updates if the index really changed:
            if (targetIndex != oldIndex)
            {
                _view.RemoveAt(oldIndex);
                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemRemoved, oldIndex, item));

                _view.Insert(targetIndex, item);
                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted, targetIndex, item));
            }
        }
        else if (string.IsNullOrEmpty(e.PropertyName))
        {
            HandleSourceChanged();
        }
    }

    /// <summary>
    /// Handles changes to the source collection.
    /// </summary>
    private void HandleSourceChanged()
    {
        var currentItem = CurrentItem;
        var items = GetFilteredSortedItems();
        _groupingSourceItems.Clear();
        _groupingSourceItems.AddRange(items);

        if (GroupDescriptions.Count > 0)
        {
            CreateGroupCollections();
        }
        else
        {
            RebuildFlatView();
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
        MoveCurrentTo(currentItem);
    }

    /// <summary>
    /// Gets the items of the source with the current filters and sorting applied.
    /// </summary>
    private List<object?> GetFilteredSortedItems()
    {
        var items = new List<object?>();

        if (Source is not null)
        {
            if (FilterDescriptions.Count > 0)
            {
                foreach (var item in Source)
                {
                    if (FilterDescriptions.All(x => x.Predicate(item)))
                        items.Add(item);
                }
            }
            else
            {
                items.AddRange(Source.OfType<object>());
            }

            if (SortDescriptions.Count > 0)
                items.Sort(this);
        }

        return items;
    }

    /// <summary>
    /// Copies <see cref="_groupingSourceItems"/> into <see cref="_view"/> as-is - the ungrouped case, where
    /// nothing is pruned.
    /// </summary>
    private void RebuildFlatView()
    {
        OnViewResetting();
        _view.Clear();
        _view.AddRange(_groupingSourceItems);
    }


    /// <summary>
    /// Rebuilds <see cref="CollectionGroups"/> - and, alongside it, <see cref="_view"/> pruned down to just
    /// what's currently visible - from <see cref="_groupingSourceItems"/>, the full filtered+sorted set.
    /// </summary>
    private void CreateGroupCollections()
    {
        OnViewResetting();
        CollectionGroups?.Clear();
        _view.Clear();

        if (GroupDescriptions.Count == 0)
        {
            _view.AddRange(_groupingSourceItems);
            return;
        }

        BuildGroupLevel(_groupingSourceItems, level: 0, parentPath: []);
    }

    /// <summary>
    /// Groups <paramref name="items"/> by the <see cref="GroupDescription"/> at <paramref name="level"/>,
    /// adding a flattened <see cref="CollectionViewGroup"/> entry (in depth-first order) for every group at
    /// every level, and recursing into deeper levels until the last <see cref="GroupDescription"/> is reached.
    /// A collapsed group still gets its own header entry, but its descendant subgroups are not built at all,
    /// and none of its items are appended to <see cref="_view"/> - only a leaf-level, expanded group's items
    /// are, in group order, so <see cref="_view"/> ends up holding exactly what's currently displayed.
    /// </summary>
    private void BuildGroupLevel(IEnumerable<object?> items, int level, object[] parentPath)
    {
        var description = GroupDescriptions[level];
        var isLeafLevel = level == GroupDescriptions.Count - 1;
        var comparer = Comparer<object?>.Create(description.Compare);

        // Materialized up front (key + item list): GroupSortMode.Count needs each group's size to decide
        // the order, which only exists once every group's items are known - Key mode materializes here too
        // so the ordering and the loop below don't have to special-case IGrouping vs. a plain list.
        var materialized = items.GroupBy(description.GetPropertyValue)
            .Select(g => (g.Key, Items: g.ToList()))
            .ToList();

        var ordered = description.SortMode switch
        {
            // Ties (equal counts) always break ascending by key, regardless of Direction - a deterministic
            // fallback, not a user-facing direction concept. Unlike key mode, where GroupBy guarantees no
            // ties, count ties are common (many groups can share a size).
            GroupSortMode.Count => description.Direction == SortDirection.Ascending
                ? materialized.OrderBy(g => g.Items.Count).ThenBy(g => g.Key, comparer)
                : materialized.OrderByDescending(g => g.Items.Count).ThenBy(g => g.Key, comparer),
            _ => description.Direction == SortDirection.Ascending
                ? materialized.OrderBy(g => g.Key, comparer)
                : materialized.OrderByDescending(g => g.Key, comparer)
        };

        var overridesForDescription = _groupExpandedStates.TryGetValue(description, out var existing) ? existing : null;

        foreach (var (key, groupedItems) in ordered)
        {
            object[] groupPath = [.. parentPath, key!];
            var isExpanded = overridesForDescription is not null && overridesForDescription.TryGetValue(groupPath, out var overridden)
                ? overridden
                : DefaultGroupState == TableViewGroupState.Expanded;
            // IsExpanded must be set before CollectionView: its DependencyProperty changed callback calls back
            // into CollectionView.OnGroupExpandedChanged (which triggers a full group rebuild), and setting it
            // here is just restoring already-computed state, not a real toggle - assigning CollectionView first
            // would make that callback fire mid-rebuild and recurse into BuildGroupLevel until the stack overflows.
            var groupInfo = new TableViewGroupInfo
            {
                Key = key,
                Level = level,
                GroupPath = groupPath,
                Description = description,
                Count = groupedItems.Count,
                IsExpanded = isExpanded,
                CollectionView = this,
            };

            CollectionGroups?.Add(new CollectionViewGroup
            {
                GroupInfo = groupInfo,
                GroupItems = new ObservableVector<object?>(isLeafLevel && isExpanded ? groupedItems : [])
            });

            if (isLeafLevel)
            {
                if (isExpanded)
                {
                    _view.AddRange(groupedItems);
                }

                continue;
            }

            if (isExpanded)
            {
                BuildGroupLevel(groupedItems, level + 1, groupPath);
            }
        }
    }

    /// <summary>
    /// Persists a group's expanded/collapsed state (across the fresh <see cref="TableViewGroupInfo"/> instances
    /// created on every rebuild) and refreshes the grouped view to reflect it. Only recorded when it differs from
    /// <see cref="DefaultGroupState"/> - toggling a group back to match the current default clears its override.
    /// Scoped under the group's own <see cref="GroupDescription"/> so it's dropped entirely once that
    /// description is removed (see <see cref="OnGroupDescriptionsCollectionChanged"/>).
    /// </summary>
    internal void OnGroupExpandedChanged(TableViewGroupInfo info)
    {
        if (info.Description is not { } description) return;

        if (info.IsExpanded == (DefaultGroupState == TableViewGroupState.Expanded))
        {
            if (_groupExpandedStates.TryGetValue(description, out var overrides))
            {
                overrides.Remove(info.GroupPath);
            }
        }
        else
        {
            if (!_groupExpandedStates.TryGetValue(description, out var overrides))
            {
                overrides = new Dictionary<object[], bool>(ObjectArrayComparer.Instance);
                _groupExpandedStates[description] = overrides;
            }

            overrides[info.GroupPath] = info.IsExpanded;
        }

        HandleGroupChanged();
    }

    sealed class ObjectArrayComparer : IEqualityComparer<object[]>
    {
        public static readonly ObjectArrayComparer Instance = new();

        public bool Equals(object[]? x, object[]? y)
        {
            return x is not null && y is not null && x.SequenceEqual(y);
        }

        public int GetHashCode(object[] obj)
        {
            return obj.Aggregate(0, (hash, item) => HashCode.Combine(hash, item));
        }
    }

    /// <summary>
    /// Handles changes to the group descriptions or an individual group's expanded state.
    /// </summary>
    private void HandleGroupChanged()
    {
        var currentItem = CurrentItem;

        CreateGroupCollections();

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
        MoveCurrentTo(currentItem);
    }

    /// <summary>
    /// Handles changes to the filter descriptions.
    /// </summary>
    private void HandleFilterChanged()
    {
        if (FilterDescriptions.Count > 0)
        {
            for (var index = 0; index < _groupingSourceItems.Count; index++)
            {
                var item = _groupingSourceItems.ElementAt(index);
                if (FilterDescriptions.All(x => x.Predicate(item)))
                {
                    continue;
                }

                RemoveFromView(index, item);
                index--;
            }
        }

        var sourceHash = new HashSet<object?>(_groupingSourceItems);
        var sourceIndex = 0;
        var i = 0;
        foreach (var item in Source)
        {
            if (sourceHash.Contains(item))
            {
                sourceIndex++;
                continue;
            }

            if (HandleItemAdded(i, item, sourceIndex))
            {
                sourceIndex++;
            }

            i++;
        }
    }

    /// <summary>
    /// Handles changes to the sort descriptions.
    /// </summary>
    private void HandleSortChanged()
    {
        if (SortDescriptions.Count > 0)
        {
            _groupingSourceItems.Sort(this);

            if (GroupDescriptions.Count > 0)
            {
                HandleGroupChanged();
                return;
            }

            RebuildFlatView();
        }
        else
        {
            HandleSourceChanged();
            return;
        }

        OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset));
    }

    /// <summary>
    /// Handles the addition of an item to the collection.
    /// </summary>
    private bool HandleItemAdded(int newStartingIndex, object? newItem, int? sourceItemsIndex = null)
    {
        if (!FilterDescriptions.All(x => x.Predicate(newItem)))
        {
            return false;
        }

        var newIndex = newStartingIndex;

        if (_sortDescriptions.Any())
        {
            newIndex = _groupingSourceItems.BinarySearch(newItem!, this);
            if (newIndex < 0)
            {
                newIndex = ~newIndex;
            }
        }
        else if (FilterDescriptions.Any())
        {
            if (Source == null)
            {
                HandleSourceChanged();
                return false;
            }
            newIndex = sourceItemsIndex ?? _groupingSourceItems.Take(newStartingIndex).Count();
        }

        _groupingSourceItems.Insert(newIndex, newItem!);

        if (GroupDescriptions.Count > 0)
        {
            HandleGroupChanged();
            return true;
        }

        _view.Insert(newIndex, newItem!);
        if (newIndex <= CurrentPosition)
        {
            CurrentPosition++;
        }

        var e = new VectorChangedEventArgs(CollectionChange.ItemInserted, newIndex, newItem);
        OnVectorChanged(e);

        return true;
    }

    /// <summary>
    /// Handles the removal of an item from the collection.
    /// </summary>
    private void HandleItemRemoved(int oldStartingIndex, object? oldItem)
    {
        if (FilterDescriptions != null && !FilterDescriptions.All(x => x.Predicate(oldItem)))
        {
            return;
        }

        if (oldStartingIndex < 0 || oldStartingIndex >= _groupingSourceItems.Count || !Equals(_groupingSourceItems[oldStartingIndex], oldItem))
        {
            oldStartingIndex = _groupingSourceItems.IndexOf(oldItem!);
        }

        if (oldStartingIndex < 0)
        {
            return;
        }

        RemoveFromView(oldStartingIndex, oldItem);
    }

    /// <summary>
    /// Removes an item from <see cref="_groupingSourceItems"/> (identified by its index there) and, if it's
    /// currently visible, from <see cref="_view"/> too.
    /// </summary>
    private void RemoveFromView(int itemIndex, object? item)
    {
        _groupingSourceItems.RemoveAt(itemIndex);

        if (GroupDescriptions.Count > 0)
        {
            HandleGroupChanged();
            return;
        }

        _view.RemoveAt(itemIndex);

        if (itemIndex <= CurrentPosition)
        {
            CurrentPosition--;
        }

        var e = new VectorChangedEventArgs(CollectionChange.ItemRemoved, itemIndex, item);
        OnVectorChanged(e);
    }

    /// <summary>
    /// Moves the current item to the specified index.
    /// </summary>
    private bool MoveCurrentToIndex(int i)
    {
        if (i < -1 || i >= _view.Count || i == CurrentPosition) return false;

        var e = new CurrentChangingEventArgs();
        OnCurrentChanging(e);

        if (e.Cancel)
        {
            return false;
        }

        CurrentPosition = i;
        OnCurrentChanged();

        return true;
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(object item)
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        Source.Add(item);
    }

    /// <summary>
    /// Clears the collection.
    /// </summary>
    public void Clear()
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        Source.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>true if the item is found in the collection; otherwise, false.</returns>
    public bool Contains(object item)
    {
        return _view.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(object[] array, int arrayIndex)
    {
        _view.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>The index of the item if found in the collection; otherwise, -1.</returns>
    public int IndexOf(object? item)
    {
        return _view.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item to the collection at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, object item)
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        Source.Insert(index, item);
    }

    /// <summary>
    /// Moves the current item to the specified item.
    /// </summary>
    /// <param name="item">The item to move to.</param>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentTo(object? item)
    {
        return item == CurrentItem || MoveCurrentToIndex(IndexOf(item));
    }

    /// <summary>
    /// Moves the current item to the first item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToFirst()
    {
        return MoveCurrentToIndex(0);
    }

    /// <summary>
    /// Moves the current item to the last item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToLast()
    {
        return MoveCurrentToIndex(_view.Count - 1);
    }

    /// <summary>
    /// Moves the current item to the next item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToNext()
    {
        return MoveCurrentToIndex(CurrentPosition + 1);
    }

    /// <summary>
    /// Moves the current item to the specified position.
    /// </summary>
    /// <param name="index">The zero-based index to move to.</param>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToPosition(int index)
    {
        return MoveCurrentToIndex(index);
    }

    /// <summary>
    /// Moves the current item to the previous item in the collection.
    /// </summary>
    /// <returns>true if the operation is successful; otherwise, false.</returns>
    public bool MoveCurrentToPrevious()
    {
        return MoveCurrentToIndex(CurrentPosition - 1);
    }

    /// <summary>
    /// Removes the first occurrence of a specific item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    public bool Remove(object? item)
    {
        if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");

        Source.Remove(item);

        return true;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        Remove(_view[index]);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<object> GetEnumerator()
    {
        return _view.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _view.GetEnumerator();
    }

    /// <summary>
    /// Loads more items asynchronously.
    /// </summary>
    /// <param name="count">The number of items to load.</param>
    /// <returns>An asynchronous operation that returns the result of the load operation.</returns>
    public IAsyncOperation<LoadMoreItemsResult>? LoadMoreItemsAsync(uint count)
    {
        return (Source as ISupportIncrementalLoading)?.LoadMoreItemsAsync(count);
    }

    /// <summary>
    /// Compares two objects based on the sort descriptions.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>An integer that indicates the relative order of the objects being compared.</returns>
    public int Compare(object? x, object? y)
    {
        foreach (var sortDescription in SortDescriptions)
        {
            var xValue = sortDescription.GetPropertyValue(x);
            var yValue = sortDescription.GetPropertyValue(y);
            var cmp = sortDescription.Compare(xValue, yValue);

            if (cmp != 0)
            {
                return sortDescription.Direction is SortDirection.Ascending ? +cmp : -cmp;
            }
        }

        return 0;
    }

    /// <summary>
    /// Manually refreshes the view.
    /// </summary>
    public void Refresh()
    {
        HandleSourceChanged();
    }

    /// <summary>
    /// Refreshes the filter applied to the view.
    /// </summary>
    public void RefreshFilter()
    {
        HandleFilterChanged();
    }

    /// <summary>
    /// Refreshes the sorting applied to the view.
    /// </summary>
    public void RefreshSorting()
    {
        HandleSortChanged();
    }

    /// <summary>
    /// Refreshes the grouping applied to the view.
    /// </summary>
    public void RefreshGrouping()
    {
        HandleGroupChanged();
    }
}
