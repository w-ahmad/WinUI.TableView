using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace WinUI.TableView.Layout;

/// <summary>
/// Creates and recycles the <see cref="TableViewRow"/> elements the row host displays.
/// </summary>
/// <remarks>
/// The repeater delegates recycling entirely to the factory, so this owns the pool. Rows are the expensive
/// element — each one builds a cell per visible column — which is exactly why they are pooled and reused rather
/// than recreated as the viewport moves.
/// </remarks>
internal sealed class TableViewRowElementFactory : IElementFactory
{
    private const int MaxPooledElements = 64;

    private readonly TableView _tableView;
    private readonly Stack<TableViewRow> _rowPool = new();

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
        var row = _rowPool.Count > 0 ? _rowPool.Pop() : _tableView.CreateRow();
        row.TableView = _tableView;

        // Assigning Content is what drives cell creation on a new row and cell refresh on a reused one.
        row.Content = args.Data;

        return row;
    }

    /// <inheritdoc/>
    public void RecycleElement(ElementFactoryRecycleArgs args)
    {
        if (args.Element is not TableViewRow row)
        {
            return;
        }

        row.PrepareForRecycle();

        if (_rowPool.Count < MaxPooledElements)
        {
            _rowPool.Push(row);
        }
    }

    /// <summary>
    /// Drops the pooled elements, for when the table's columns or templates change so completely that reusing
    /// existing rows would cost more than rebuilding them.
    /// </summary>
    public void ClearPools()
    {
        _rowPool.Clear();
    }
}
