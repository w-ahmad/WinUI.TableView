using System;
using System.Collections;
using System.Collections.Generic;

namespace WinUI.TableView.Selection;

/// <summary>
/// A live, lazily projected view of the selected items backed by a <see cref="TableViewSelectionModel"/>.
/// </summary>
/// <remarks>
/// Nothing is materialised: <see cref="Count"/> is read straight off the range model and the items are resolved
/// from the item view on demand. Selecting every row of a million-row source therefore costs one range rather
/// than a million-element list, while <c>Add</c>/<c>Remove</c>/<c>Clear</c> keep working so code that mutates
/// <c>SelectedItems</c> directly behaves as it did when the collection came from <c>ListViewBase</c>.
/// </remarks>
internal sealed class TableViewSelectedItemsCollection : IList<object>
{
    private readonly TableViewSelectionModel _model;
    private readonly Func<int, object?> _itemAt;
    private readonly Func<object?, int> _indexOfItem;
    private readonly Action<int, bool> _setSelected;
    private readonly Action _clearSelection;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewSelectedItemsCollection"/> class.
    /// </summary>
    /// <param name="model">The range model holding the selected item indexes.</param>
    /// <param name="itemAt">Resolves an item view index to the item.</param>
    /// <param name="indexOfItem">Resolves an item to its item view index, or -1.</param>
    /// <param name="setSelected">Selects or deselects a single item view index, raising the owner's events.</param>
    /// <param name="clearSelection">Clears the whole selection, raising the owner's events.</param>
    public TableViewSelectedItemsCollection(TableViewSelectionModel model,
                                            Func<int, object?> itemAt,
                                            Func<object?, int> indexOfItem,
                                            Action<int, bool> setSelected,
                                            Action clearSelection)
    {
        _model = model;
        _itemAt = itemAt;
        _indexOfItem = indexOfItem;
        _setSelected = setSelected;
        _clearSelection = clearSelection;
    }

    /// <inheritdoc/>
    public int Count => _model.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public object this[int index]
    {
        get
        {
            var itemIndex = _model.IndexAt(index);

            if (itemIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _itemAt(itemIndex)!;
        }
        set => throw new NotSupportedException("Selected items cannot be replaced by index.");
    }

    /// <inheritdoc/>
    public void Add(object item)
    {
        var itemIndex = _indexOfItem(item);

        if (itemIndex >= 0)
        {
            _setSelected(itemIndex, true);
        }
    }

    /// <inheritdoc/>
    public bool Remove(object item)
    {
        var itemIndex = _indexOfItem(item);

        if (itemIndex < 0 || !_model.Contains(itemIndex))
        {
            return false;
        }

        _setSelected(itemIndex, false);

        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _clearSelection();
    }

    /// <inheritdoc/>
    public bool Contains(object item)
    {
        var itemIndex = _indexOfItem(item);

        return itemIndex >= 0 && _model.Contains(itemIndex);
    }

    /// <inheritdoc/>
    public int IndexOf(object item)
    {
        var itemIndex = _indexOfItem(item);

        return itemIndex < 0 ? -1 : _model.PositionOf(itemIndex);
    }

    /// <inheritdoc/>
    public void CopyTo(object[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    /// <inheritdoc/>
    public IEnumerator<object> GetEnumerator()
    {
        foreach (var itemIndex in _model.GetIndexes())
        {
            yield return _itemAt(itemIndex)!;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public void Insert(int index, object item) =>
        throw new NotSupportedException("Selected items cannot be inserted at a position; use Add instead.");

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        var itemIndex = _model.IndexAt(index);

        if (itemIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _setSelected(itemIndex, false);
    }
}
