using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace WinUI.TableView.Layout;

/// <summary>
/// Creates and recycles the elements the row host displays: a <see cref="TableViewRow"/> for a data row and a
/// <see cref="TableViewGroupRow"/> for a group header row.
/// </summary>
/// <remarks>
/// The repeater delegates recycling entirely to the factory, so this owns the pools. Rows are the expensive
/// element — each one builds a cell per visible column — which is exactly why they are pooled and reused rather
/// than recreated as the viewport moves.
/// </remarks>
internal sealed class TableViewRowElementFactory : IElementFactory
{
    private const int MaxPooledElements = 64;

    private readonly TableView _tableView;
    private readonly Stack<TableViewRow> _rowPool = new();
    private readonly Stack<TableViewGroupRow> _groupRowPool = new();

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
        if (args.Data is TableViewGroup group)
        {
            var groupRow = _groupRowPool.Count > 0 ? _groupRowPool.Pop() : new TableViewGroupRow();
            groupRow.TableView = _tableView;
            groupRow.PrepareForGroup(group, _tableView.ResolveGroupHeaderTemplate(group), !_tableView.IsGroupCollapsed(group));

            return groupRow;
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

            case TableViewGroupRow groupRow:
                groupRow.PrepareForRecycle();

                if (_groupRowPool.Count < MaxPooledElements)
                {
                    _groupRowPool.Push(groupRow);
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
        _groupRowPool.Clear();
    }
}
