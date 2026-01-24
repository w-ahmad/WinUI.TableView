using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WinUI.TableView.Collections;

/// <summary>
/// Represents a set of objects that enforces a specific element type at runtime, providing set operations over objects
/// backed by a strongly typed collection.
/// </summary>
internal partial class ObjectBackedTypedSet<T> : ICollection<object?>
{
    private readonly HashSet<T?> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectBackedTypedSet{T}"/> class.
    /// </summary>
    /// <param name="inner">The inner collection of objects.</param>
    public ObjectBackedTypedSet(IEnumerable<object?> inner)
    {
        _inner = [.. inner.Cast<T?>()];
    }

    /// <inheritdoc />
    public int Count => _inner.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(object? item)
    {
        _inner.Add((T?)item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _inner.Clear();
    }

    /// <inheritdoc />
    public bool Contains(object? item)
    {
        return _inner.Contains((T?)item);
    }

    /// <inheritdoc />
    public void CopyTo(object?[] array, int arrayIndex)
    {
        foreach (var item in _inner)
            array[arrayIndex++] = item!;
    }

    /// <inheritdoc />
    public IEnumerator<object?> GetEnumerator()
    {
        foreach (var item in _inner)
            yield return item;
    }

    /// <inheritdoc />
    public bool Remove(object? item)
    {
        return _inner.Remove((T?)item);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}