using Microsoft.UI.Xaml.Data;
using System;
using System.Reflection;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that is bound to a data source.
/// </summary>
public abstract class TableViewBoundColumn : TableViewColumn
{
    private Type? _listType;
    private string? _propertyPath;
    private Binding _binding = new();
    private (PropertyInfo, object?)[]? _propertyInfo;

    public override object? GetCellContent(object? dataItem)
    {
        if (dataItem is null) return null;

        if (_propertyInfo is null || dataItem.GetType() != _listType)
        {
            _listType = dataItem.GetType();
            dataItem = dataItem.GetValue(_listType, PropertyPath, out _propertyInfo);
        }
        else
        {
            dataItem = dataItem.GetValue(_propertyInfo);
        }

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
    internal string? PropertyPath
    {
        get
        {
            _propertyPath ??= Binding?.Path?.Path;
            return _propertyPath;
        }
    }

    /// <summary>
    /// Gets or sets the binding for the column.
    /// </summary>
    public virtual Binding Binding
    {
        get => _binding;
        set
        {
            _binding = value;
            if (_binding is not null)
            {
                _binding.Mode = BindingMode.TwoWay;
            }
        }
    }
}
