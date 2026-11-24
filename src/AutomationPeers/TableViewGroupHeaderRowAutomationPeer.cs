using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableViewGroupHeaderRow"/> to UI Automation as an expandable group, named after the group's
/// key and item count.
/// </summary>
/// <remarks>
/// The header used to derive from <c>ListViewHeaderItem</c> and get its peer from there. It is a plain control now
/// that the table hosts its own rows, so the peer is spelled out here, including the ExpandCollapse pattern that
/// lets assistive technology open and close a group without reaching for the chevron button.
/// </remarks>
public partial class TableViewGroupHeaderRowAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly TableViewGroupHeaderRow _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroupHeaderRowAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableViewGroupHeaderRow"/> that is associated with this peer.</param>
    public TableViewGroupHeaderRowAutomationPeer(TableViewGroupHeaderRow owner) : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableViewGroupHeaderRow);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Group;
    }

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = AutomationProperties.GetName(_owner);

        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return _owner.GroupInfo is { } groupInfo ? $"{groupInfo.Key} ({groupInfo.Count})" : base.GetNameCore();
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.ExpandCollapse && _owner.GroupInfo is not null)
        {
            return this;
        }

        return base.GetPatternCore(patternInterface);
    }

    /// <summary>
    /// Gets the expanded/collapsed state of the group.
    /// </summary>
    public ExpandCollapseState ExpandCollapseState =>
        _owner.GroupInfo?.IsExpanded is true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <summary>
    /// Expands the group.
    /// </summary>
    public void Expand()
    {
        if (_owner.GroupInfo is { IsExpanded: false })
        {
            _owner.ToggleExpanded();
        }
    }

    /// <summary>
    /// Collapses the group.
    /// </summary>
    public void Collapse()
    {
        if (_owner.GroupInfo is { IsExpanded: true })
        {
            _owner.ToggleExpanded();
        }
    }
}
