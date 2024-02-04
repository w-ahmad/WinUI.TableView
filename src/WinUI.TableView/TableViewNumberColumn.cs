using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

public class TableViewNumberColumn : TableViewBoundColumn
{
    internal override FrameworkElement GenerateElement()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(11, 0, 0, 0)
        };

        textBlock.SetBinding(TextBlock.TextProperty, Binding);

        return textBlock;
    }

    internal override FrameworkElement GenerateEditingElement()
    {
        var numberBox = new NumberBox();
        numberBox.SetBinding(NumberBox.ValueProperty, Binding);

        return numberBox;
    }
}
