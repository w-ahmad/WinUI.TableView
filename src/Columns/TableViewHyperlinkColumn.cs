using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays a HyperlinkButton.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(HyperlinkButton))]
[StyleTypedProperty(Property = nameof(EditingElementStyle), StyleTargetType = typeof(TextBox))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewHyperlinkColumn : TableViewTextColumn
{
    /// <summary>
    /// Generates a HyperlinkButton element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A HyperlinkButton element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var hyperlinkButton = new HyperlinkButton
        {
            Margin = new Thickness(2, 0, 2, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        // Bind NavigateUri to the main Binding property
        hyperlinkButton.SetBinding(HyperlinkButton.NavigateUriProperty, Binding);

        // Bind Content to ContentBinding if set, otherwise use the NavigateUri
        var contentBinding = ContentBinding ?? Binding;
        hyperlinkButton.SetBinding(ContentControl.ContentProperty, contentBinding);

        return hyperlinkButton;
    }

    /// <summary>
    /// Gets or sets the binding for the hyperlink content.
    /// If not set, the NavigateUri binding will be used for the content.
    /// </summary>
    public Binding? ContentBinding { get; set; }
}