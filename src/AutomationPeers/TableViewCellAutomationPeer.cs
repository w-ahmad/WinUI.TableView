using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System.Collections.Generic;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableViewCell"/> to UI Automation.
/// Implements <see cref="IGridItemProvider"/>, <see cref="ITableItemProvider"/>, and
/// <see cref="ISelectionItemProvider"/> so that automation clients can identify the cell's
/// position within the grid, its column header, and its selection state.
/// </summary>
public partial class TableViewCellAutomationPeer : FrameworkElementAutomationPeer,
    IGridItemProvider,
    ITableItemProvider,
    ISelectionItemProvider
{
    private readonly TableViewCell _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewCellAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableViewCell"/> that is associated with this peer.</param>
    public TableViewCellAutomationPeer(TableViewCell owner) : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableViewCell);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Custom;
    }

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore()
    {
        return "cell";
    }

    /// <inheritdoc/>
    protected override bool IsContentElementCore()
    {
        return true;
    }

    /// <inheritdoc/>
    protected override bool IsControlElementCore()
    {
        return true;
    }

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = AutomationProperties.GetName(_owner);
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        var columnHeader = _owner.Column is { } col
            ? TableViewRowAutomationPeer.GetColumnHeaderText(col)
            : string.Empty;

        var rowNumber = _owner.Row?.RowNumber ?? 0;
        var rowDisplay = rowNumber > 0 ? $"Row {rowNumber}" : string.Empty;

        var cellValue = _owner.Column is TableViewTemplateColumn
            ? string.Empty
            : _owner.Column?.GetCellContent(_owner.Row?.Content)?.ToString() ?? string.Empty;

        var parts = new List<string>(3);
        if (!string.IsNullOrEmpty(columnHeader))
        {
            parts.Add(columnHeader);
        }

        if (!string.IsNullOrEmpty(rowDisplay))
        {
            parts.Add(rowDisplay);
        }

        if (!string.IsNullOrEmpty(cellValue))
        {
            parts.Add(cellValue);
        }

        return parts.Count > 0 ? string.Join(", ", parts) : base.GetNameCore();
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        if (_owner.Column is not { } column)
        {
            return base.GetHelpTextCore();
        }

        var sortInfo = column.SortDirection switch
        {
            SortDirection.Ascending => $" ({TableViewLocalizedStrings.SortAscending})",
            SortDirection.Descending => $" ({TableViewLocalizedStrings.SortDescending})",
            _ => string.Empty
        };

        var filterInfo = column.IsFiltered ? $" ({TableViewLocalizedStrings.Filtered})" : string.Empty;

        var header = TableViewRowAutomationPeer.GetColumnHeaderText(column);
        return string.IsNullOrEmpty(header) ? base.GetHelpTextCore() : $"{header}{sortInfo}{filterInfo}";
    }

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.GridItem => this,
            PatternInterface.TableItem => this,
            PatternInterface.SelectionItem => this,
            _ => base.GetPatternCore(patternInterface)
        };
    }

    // IGridItemProvider

    /// <summary>
    /// Gets the zero-based row index of the cell within the grid.
    /// </summary>
    public int Row => _owner.Row?.Index ?? -1;

    /// <summary>
    /// Gets the zero-based column index of the cell within the grid.
    /// </summary>
    public int Column => _owner.Column is null ? -1 : _owner.Index;

    /// <summary>
    /// Gets the number of rows spanned by this cell. Always 1.
    /// </summary>
    public int RowSpan => 1;

    /// <summary>
    /// Gets the number of columns spanned by this cell. Always 1.
    /// </summary>
    public int ColumnSpan => 1;

    /// <summary>
    /// Gets the automation provider for the containing <see cref="TableView"/> grid.
    /// </summary>
    public IRawElementProviderSimple? ContainingGrid
    {
        get
        {
            var tableView = _owner.TableView;
            if (tableView is null)
            {
                return null;
            }

            var peer = CreatePeerForElement(tableView);
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }

    // ITableItemProvider

    /// <summary>
    /// Returns row header items associated with this cell. Returns an empty array when no row headers are configured.
    /// </summary>
    public IRawElementProviderSimple[] GetRowHeaderItems()
    {
        var rowHeader = _owner.Row?.RowPresenter?.RowHeader;
        if (rowHeader is null)
        {
            return [];
        }

        var peer = CreatePeerForElement(rowHeader);
        if (peer is null)
        {
            return [];
        }

        var provider = ProviderFromPeer(peer);
        return provider is null ? [] : [provider];
    }

    /// <summary>
    /// Returns the column header item for the column this cell belongs to.
    /// </summary>
    public IRawElementProviderSimple[] GetColumnHeaderItems()
    {
        if (_owner.Column?.HeaderControl is not { } headerControl)
        {
            return [];
        }

        var peer = CreatePeerForElement(headerControl);
        if (peer is null)
        {
            return [];
        }

        var provider = ProviderFromPeer(peer);
        return provider is null ? [] : [provider];
    }

    // ISelectionItemProvider

    /// <summary>
    /// Gets a value indicating whether this cell is currently selected.
    /// </summary>
    public bool IsSelected => _owner.IsSelected;

    /// <summary>
    /// Selects this cell, clearing any existing selection.
    /// </summary>
    public void Select()
    {
        if (_owner.TableView is { } tableView)
        {
            tableView.MakeSelection(_owner.Slot, shiftKey: false, ctrlKey: false);
        }
    }

    /// <summary>
    /// Adds this cell to the current selection without clearing it.
    /// </summary>
    public void AddToSelection()
    {
        if (_owner.TableView is { } tableView)
        {
            tableView.MakeSelection(_owner.Slot, shiftKey: false, ctrlKey: true);
        }
    }

    /// <summary>
    /// Removes this cell from the current selection.
    /// </summary>
    public void RemoveFromSelection()
    {
        if (_owner.TableView is { } tableView && _owner.IsSelected)
        {
            tableView.DeselectCell(_owner.Slot);
        }
    }

    /// <summary>
    /// Gets the automation provider for the selection container (the owning <see cref="TableView"/>).
    /// </summary>
    public IRawElementProviderSimple? SelectionContainer => ContainingGrid;
}
