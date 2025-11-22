using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for FrameworkElements.
/// </summary>
internal static class FrameworkElementExtensions
{
    /// <summary>
    /// NumberBox does not update its Text property automatically until it loses focus in some cases.
    /// This method forces the NumberBox to update its Text property.
    /// </summary>
    /// <param name="numberBox">The NumberBox to update its value.</param>
    internal static void UpdateValue(this NumberBox numberBox)
    {
        var inputTextBox = numberBox.FindDescendant<TextBox>(x => x.Name is "InputBox");

        if (inputTextBox is not null && inputTextBox.Text != numberBox.Text)
        {
            numberBox.Text = inputTextBox.Text;
        }
    }
}
