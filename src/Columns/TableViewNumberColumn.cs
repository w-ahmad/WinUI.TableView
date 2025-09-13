using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays a number.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(TextBlock))]
[StyleTypedProperty(Property = nameof(EditingElementStyle), StyleTargetType = typeof(NumberBox))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewNumberColumn : TableViewBoundColumn
{
    /// <summary>
    /// Generates a TextBlock element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TextBlock element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var textBlock = new TextBlock
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(12, 0, 12, 0),
        };
        textBlock.SetBinding(TextBlock.TextProperty, Binding);

        return textBlock;
    }

    /// <summary>
    /// Generates a NumberBox element for editing the cell.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A NumberBox element.</returns>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        var numberBox = new NumberBox();
        numberBox.SetBinding(NumberBox.ValueProperty, Binding);
#if !WINDOWS
        numberBox.DataContext = dataItem;
#endif
        return numberBox;
    }
}
