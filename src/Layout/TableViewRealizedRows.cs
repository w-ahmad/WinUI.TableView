using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;

namespace WinUI.TableView.Layout;

/// <summary>
/// The rows and group headers the row host has realized, in visual order, and where each one is.
/// </summary>
/// <remarks>
/// Finds the row at a vertical position from the realized rows themselves, so hit testing and paging do not need a
/// layout that knows the height of every row. That is enough because the rows under the pointer, under a drag
/// rectangle and in the viewport are always realized. The list is rebuilt lazily after the repeater prepares, clears
/// or re-indexes an element.
/// </remarks>
internal sealed class TableViewRealizedRows
{
    private readonly Func<IEnumerable<(int VisualIndex, UIElement Element)>> _source;
    private readonly Func<UIElement, (double Top, double Height)> _bounds;
    private readonly Func<UIElement, bool> _isInFlow;
    private List<(int VisualIndex, UIElement Element)>? _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRealizedRows"/> class.
    /// </summary>
    /// <param name="source">The realized elements and their visual indexes, in any order.</param>
    /// <param name="bounds">An element's top and height in the row host's coordinates.</param>
    /// <param name="isInFlow">
    /// Whether an element sits where its visual index puts it; one drawn somewhere else does not count for offsets.
    /// </param>
    public TableViewRealizedRows(
        Func<IEnumerable<(int VisualIndex, UIElement Element)>> source,
        Func<UIElement, (double Top, double Height)>? bounds = null,
        Func<UIElement, bool>? isInFlow = null)
    {
        _source = source;
        _bounds = bounds ?? (static element => (element.ActualOffset.Y, element.ActualSize.Y));
        _isInFlow = isInFlow ?? (static _ => true);
    }

    /// <summary>
    /// Gets the number of realized elements.
    /// </summary>
    public int Count => Entries.Count;

    /// <summary>
    /// Gets the visual index of the last realized element, or -1 when none is realized.
    /// </summary>
    public int LastVisualIndex => Entries is { Count: > 0 } entries ? entries[^1].VisualIndex : -1;

    /// <summary>
    /// Gets the realized elements in visual order.
    /// </summary>
    public IEnumerable<UIElement> InOrder
    {
        get
        {
            foreach (var entry in Entries)
            {
                yield return entry.Element;
            }
        }
    }

    /// <summary>
    /// Forgets the current list, to be rebuilt on the next query.
    /// </summary>
    public void Invalidate()
    {
        _entries = null;
    }

    /// <summary>
    /// Returns the realized element at a visual index, or null when it is not realized.
    /// </summary>
    public UIElement? Get(int visualIndex)
    {
        var entries = Entries;
        var low = 0;
        var high = entries.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = entries[middle].VisualIndex;

            if (candidate == visualIndex)
            {
                return entries[middle].Element;
            }

            if (candidate < visualIndex)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the top and height of the realized element at a visual index.
    /// </summary>
    public bool TryGetBounds(int visualIndex, out double top, out double height)
    {
        if (Get(visualIndex) is { } element && _isInFlow(element))
        {
            (top, height) = _bounds(element);
            return height > 0d;
        }

        top = height = 0d;
        return false;
    }

    /// <summary>
    /// Returns the visual index of the realized element that contains a vertical position, or of the nearest one.
    /// </summary>
    /// <param name="y">The position, in the row host's coordinates.</param>
    /// <param name="exact">Whether an element contains the position, rather than being the nearest to it.</param>
    /// <returns>The visual index, or -1 when no element is realized and laid out.</returns>
    public int FindAtOffset(double y, out bool exact)
    {
        exact = false;

        var nearest = -1;
        var nearestDistance = double.PositiveInfinity;

        foreach (var (visualIndex, element) in Entries)
        {
            if (!_isInFlow(element))
            {
                continue;
            }

            var (top, height) = _bounds(element);

            // A zero height is an element the repeater keeps but does not show, such as a focused row out of view.
            if (height <= 0d)
            {
                continue;
            }

            if (y >= top && y < top + height)
            {
                exact = true;
                return visualIndex;
            }

            var distance = y < top ? top - y : y - (top + height);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = visualIndex;
            }
        }

        return nearest;
    }

    private List<(int VisualIndex, UIElement Element)> Entries => _entries ??= Build();

    private List<(int VisualIndex, UIElement Element)> Build()
    {
        var entries = new List<(int VisualIndex, UIElement Element)>();

        foreach (var entry in _source())
        {
            if (entry.VisualIndex >= 0)
            {
                entries.Add(entry);
            }
        }

        entries.Sort(static (x, y) => x.VisualIndex.CompareTo(y.VisualIndex));

        return entries;
    }
}
