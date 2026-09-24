using Microsoft.UI.Xaml.Automation.Peers;
using WinUI.TableView.AutomationPeers;

namespace WinUI.TableView;

/// <summary>
/// Partial class for TableView that provides UI Automation support.
/// </summary>
public partial class TableView
{
    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TableViewAutomationPeer(this);
    }
}
