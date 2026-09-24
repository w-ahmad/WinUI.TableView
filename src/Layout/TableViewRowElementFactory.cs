using Microsoft.UI.Xaml;
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
/// </remarks>
internal sealed class TableViewRowElementFactory : IElementFactory
{
    private const int MaxPooledElements = 64;

    private readonly TableView _tableView;
    private readonly Stack<TableViewRow> _rowPool = new();
    private readonly Stack<TableViewGroupHeaderRow> _groupHeaderPool = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowElementFactory"/> class.
    /// </summary>
    public TableViewRowElementFactory(TableView tableView)
    {
        _tableView = tableView;
    }

    /// <inheritdoc/>
    public UIElement GetElement(ElementFactoryGetArgs args)
    {
        if (args.Data is TableViewGroupInfo groupInfo)
        {
            var header = _groupHeaderPool.Count > 0 ? _groupHeaderPool.Pop() : new TableViewGroupHeaderRow();
            header.TableView = _tableView;
            header.ContentTemplate = _tableView.GetGroupHeaderTemplate(groupInfo, header);
            header.Content = groupInfo;

            return header;
        }

        var row = _rowPool.Count > 0 ? _rowPool.Pop() : _tableView.CreateRow();
        row.TableView = _tableView;

        // Assigning Content is what drives cell creation on a new row and cell refresh on a reused one.
        row.Content = args.Data;

        return row;
    }

    /// <inheritdoc/>
    public void RecycleElement(ElementFactoryRecycleArgs args)
    {
        switch (args.Element)
        {
            case TableViewRow row:
                row.PrepareForRecycle();

                if (_rowPool.Count < MaxPooledElements)
                {
                    _rowPool.Push(row);
                }

                break;

            case TableViewGroupHeaderRow header:
                // Drop the group so a pooled header does not keep a stale TableViewGroupInfo (and through it the
                // CollectionView) alive, and so its next group always arrives as a content change.
                header.Content = null;

                if (_groupHeaderPool.Count < MaxPooledElements)
                {
                    _groupHeaderPool.Push(header);
                }

                break;
        }
    }

    /// <summary>
    /// Drops the pooled elements, for when the table's columns or templates change so completely that reusing
    /// existing rows would cost more than rebuilding them.
    /// </summary>
    public void ClearPools()
    {
        _rowPool.Clear();
        _groupHeaderPool.Clear();
    }
}
