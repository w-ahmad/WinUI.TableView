using System;
using System.Collections;
using System.Collections.Generic;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Selection;

/// <summary>
/// A read-only, lazily projected view of every cell slot covered by a set of disjoint cell ranges.
/// </summary>
/// <remarks>
/// Cell selection is stored as rectangles, and this projects those rectangles back to individual slots without
/// materialising them. Selecting every cell of a large grid is one rectangle, so <see cref="Count"/> is a sum
/// over the rectangles and <see cref="Contains"/> is a scan of them, rather than a set holding
/// <c>rows × columns</c> entries.
/// <para>
/// The ranges must be disjoint for <see cref="Count"/> to be exact; <see cref="TableView"/> keeps them that way
/// by subtracting a new range from the existing selection before adding it.
/// </para>
/// </remarks>
internal sealed class TableViewCellSlotCollection : IList<TableViewCellSlot>
{
    private readonly IEnumerable<TableViewCellSlotRange> _ranges;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewCellSlotCollection"/> class over a live or
    /// snapshotted set of disjoint ranges.
    /// </summary>
    public TableViewCellSlotCollection(IEnumerable<TableViewCellSlotRange> ranges)
    {
        _ranges = ranges;
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            var count = 0;

            foreach (var range in _ranges)
            {
                count += range.Length;
            }

            return count;
        }
    }

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public TableViewCellSlot this[int index]
    {
        get
        {
            if (index >= 0)
            {
                foreach (var range in _ranges)
                {
                    if (index < range.Length)
                    {
                        return new TableViewCellSlot(
                            range.FirstRow + (index / range.Columns),
                            range.FirstColumn + (index % range.Columns));
                    }

                    index -= range.Length;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public bool Contains(TableViewCellSlot slot)
    {
        foreach (var range in _ranges)
        {
            if (range.Contains(slot.Row, slot.Column))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public int IndexOf(TableViewCellSlot slot)
    {
        var offset = 0;

        foreach (var range in _ranges)
        {
            if (range.Contains(slot.Row, slot.Column))
            {
                return offset + ((slot.Row - range.FirstRow) * range.Columns) + (slot.Column - range.FirstColumn);
            }

            offset += range.Length;
        }

        return -1;
    }

    /// <inheritdoc/>
    public IEnumerator<TableViewCellSlot> GetEnumerator()
    {
        foreach (var range in _ranges)
        {
            foreach (var slot in range.GetSlots())
            {
                yield return slot;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public void CopyTo(TableViewCellSlot[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        foreach (var slot in this)
        {
            array[arrayIndex++] = slot;
        }
    }

    /// <inheritdoc/>
    public void Add(TableViewCellSlot item) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Clear() => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Insert(int index, TableViewCellSlot item) => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool Remove(TableViewCellSlot item) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void RemoveAt(int index) => throw new NotSupportedException();
}
