using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableView"/> to UI Automation, implementing the Grid, Table, Selection, Scroll and
/// ItemContainer patterns so automation clients can navigate the row/column structure of the control.
/// </summary>
/// <remarks>
/// The Selection, Scroll and ItemContainer patterns used to come from <c>ListViewAutomationPeer</c>. They are
/// implemented here now, over the table's own selection model and scroll viewer.
/// </remarks>
public partial class TableViewAutomationPeer : FrameworkElementAutomationPeer,
                                              IGridProvider,
                                              ITableProvider,
                                              ISelectionProvider,
                                              IScrollProvider,
                                              IItemContainerProvider
{
    private readonly TableView _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableView"/> that is associated with this peer.</param>
    public TableViewAutomationPeer(TableView owner) : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableView);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.DataGrid;
    }

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore()
    {
        return "table view";
    }

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.Grid => this,
            PatternInterface.Table => this,
            PatternInterface.Selection => this,
            PatternInterface.Scroll => this,
            PatternInterface.ItemContainer => this,
            _ => base.GetPatternCore(patternInterface)
        };
    }

    /// <inheritdoc/>
    protected override IList<AutomationPeer> GetChildrenCore()
    {
        List<AutomationPeer> children = [];

        if (_owner.HeaderRow is { } headerRow && CreatePeerForElement(headerRow) is { } headerPeer)
        {
            children.Add(headerPeer);
        }

        // Only realized rows have peers, which is how every virtualizing control behaves; clients reach the rest
        // through the ItemContainer pattern below.
        foreach (var row in _owner.Rows)
        {
            if (CreatePeerForElement(row) is { } rowPeer)
            {
                children.Add(rowPeer);
            }
        }

        return children;
    }

    /// <summary>
    /// Gets the total number of rows in the grid, equal to the number of items in the <see cref="TableView"/>.
    /// </summary>
    public int RowCount => _owner.Items.Count;

    /// <summary>
    /// Gets the total number of visible columns in the grid.
    /// </summary>
    public int ColumnCount => _owner.Columns.VisibleColumns.Count;

    /// <summary>
    /// Returns the automation peer for the cell at the specified row and column index, realizing the row when it
    /// is not on screen.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    public IRawElementProviderSimple? GetItem(int row, int column)
    {
        if (row < 0 || row >= RowCount || column < 0 || column >= ColumnCount)
        {
            return null;
        }

        // Realize on demand rather than returning null for an off-screen row: the Grid pattern is supposed to be
        // able to reach every cell, and returning null for anything scrolled out of view made large tables
        // effectively unreadable to assistive technology.
        var cell = _owner.GetCellFromSlot(new TableViewCellSlot(row, column))
                   ?? _owner.RealizeRow(row)?.Cells.ElementAtOrDefault(column);

        if (cell is null)
        {
            return null;
        }

        var peer = CreatePeerForElement(cell);

        return peer is null ? null : ProviderFromPeer(peer);
    }

    /// <summary>
    /// Returns automation peers for the realized row header elements.
    /// </summary>
    public IRawElementProviderSimple[] GetRowHeaders()
    {
        List<IRawElementProviderSimple> providers = [];

        foreach (var row in _owner.Rows)
        {
            if (row.RowPresenter?.RowHeader is not { } rowHeader)
            {
                continue;
            }

            if (CreatePeerForElement(rowHeader) is { } peer && ProviderFromPeer(peer) is { } provider)
            {
                providers.Add(provider);
            }
        }

        return [.. providers];
    }

    /// <summary>
    /// Returns automation peers for all visible column header elements.
    /// </summary>
    public IRawElementProviderSimple[] GetColumnHeaders()
    {
        List<IRawElementProviderSimple> providers = [];

        foreach (var column in _owner.Columns.VisibleColumns)
        {
            if (column.HeaderControl is not { } headerControl)
            {
                continue;
            }

            if (CreatePeerForElement(headerControl) is { } peer && ProviderFromPeer(peer) is { } provider)
            {
                providers.Add(provider);
            }
        }

        return [.. providers];
    }

    /// <summary>
    /// Gets the primary axis of traversal for the table. TableView is row-major.
    /// </summary>
    public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;

    // ---------------------------------------------------------------- ISelectionProvider

    /// <inheritdoc/>
    public bool CanSelectMultiple =>
        _owner.SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended;

    /// <inheritdoc/>
    public bool IsSelectionRequired => false;

    /// <summary>
    /// Returns providers for the selected rows that are realized.
    /// </summary>
    /// <remarks>
    /// Bounded to the realized rows on purpose: a selection can cover millions of rows, and materialising a
    /// provider per selected row would hang the client rather than help it.
    /// </remarks>
    public IRawElementProviderSimple[] GetSelection()
    {
        List<IRawElementProviderSimple> providers = [];

        foreach (var row in _owner.Rows)
        {
            if (!row.IsSelected)
            {
                continue;
            }

            if (CreatePeerForElement(row) is { } peer && ProviderFromPeer(peer) is { } provider)
            {
                providers.Add(provider);
            }
        }

        return [.. providers];
    }

    // ------------------------------------------------------------------- IScrollProvider

    /// <inheritdoc/>
    public bool HorizontallyScrollable => _owner.ScrollableWidth > 0;

    /// <inheritdoc/>
    public bool VerticallyScrollable => _owner.ScrollableHeight > 0;

    /// <inheritdoc/>
    public double HorizontalScrollPercent => GetScrollPercent(_owner.HorizontalOffset, _owner.ScrollableWidth);

    /// <inheritdoc/>
    public double VerticalScrollPercent => GetScrollPercent(_owner.VerticalOffset, _owner.ScrollableHeight);

    /// <inheritdoc/>
    public double HorizontalViewSize => GetViewSize(_owner.ViewportWidth, _owner.ScrollableWidth);

    /// <inheritdoc/>
    public double VerticalViewSize => GetViewSize(_owner.ViewportHeight, _owner.ScrollableHeight);

    /// <inheritdoc/>
    public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
    {
        _owner.ScrollBy(ToDelta(horizontalAmount, _owner.ViewportWidth), ToDelta(verticalAmount, _owner.ViewportHeight));
    }

    /// <inheritdoc/>
    public void SetScrollPercent(double horizontalPercent, double verticalPercent)
    {
        _owner.ScrollTo(
            horizontalPercent < 0 ? _owner.HorizontalOffset : horizontalPercent / 100d * _owner.ScrollableWidth,
            verticalPercent < 0 ? _owner.VerticalOffset : verticalPercent / 100d * _owner.ScrollableHeight);
    }

    // -------------------------------------------------------------- IItemContainerProvider

    /// <summary>
    /// Finds the next row after <paramref name="startAfter"/> matching a property, realizing rows as needed.
    /// </summary>
    /// <remarks>
    /// This is how a client reaches rows that are not on screen. Only the properties a virtualized row can be
    /// matched on without realizing every row are supported; anything else returns null rather than quietly
    /// walking the whole source.
    /// </remarks>
    public IRawElementProviderSimple? FindItemByProperty(IRawElementProviderSimple startAfter, AutomationProperty automationProperty, object value)
    {
        var startIndex = 0;

        if (startAfter is not null && PeerFromProvider(startAfter) is TableViewRowAutomationPeer startPeer)
        {
            startIndex = startPeer.RowIndex + 1;
        }

        for (var index = startIndex; index < RowCount; index++)
        {
            if (!Matches(index, automationProperty, value))
            {
                continue;
            }

            if (_owner.RealizeRow(index) is not { } row)
            {
                continue;
            }

            var peer = CreatePeerForElement(row);

            return peer is null ? null : ProviderFromPeer(peer);
        }

        return null;
    }

    private bool Matches(int itemIndex, AutomationProperty? automationProperty, object value)
    {
        if (automationProperty is null)
        {
            return true;
        }

        if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty)
        {
            return value is bool isSelected && _owner.IsRowSelected(itemIndex) == isSelected;
        }

        return false;
    }

    private static double GetScrollPercent(double offset, double scrollable)
    {
        return scrollable > 0 ? offset / scrollable * 100d : 0d;
    }

    private static double GetViewSize(double viewport, double scrollable)
    {
        var extent = viewport + scrollable;

        return extent > 0 ? viewport / extent * 100d : 100d;
    }

    private static double ToDelta(ScrollAmount amount, double viewport)
    {
        const double smallChange = 48d;

        return amount switch
        {
            ScrollAmount.SmallIncrement => smallChange,
            ScrollAmount.SmallDecrement => -smallChange,
            ScrollAmount.LargeIncrement => viewport,
            ScrollAmount.LargeDecrement => -viewport,
            _ => 0d
        };
    }
}
