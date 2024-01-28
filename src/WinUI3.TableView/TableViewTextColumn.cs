using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI3.TableView;

public class TableViewTextColumn : TableViewBoundColumn
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
        var textBox = new TextBox();
        textBox.SetBinding(TextBox.TextProperty, Binding);

        return textBox;
    }
}
