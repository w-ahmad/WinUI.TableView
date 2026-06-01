using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace WinUI.TableView.SampleApp.Helpers;
internal static class UIHelper
{
    // Confirmation of Action
    static public void AnnounceActionForAccessibility(UIElement ue, string announcement, string activityID)
    {
        var peer = FrameworkElementAutomationPeer.FromElement(ue);
        peer.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted,
                                    AutomationNotificationProcessing.ImportantMostRecent, announcement, activityID);
    }
}
