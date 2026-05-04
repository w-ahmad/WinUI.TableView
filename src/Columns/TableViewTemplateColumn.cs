using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that uses a DataTemplate for its content.
/// </summary>
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewTemplateColumn : TableViewColumn
{
    private Func<object, object?>? _funcCompiledPropertyPath;

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
        return new ContentControl
        {
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ContentTemplate = CellTemplateSelector?.SelectTemplate(dataItem) ?? CellTemplate
        };
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
            return new ContentControl
            {
                VerticalContentAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ContentTemplate = EditingTemplateSelector?.SelectTemplate(dataItem) ?? EditingTemplate
            };
        }

        return GenerateElement(cell, dataItem);
    }

    /// <inheritdoc/>
    public override void RefreshElement(TableViewCell cell, object? dataItem)
    {
        cell.Content = GenerateElement(cell, dataItem);
    }

    /// <inheritdoc/>
    public override object? GetCellContent(object? dataItem)
    {
        if (dataItem is null)
            return null;

        if (_funcCompiledPropertyPath is null)
        {
            if (!string.IsNullOrWhiteSpace(OperationContentBindingPropertyPath))
                _funcCompiledPropertyPath = dataItem.GetFuncCompiledPropertyPath(OperationContentBindingPropertyPath!);
            else
                return null;
        }

        if (_funcCompiledPropertyPath is not null)
            dataItem = _funcCompiledPropertyPath(dataItem);

        if (OperationContentBinding?.Converter is not null)
        {
            dataItem = OperationContentBinding.Converter.Convert(
                dataItem,
                typeof(object),
                OperationContentBinding.ConverterParameter,
                OperationContentBinding.ConverterLanguage);
        }

        return dataItem;
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
    /// Gets or sets the optional data binding used to perform operations on cell content, for example sorting, filtering and exporting.
    /// Is not used in the CellTemplate or EditingTemplate.
    /// </summary>
    public Binding? OperationContentBinding { get; set; }

    /// <summary>
    /// Gets the property path for the <see cref="OperationContentBinding"/>.
    /// </summary>
    internal string? OperationContentBindingPropertyPath => OperationContentBinding?.Path?.Path;

    /// <summary>
    /// Gets or sets the data binding used to retrieve cell content when copying to the clipboard.
    /// If no explicit clipboard binding is set, the column's <see cref="OperationContentBinding"/> is returned as a fallback.
    /// </summary>
    public override Binding? ClipboardContentBinding
    {
        get => base.ClipboardContentBinding ?? OperationContentBinding;
        set => base.ClipboardContentBinding = value;
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
