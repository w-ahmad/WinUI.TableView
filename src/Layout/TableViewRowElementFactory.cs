using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;

namespace WinUI.TableView.Layout;

/// <summary>
/// Creates and recycles the <see cref="TableViewRow"/> and <see cref="TableViewGroupHeaderRow"/> elements the row
/// host displays.
/// </summary>
/// <remarks>
/// The repeater delegates recycling entirely to the factory, so this owns the pools. Rows are the expensive
/// element — each one builds a cell per visible column — which is exactly why they are pooled and reused rather
/// than recreated as the viewport moves. Group headers get a pool of their own, since a header can never be
/// reused as a data row or the other way round.
/// <para>
/// The repeater is not given this factory but <see cref="Shim"/>. A repeater holds its item template through a
/// reference the garbage collector's reference tracking does not see, so whatever it is given acts as a root for
/// as long as the repeater lives; and this factory leads back to the table, whose visual tree holds the repeater.
/// Handed over directly, it kept every table alive after it was unloaded. The shim reaches the factory weakly, and
/// the table owns the factory, so all of it is collected once the table is.
/// </para>
/// </remarks>
internal sealed partial class TableViewRowElementFactory
{
    private readonly TableView _tableView;
    private readonly Stack<TableViewRow> _rowPool = new();
    private readonly List<TableViewGroupHeaderRow> _groupHeaderPool = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowElementFactory"/> class.
    /// </summary>
    public TableViewRowElementFactory(TableView tableView)
    {
        _tableView = tableView;
        Shim = new WeakShim(this);
    }

    /// <summary>
    /// Gets the element factory to give the repeater in place of this one.
    /// </summary>
#if WINDOWS
    public IElementFactory Shim { get; }
#else
    public object Shim { get; }
#endif

    /// <summary>
    /// Returns a row for a data item, or a group header for a group, reusing a pooled one when there is one.
    /// </summary>
    public UIElement GetElement(object? data)
    {
        if (data is TableViewGroupInfo groupInfo)
        {
            var header = TakeGroupHeader(GetGroupHash(groupInfo));
            header.TableView = _tableView;
            header.ContentTemplate = _tableView.GetGroupHeaderTemplate(groupInfo, header);
            groupInfo.PublishBindableValues();
            header.Content = groupInfo;

            return header;
        }

        TableViewRow row;

        if (_rowPool.Count > 0)
        {
            row = _rowPool.Pop();
        }
        else
        {
            row = _tableView.CreateRow();
            CreatedRowCount++;
        }

        row.TableView = _tableView;

        // Assigning Content is what drives cell creation on a new row and cell refresh on a reused one.
        row.Content = data;

        return row;
    }

    /// <summary>
    /// Takes a pooled header, preferring the one that last showed the group, so a view rebuild leaves each
    /// header's pointer and focus state on its own group.
    /// </summary>
    private TableViewGroupHeaderRow TakeGroupHeader(int groupHash)
    {
        var index = _groupHeaderPool.Count - 1;

        if (index < 0)
        {
            return new TableViewGroupHeaderRow();
        }

        for (var i = index; i >= 0; i--)
        {
            if (_groupHeaderPool[i].LastGroupHash == groupHash)
            {
                index = i;
                break;
            }
        }

        var header = _groupHeaderPool[index];
        _groupHeaderPool.RemoveAt(index);

        return header;
    }

    private static int GetGroupHash(TableViewGroupInfo groupInfo)
    {
        var hash = 17;

        foreach (var key in groupInfo.GroupPath)
        {
            hash = (hash * 31) + (key?.GetHashCode() ?? 0);
        }

        return hash;
    }

    /// <summary>
    /// Gets the number of rows the factory has created, as opposed to taken from its pool.
    /// </summary>
    internal int CreatedRowCount { get; private set; }

    /// <summary>
    /// Gets the rows waiting to be reused.
    /// </summary>
    internal IReadOnlyCollection<TableViewRow> PooledRows => _rowPool;

    /// <summary>
    /// Lets pooled rows drop the items they last showed, so those items are not kept alive.
    /// </summary>
    /// <remarks>
    /// Must not run inside a repeater callback: clearing a row's content rebuilds its cells.
    /// </remarks>
    internal void ReleasePooledContent()
    {
        foreach (var row in _rowPool)
        {
            row.Content = null;
        }
    }

    /// <summary>
    /// Takes back a row or group header the repeater no longer shows, pooling it for reuse.
    /// </summary>
    public void RecycleElement(UIElement? element)
    {
        switch (element)
        {
            case TableViewRow row:
                // Every row the repeater lets go of is pooled. The pool can only grow to the most rows the table has
                // shown at once, and a row left out of it would stay one of the repeater's children, still listening
                // to the table's columns, without ever being shown again.
                row.PrepareForRecycle();
                _rowPool.Push(row);

                break;

            case TableViewGroupHeaderRow header:
                // Drop the group so a pooled header does not keep a stale TableViewGroupInfo (and through it the
                // CollectionView) alive, and so its next group always arrives as a content change.
                header.LastGroupHash = header.GroupInfo is { } group ? GetGroupHash(group) : 0;
                header.Content = null;
                _groupHeaderPool.Add(header);

                break;
        }
    }

    /// <summary>
    /// The element factory the repeater holds: forwards to the table's factory without keeping it alive.
    /// </summary>
    /// <remarks>
    /// Uno's repeater only accepts a data template, a template selector or an <c>IElementFactoryShim</c> as its
    /// item template, not any <see cref="IElementFactory"/>, so on that platform the shim is only that: Uno does not
    /// implement the <see cref="IElementFactory"/> argument types. That interface has its own argument types, which
    /// is why the factory itself takes the data and the element.
    /// </remarks>
#if WINDOWS
    private sealed partial class WeakShim(TableViewRowElementFactory owner) : IElementFactory
    {
        private readonly WeakReference<TableViewRowElementFactory> _owner = new(owner);

        /// <inheritdoc/>
        public UIElement GetElement(ElementFactoryGetArgs args)
        {
            // The repeater only asks while its table is alive, so the fallback is never shown; it only keeps the
            // contract of always returning an element.
            return _owner.TryGetTarget(out var owner) ? owner.GetElement(args?.Data) : new TableViewRow();
        }

        /// <inheritdoc/>
        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            if (_owner.TryGetTarget(out var owner))
            {
                owner.RecycleElement(args?.Element);
            }
        }
    }
#else
    private sealed partial class WeakShim(TableViewRowElementFactory owner) : Microsoft.UI.Xaml.Controls.IElementFactoryShim
    {
        private readonly WeakReference<TableViewRowElementFactory> _owner = new(owner);

        /// <inheritdoc/>
        public UIElement GetElement(Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs args)
        {
            // The repeater only asks while its table is alive, so the fallback is never shown; it only keeps the
            // contract of always returning an element.
            return _owner.TryGetTarget(out var owner) ? owner.GetElement(args?.Data) : new TableViewRow();
        }

        /// <inheritdoc/>
        public void RecycleElement(Microsoft.UI.Xaml.Controls.ElementFactoryRecycleArgs args)
        {
            if (_owner.TryGetTarget(out var owner))
            {
                owner.RecycleElement(args?.Element);
            }
        }
    }
#endif
}
