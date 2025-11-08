using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Linq.Expressions;
using System.Reflection;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that is bound to a data source.
/// </summary>
public abstract class TableViewBoundColumn : TableViewColumn
{
    private Binding _binding = new();
    private Func<object, object?>? _funcCompiledPropertyPath;

    /// <inheritdoc/>
    public override object? GetCellContent(object? dataItem)
    {
        if (dataItem is null)
            return null;

        if (_funcCompiledPropertyPath is null && !string.IsNullOrWhiteSpace(PropertyPath))
            _funcCompiledPropertyPath = dataItem.GetFuncCompiledPropertyPath(PropertyPath!);

        if (_funcCompiledPropertyPath is not null)
            dataItem = _funcCompiledPropertyPath(dataItem);

        if (Binding?.Converter is not null)
        {
            dataItem = Binding.Converter.Convert(
                dataItem,
                typeof(object),
                Binding.ConverterParameter,
                Binding.ConverterLanguage);
        }

        return dataItem;
    }

    /// <summary>
    /// Gets the property path for the binding.
    /// </summary>
    internal string? PropertyPath => Binding?.Path?.Path;

    /// <summary>
    /// Gets or sets the binding for the column.
    /// </summary>
    public virtual Binding Binding
    {
        get => _binding;
        set
        {
            if (_binding != value)
            {
                if (value is not null)
                {
                    value.Mode = BindingMode.TwoWay;

                    if (value.UpdateSourceTrigger == UpdateSourceTrigger.Default)
                    {
                        value.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                    }
                }

                _binding = value!;
            }
        }
    }

    /// <summary>
    /// Gets or sets the data binding used to get or set content for the clipboard.
    /// If no explicit clipboard binding is set, the column's <see cref="Binding"/> is returned as a fallback.
    /// </summary>
    public override Binding? ClipboardContentBinding
    {
        get => base.ClipboardContentBinding ?? Binding;
        set => base.ClipboardContentBinding = value;
    }

    /// <summary>
    /// Handles changes to the ElementStyle property.
    /// </summary>
    private static void OnElementStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column.OwningCollection is { })
        {
            column.OwningCollection.HandleColumnPropertyChanged(column, nameof(ElementStyle));
        }
    }

    /// <summary>
    /// Handles changes to the EditingElementStyle property.
    /// </summary>
    private static void OnEditingElementStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumn column && column.OwningCollection is { })
        {
            column.OwningCollection.HandleColumnPropertyChanged(column, nameof(EditingElementStyle));
        }
    }

    /// <summary>
    /// Gets or sets the style that is used when rendering the element that the column
    /// displays for a cell that is not in editing mode.
    /// </summary>
    public Style ElementStyle
    {
        get => (Style)GetValue(ElementStyleProperty);
        set => SetValue(ElementStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style that is used when rendering the element that the column
    /// displays for a cell in editing mode.
    /// </summary>
    public Style EditingElementStyle
    {
        get => (Style)GetValue(EditingElementStyleProperty);
        set => SetValue(EditingElementStyleProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ElementStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ElementStyleProperty = DependencyProperty.Register(nameof(ElementStyle), typeof(Style), typeof(TableViewBoundColumn), new PropertyMetadata(null, OnElementStyleChanged));

    /// <summary>
    /// Identifies the <see cref="EditingElementStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EditingElementStyleProperty = DependencyProperty.Register(nameof(EditingElementStyle), typeof(Style), typeof(TableViewBoundColumn), new PropertyMetadata(null, OnEditingElementStyleChanged));
}
