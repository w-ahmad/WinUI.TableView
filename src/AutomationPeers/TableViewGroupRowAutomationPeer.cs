using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableViewGroupRow"/> to UI Automation as an expandable group.
/// </summary>
public partial class TableViewGroupRowAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly TableViewGroupRow _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroupRowAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableViewGroupRow"/> that is associated with this peer.</param>
    public TableViewGroupRowAutomationPeer(TableViewGroupRow owner) : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableViewGroupRow);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Group;
    }

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore()
    {
        return "group header";
    }

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = AutomationProperties.GetName(_owner);

        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (_owner.Group is not { } group)
        {
            return "Group";
        }

        return $"{group.Key}, {group.ItemCount} items";
    }

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface is PatternInterface.ExpandCollapse ? this : base.GetPatternCore(patternInterface);
    }

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState =>
        _owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public void Expand()
    {
        if (_owner is { Group: { } group, TableView: { } tableView })
        {
            tableView.ExpandGroup(group);
        }
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        if (_owner is { Group: { } group, TableView: { } tableView })
        {
            tableView.CollapseGroup(group);
        }
    }
}
