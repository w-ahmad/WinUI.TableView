using System.Collections;
using Windows.Foundation.Collections;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Collections;

/// <summary>
/// An observable vector implementation for use with ICollectionViewGroup.
/// </summary>
internal partial class ObservableVector<T> : IObservableVector<T>
{
    private readonly List<T> _innerItems;
    public event VectorChangedEventHandler<T>? VectorChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableVector{T}"/> class.
    /// </summary>
    public ObservableVector()
    {
        _innerItems = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableVector{T}"/> class with the specified items.
    /// </summary>
    /// <param name="items">The items to initialize the vector with.</param>
    public ObservableVector(IEnumerable<T> items)
    {
        _innerItems = [.. items];
    }

    /// <inheritdoc />
    public T this[int index]
    {
        get => _innerItems[index];
        set
        {
            _innerItems[index] = value;
            VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemChanged, index));
        }
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        _innerItems.Add(item);
        VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemInserted, _innerItems.Count - 1));
    }

    /// <inheritdoc />
    public void Clear()
    {
        _innerItems.Clear();
        VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.Reset, 0));

    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        return _innerItems.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        _innerItems.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        return _innerItems.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        _innerItems.Insert(index, item);
        VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemInserted, index));
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        var index = _innerItems.IndexOf(item);
        if (index < 0) return false;
        _innerItems.RemoveAt(index);
        VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemRemoved, index));
        return true;
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        _innerItems.RemoveAt(index);
        VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemRemoved, index));
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        return _innerItems.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int BinarySearch(T item, IComparer<T>? comparer)
    {
        return _innerItems.BinarySearch(item, comparer);
    }

    /// <inheritdoc />
    public int Count => _innerItems.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _innerItems.IsReadOnly();
}