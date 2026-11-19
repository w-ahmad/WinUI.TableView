using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for FrameworkElements.
/// </summary>
internal static class FrameworkElementExtensions
{
#if !WINDOWS
    /// <summary>
    /// Returns the root element a content presenter realized, or <see langword="null"/> when it holds nothing yet.
    /// </summary>
    /// <remarks>
    /// The root is the presenter's single visual child. It is read from the tree because Uno 7 dropped the
    /// <c>ContentTemplateRoot</c> property that Uno 6 had on <see cref="ContentPresenter"/>.
    /// </remarks>
    internal static Microsoft.UI.Xaml.FrameworkElement? GetContentTemplateRoot(this ContentPresenter presenter)
    {
        return Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(presenter) > 0
            ? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(presenter, 0) as Microsoft.UI.Xaml.FrameworkElement
            : null;
    }
#endif

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
