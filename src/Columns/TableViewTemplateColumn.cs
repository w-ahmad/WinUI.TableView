using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that uses a DataTemplate for its content.
/// </summary>
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewTemplateColumn : TableViewColumn
{
    /// <summary>
    /// Initializes a new instance of the TableViewTemplateColumn class.
    /// </summary>
    public TableViewTemplateColumn()
    {
        CanSort = false;
        CanFilter = false;
    }

    /// <summary>
    /// Generates a ContentControl for the cell based on CellTemplate and CellTemplateSelector.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A ContentControl element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var template = CellTemplateSelector?.SelectTemplate(dataItem) ?? CellTemplate;
        return (template?.LoadContent() as FrameworkElement)!;
    }

    /// <summary>
    /// Generates a ContentControl for editing the cell based on EditingTemplate and EditingTemplateSelector. 
    /// If EditingTemplate or EditingTemplateSelector is not set, GenerateElement is used instead to generate the editing element.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A ContentControl element.</returns>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        if (EditingTemplate is not null || EditingTemplateSelector is not null)
        {
            var template = EditingTemplateSelector?.SelectTemplate(dataItem) ?? EditingTemplate;
            return (template?.LoadContent() as FrameworkElement)!;
        }

        return GenerateElement(cell, dataItem);
    }

    /// <inheritdoc/>
    public override void RefreshElement(TableViewCell cell, object? dataItem)
    {
        cell.Content = GenerateElement(cell, dataItem);
    }

    /// <summary>
    /// Gets or sets the DataTemplate for the cell content.
    /// </summary>
    public DataTemplate? CellTemplate
    {
        get => (DataTemplate?)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the DataTemplateSelector for the cell content.
    /// </summary>
    public DataTemplateSelector? CellTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(CellTemplateSelectorProperty);
        set => SetValue(CellTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the DataTemplate for the editing cell content.
    /// </summary>
    public DataTemplate? EditingTemplate
    {
        get => (DataTemplate?)GetValue(EditingTemplateProperty);
        set => SetValue(EditingTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the DataTemplateSelector for the editing cell content.
    /// </summary>
    public DataTemplateSelector? EditingTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(EditingTemplateSelectorProperty);
        set => SetValue(EditingTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Identifies the CellTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty CellTemplateProperty = DependencyProperty.Register(nameof(CellTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the CellTemplateSelector dependency property.
    /// </summary>
    public static readonly DependencyProperty CellTemplateSelectorProperty = DependencyProperty.Register(nameof(CellTemplateSelector), typeof(DataTemplateSelector), typeof(TableViewTemplateColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the EditingTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty EditingTemplateProperty = DependencyProperty.Register(nameof(EditingTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the EditingTemplateSelector dependency property.
    /// </summary>
    public static readonly DependencyProperty EditingTemplateSelectorProperty = DependencyProperty.Register(nameof(EditingTemplateSelector), typeof(DataTemplateSelector), typeof(TableViewTemplateColumn), new PropertyMetadata(default));
}
