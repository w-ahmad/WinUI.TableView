using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableViewRowHeader"/> to UI Automation, providing a meaningful
/// accessible name that identifies the row number and any content displayed in the header.
/// </summary>
public partial class TableViewRowHeaderAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly TableViewRowHeader _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowHeaderAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableViewRowHeader"/> that is associated with this peer.</param>
    public TableViewRowHeaderAutomationPeer(TableViewRowHeader owner) : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableViewRowHeader);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.HeaderItem;
    }

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore()
    {
        return "row header";
    }

    /// <inheritdoc/>
    protected override bool IsContentElementCore()
    {
        return false;
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

        // Try to derive a name from the row header's content
        var content = _owner.Content;
        if (content is string contentStr && !string.IsNullOrEmpty(contentStr))
        {
            return contentStr;
        }

        if (content is not null && content is not string)
        {
            var str = content.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                return str;
            }
        }

        // Fall back to the row number
        var rowNumber = _owner.TableViewRow?.RowNumber ?? 0;
        return rowNumber > 0 ? $"Row {rowNumber}" : base.GetNameCore();
    }
}
