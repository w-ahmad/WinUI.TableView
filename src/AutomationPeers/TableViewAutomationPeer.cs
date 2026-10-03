using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System.Collections.Generic;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableView"/> to UI Automation, implementing the Grid and Table patterns
/// so automation clients can navigate the row/column structure of the control.
/// </summary>
public partial class TableViewAutomationPeer : ListViewAutomationPeer, IGridProvider, ITableProvider
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
        return TableViewLocalizedStrings.TableViewControlType;
    }

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.Grid => this,
            PatternInterface.Table => this,
            _ => base.GetPatternCore(patternInterface)
        };
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
    /// Returns the automation peer for the cell at the specified row and column index.
    /// Returns <see langword="null"/> if the cell is not realized (virtualized) or the indices are out of range.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="column">Zero-based column index.</param>
    public IRawElementProviderSimple? GetItem(int row, int column)
    {
        if (row < 0 || row >= RowCount || column < 0 || column >= ColumnCount)
        {
            return null;
        }

        var slot = new TableViewCellSlot(row, column);
        var cell = _owner.GetCellFromSlot(slot);
        if (cell is null)
        {
            return null;
        }

        var peer = CreatePeerForElement(cell);
        return peer is null ? null : ProviderFromPeer(peer);
    }

    /// <summary>
    /// Returns automation peers for all row header elements.
    /// </summary>
    public IRawElementProviderSimple[] GetRowHeaders()
    {
        var providers = new List<IRawElementProviderSimple>();

        foreach (var row in _owner.Rows)
        {
            var rowHeader = row.RowPresenter?.RowHeader;
            if (rowHeader is null)
            {
                continue;
            }

            var peer = CreatePeerForElement(rowHeader);
            if (peer is null)
            {
                continue;
            }

            var provider = ProviderFromPeer(peer);
            if (provider is not null)
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
        var providers = new List<IRawElementProviderSimple>();

        foreach (var column in _owner.Columns.VisibleColumns)
        {
            if (column.HeaderControl is { } headerControl)
            {
                var peer = CreatePeerForElement(headerControl);
                if (peer is not null)
                {
                    var provider = ProviderFromPeer(peer);
                    if (provider is not null)
                    {
                        providers.Add(provider);
                    }
                }
            }
        }

        return [.. providers];
    }

    /// <summary>
    /// Gets the primary axis of traversal for the table. TableView is row-major.
    /// </summary>
    public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;
}
