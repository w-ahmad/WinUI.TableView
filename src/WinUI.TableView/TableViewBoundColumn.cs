using Microsoft.UI.Xaml;
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

    /// <summary>
    /// Gets or sets a value indicating whether the column can be sorted. This can be overridden by the TableView.
    /// </summary>
    public bool CanSort
    {
        get => (bool)GetValue(CanSortProperty);
        set => SetValue(CanSortProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column can be filtered.
    /// </summary>
    public bool CanFilter
    {
        get => (bool)GetValue(CanFilterProperty);
        set => SetValue(CanFilterProperty, value);
    }

    /// <summary>
    /// Identifies the CanSort dependency property.
    /// </summary>
    public static readonly DependencyProperty CanSortProperty = DependencyProperty.Register(nameof(CanSort), typeof(bool), typeof(TableViewBoundColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the CanFilter dependency property.
    /// </summary>
    public static readonly DependencyProperty CanFilterProperty = DependencyProperty.Register(nameof(CanFilter), typeof(bool), typeof(TableViewBoundColumn), new PropertyMetadata(true));
}
