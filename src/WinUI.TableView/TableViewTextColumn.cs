using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

public class TableViewTextColumn : TableViewBoundColumn
{
    public override FrameworkElement GenerateElement()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
            TextTrimming = TextTrimming.Clip
        };
        textBlock.SetBinding(TextBlock.TextProperty, Binding);
        return textBlock;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        var textBox = new TextBox();
        textBox.SetBinding(TextBox.TextProperty, Binding);

        return textBox;
    }
}
