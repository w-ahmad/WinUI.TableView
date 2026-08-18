using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableViewRow"/> to UI Automation, providing row-specific automation information including
/// meaningful names that convey the row index and content.
/// </summary>
/// <remarks>
/// The SelectionItem pattern used to come from <c>SelectorItemAutomationPeer</c>, which drove
/// <c>ListViewItem.IsSelected</c> directly and so bypassed the table's selection unit and anchor handling. It is
/// implemented here now and routed through the table, matching what the cell peer already did.
/// </remarks>
public partial class TableViewRowAutomationPeer : FrameworkElementAutomationPeer,
                                                 ISelectionItemProvider,
                                                 IExpandCollapseProvider
{
    private readonly TableViewRow _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableViewRow"/> that is associated with this peer.</param>
    public TableViewRowAutomationPeer(TableViewRow owner) : base(owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets the item index of the row this peer represents.
    /// </summary>
    internal int RowIndex => _owner.Index;

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableViewRow);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.DataItem;
    }

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = AutomationProperties.GetName(_owner);
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        var index = _owner.Index;
        return index >= 0 ? $"Row {index + 1}" : "Row";
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var tableView = _owner.TableView;
        if (tableView is null || _owner.Index < 0)
        {
            return base.GetHelpTextCore();
        }

        var parts = new List<string>();
        foreach (var cell in _owner.Cells)
        {
            if (cell.Column is { } column)
            {
                var headerText = GetColumnHeaderText(column);
                var value = column.GetCellContent(_owner.Content)?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(headerText))
                {
                    parts.Add($"{headerText}: {value}");
                }
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : base.GetHelpTextCore();
    }

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.SelectionItem)
        {
            return this;
        }

        if (patternInterface == PatternInterface.ExpandCollapse && HasRowDetails())
        {
            return this;
        }

        return base.GetPatternCore(patternInterface);
    }

    // ISelectionItemProvider

    /// <inheritdoc/>
    public bool IsSelected => _owner.IsSelected;

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer =>
        _owner.TableView is { } tableView && CreatePeerForElement(tableView) is { } peer
            ? ProviderFromPeer(peer)
            : null;

    /// <inheritdoc/>
    public void Select()
    {
        if (_owner is { TableView: { } tableView, Index: >= 0 })
        {
            tableView.MakeSelection(new TableViewCellSlot(_owner.Index, -1), shiftKey: false);
        }
    }

    /// <inheritdoc/>
    public void AddToSelection()
    {
        if (_owner is { TableView: { } tableView, Index: >= 0 })
        {
            tableView.MakeSelection(new TableViewCellSlot(_owner.Index, -1), shiftKey: false, ctrlKey: true);
        }
    }

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        if (_owner is { TableView: { } tableView, Index: >= 0 } && tableView.IsRowSelected(_owner.Index))
        {
            tableView.DeselectRange(new Microsoft.UI.Xaml.Data.ItemIndexRange(_owner.Index, 1));
        }
    }

    // IExpandCollapseProvider

    /// <summary>
    /// Gets the expanded/collapsed state of the row details panel.
    /// </summary>
    public ExpandCollapseState ExpandCollapseState =>
        (_owner.RowPresenter?.IsDetailsPanelVisible ?? false)
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;

    /// <summary>
    /// Expands the row details panel if one is available.
    /// </summary>
    public void Expand()
    {
        if (_owner.TableView is not null && HasRowDetails())
        {
            _owner.RowPresenter?.ShowDetailPane(visible: true);
        }
    }

    /// <summary>
    /// Collapses the row details panel if one is available.
    /// </summary>
    public void Collapse()
    {
        if (_owner.TableView is not null && HasRowDetails())
        {
            _owner.RowPresenter?.ShowDetailPane(visible: false);
        }
    }

    private bool HasRowDetails()
    {
        return (_owner.TableView?.RowDetailsTemplate is not null
            || _owner.TableView?.RowDetailsTemplateSelector is not null)
            && _owner.TableView?.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded;
    }

    /// <summary>
    /// Gets the header text for a column as a string.
    /// </summary>
    internal static string GetColumnHeaderText(TableViewColumn column)
    {
        return column.Header switch
        {
            string s => s,
            { } obj => obj.ToString() ?? string.Empty,
            _ => string.Empty
        };
    }
}
