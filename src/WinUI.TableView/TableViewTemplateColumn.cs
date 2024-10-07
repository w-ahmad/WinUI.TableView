using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

public class TableViewTemplateColumn : TableViewColumn
{
    public override FrameworkElement GenerateElement()
    {
        var contentControl = new ContentControl
        {
            ContentTemplate = CellTemplate,
            ContentTemplateSelector = CellTemplateSelector
        };

        return contentControl;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        if (EditingTemplate is not null || EditingTemplateSelector is not null)
        {
            var contentControl = new ContentControl
            {
                ContentTemplate = EditingTemplate,
                ContentTemplateSelector = EditingTemplateSelector
            };

            return contentControl;
        }

        return GenerateElement();
    }

    public DataTemplate? CellTemplate
    {
        get => (DataTemplate?)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public DataTemplateSelector? CellTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(CellTemplateSelectorProperty);
        set => SetValue(CellTemplateSelectorProperty, value);
    }

    public DataTemplate? EditingTemplate
    {
        get => (DataTemplate?)GetValue(EditingTemplateProperty); 
        set => SetValue(EditingTemplateProperty, value);
    }

    public DataTemplateSelector? EditingTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(EditingTemplateSelectorProperty);
        set => SetValue(EditingTemplateSelectorProperty, value);
    }

    public static readonly DependencyProperty CellTemplateProperty = DependencyProperty.Register(nameof(CellTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty CellTemplateSelectorProperty = DependencyProperty.Register(nameof(CellTemplateSelector), typeof(DataTemplateSelector), typeof(TableViewTemplateColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty EditingTemplateProperty = DependencyProperty.Register(nameof(EditingTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty EditingTemplateSelectorProperty = DependencyProperty.Register(nameof(EditingTemplateSelector), typeof(DataTemplateSelector), typeof(TableViewTemplateColumn), new PropertyMetadata(default));
}
