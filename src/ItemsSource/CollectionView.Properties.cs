using Microsoft.UI.Xaml.Data;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using WinUI.TableView.Collections;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

partial class CollectionView
{
    /// <summary>
    /// Gets or sets the source collection.
    /// </summary>
    public IEnumerable Source
    {
        get;
        set
        {
            if (field == value) return;

            DetachCollectionChangedHandlers(field);
            DetachPropertyChangedHandlers(field);
            OnPropertyChanging();
            field = value;

            AttachCollectionChangedHandlers(field);
            AttachPropertyChangedHandlers(field);

            CreateItemsCopy(field);

            HandleSourceChanged();
            OnPropertyChanged();
        }
    } = new List<object>();

    /// <summary>
    /// Gets a value indicating whether this CollectionView can filter its items.
    /// </summary>
    public bool CanFilter => FilterDescriptions.Count > 0;

    /// <summary>
    /// Gets the collection of filter descriptions.
    /// </summary>
    public IList<FilterDescription> FilterDescriptions => _filterDescriptions;

    /// <summary>
    /// Gets the collection of sort descriptions.
    /// </summary>
    public IList<SortDescription> SortDescriptions => _sortDescriptions;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to get or set.</param>
    /// <returns>The item at the specified index.</returns>
    public object? this[int index]
    {
        get => _view[index];
        set => _view[index] = value;
    }

#if WINDOWS
    /// <summary>
    /// Gets the collection of group descriptions.
    /// </summary>
    public IList<GroupDescription> GroupDescriptions => _groupDescriptions;

    /// <summary>
    /// Gets or sets whether a group starts out expanded or collapsed by default, unless it's been explicitly
    /// toggled away from that. Changing this re-applies it to every group that hasn't been explicitly toggled.
    /// </summary>
    public TableViewGroupState DefaultGroupState
    {
        get;
        set
        {
            if (field == value) return;

            field = value;

            if (GroupDescriptions.Count > 0)
            {
                HandleGroupChanged();
            }
        }
    } = TableViewGroupState.Collapsed;

    /// <summary>
    /// Gets the collection groups.
    /// </summary>
    public IObservableVector<object>? CollectionGroups
    {
        get;
        set
        {
            if (field == value) return;
            OnPropertyChanging();
            field = value;
            OnPropertyChanged();
        }
    }
#else
    public IObservableVector<object?>? CollectionGroups { get; } = null;
#endif

    /// <summary>
    /// Gets or sets the current item in the view.
    /// </summary>
    public object? CurrentItem
    {
        get => CurrentPosition > -1 && CurrentPosition < _view.Count ? _view[CurrentPosition] : null!;
        set => MoveCurrentTo(value);
    }

    /// <summary>
    /// Gets the current position of the item in the view.
    /// </summary>
    public int CurrentPosition { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are more items to load.
    /// </summary>
    public bool HasMoreItems => (Source as ISupportIncrementalLoading)?.HasMoreItems ?? false;

    /// <summary>
    /// Gets a value indicating whether the current item is after the last item in the view.
    /// </summary>
    public bool IsCurrentAfterLast => CurrentPosition >= _view.Count;

    /// <summary>
    /// Gets a value indicating whether the current item is before the first item in the view.
    /// </summary>
    public bool IsCurrentBeforeFirst => CurrentPosition < 0;

    /// <summary>
    /// Gets the number of items in the view.
    /// </summary>
    public int Count => _view.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => Source == null || Source.IsReadOnly();

    /// <summary>
    /// Gets or sets a value indicating whether live shaping is enabled.
    /// </summary>
    public bool AllowLiveShaping
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            if (field)
                AttachPropertyChangedHandlers(Source);
            else
                DetachPropertyChangedHandlers(Source);
        }
    }
}
