using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

public class TableViewNumberColumn : TableViewBoundColumn
{
    public override FrameworkElement GenerateElement()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
        };

        return textBlock;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        var numberBox = new NumberBox();
        numberBox.SetBinding(NumberBox.ValueProperty, Binding);

        return numberBox;
    }
}
