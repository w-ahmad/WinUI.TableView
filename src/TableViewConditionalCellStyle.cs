using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
#if WINDOWS
using WinUI.TableView.Collections;
#endif

namespace WinUI.TableView;

/// <summary>
/// Represents a collection of <see cref="TableViewConditionalCellStyle"/> for a TableView.
/// </summary>
public partial class TableViewConditionalCellStylesCollection : DependencyObjectCollection<TableViewConditionalCellStyle>;

/// <summary>
/// Represents the context information for evaluating a conditional cell style in a TableView.
/// </summary>
/// <param name="Column">The column associated with the cell.</param>
/// <param name="DataItem">The data item represented by the cell.</param>
public partial record struct TableViewConditionalCellStyleContext(TableViewColumn Column, object? DataItem);

/// <summary>
/// Represents a conditional style for a table view cell that is applied when a specified predicate evaluates to true.
/// </summary>
/// <remarks>Use this class to define styles that are conditionally applied to cells in a table view based on
/// custom logic. The style is only applied to cells for which the predicate returns <see langword="true"/>. Multiple
/// conditional styles can be used to support complex styling scenarios.</remarks>
[ContentProperty(Name = nameof(Style))]
[StyleTypedProperty(Property = nameof(Style), StyleTargetType = typeof(TableViewCell))]
public partial class TableViewConditionalCellStyle : DependencyObject
{
    /// <summary>
    /// Gets or sets the style to be applied to the table view cell when the predicate evaluates to true.
    /// </summary>
    public Style? Style
    {
        get => (Style?)GetValue(StyleProperty);
        set => SetValue(StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the predicate that determines whether the style should be applied to a given cell.
    /// </summary>
    public Predicate<TableViewConditionalCellStyleContext>? Predicate
    {
        get => (Predicate<TableViewConditionalCellStyleContext>?)GetValue(PredicateProperty);
        set => SetValue(PredicateProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty StyleProperty = DependencyProperty.Register(nameof(Style), typeof(Style), typeof(TableViewConditionalCellStyle), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the <see cref="Predicate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PredicateProperty = DependencyProperty.Register(nameof(Predicate), typeof(Predicate<TableViewConditionalCellStyleContext>), typeof(TableViewConditionalCellStyle), new PropertyMetadata(default));
}
