using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

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
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = GetContent(dataItem),
            NavigateUri = GetNavigateUri(dataItem)
        };

        return hyperlinkButton;
    }

    /// <summary>
    /// Gets the NavigateUri for the HyperlinkButton based on the cell content or binding.
    /// </summary>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>The NavigateUri for the HyperlinkButton.</returns>
    protected virtual Uri? GetNavigateUri(object? dataItem)
    {
        var cellContent = GetCellContent(dataItem);

        if (cellContent is Uri uri)
        {
            return uri;
        }

        if (cellContent is string str)
        {
            return new Uri(str, UriKind.RelativeOrAbsolute);
        }

        return default;
    }

    /// <summary>
    /// Gets the content for the HyperlinkButton based on the ContentBinding or falls back to the cell content.
    /// </summary>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>The content for the HyperlinkButton.</returns>
    protected virtual object? GetContent(object? dataItem)
    {
        if (TableView?.MemberValueProvider is { } provider &&
             provider.TryGetContentBindingValue(ContentBinding?.Path?.Path, dataItem, out var value))
        {
            return value;
        }

        return GetCellContent(dataItem);
    }

    /// <inheritdoc/>
    public override void RefreshElement(TableViewCell cell, object? dataItem)
    {
        if (cell.Content is not HyperlinkButton hyperlinkButton)
            base.RefreshElement(cell, dataItem);
        else
        {
            hyperlinkButton.Content = GetContent(dataItem);
            hyperlinkButton.NavigateUri = GetNavigateUri(dataItem);
        }
    }

    /// <summary>
    /// Gets or sets the binding for the hyperlink content.
    /// If not set, the NavigateUri binding will be used for the content.
    /// </summary>
    public Binding? ContentBinding { get; set; }
}