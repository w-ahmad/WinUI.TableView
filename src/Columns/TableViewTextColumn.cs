using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays text.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(TextBlock))]
[StyleTypedProperty(Property = nameof(EditingElementStyle), StyleTargetType = typeof(TextBox))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewTextColumn : TableViewBoundColumn
{
    /// <summary>
    /// Generates a TextBlock element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TextBlock element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
            Text = GetCellContent(dataItem)?.ToString()
        };

        return textBlock;
    }

    /// <summary>
    /// Generates a TextBox element for editing the cell.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TextBox element.</returns>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        return new TextBox { Text = GetCellContent(dataItem)?.ToString() };
    }

    /// <inheritdoc/>
    public override void RefreshElement(TableViewCell cell, object? dataItem)
    {
        if (cell.Content is not TextBlock textBlock)
            base.RefreshElement(cell, dataItem);
        else
            textBlock.Text = GetCellContent(dataItem)?.ToString();
    }

    /// <inheritdoc/>
    protected internal override object? PrepareCellForEdit(TableViewCell cell, object? dataItem, RoutedEventArgs routedEvent)
    {
        if (cell.Content is TextBox textBox)
        {
            textBox.SelectAll();
            return textBox.Text;
        }

        return base.PrepareCellForEdit(cell, dataItem, routedEvent);
    }

    /// <inheritdoc/>
    protected internal override void EndCellEditing(TableViewCell cell, object? dataItem, TableViewEditAction editAction, object? uneditedValue)
    {
        if (cell.Content is TextBox textBox && editAction == TableViewEditAction.Commit)
        {
            TrySetBindingValue(dataItem, textBox.Text);
        }
    }
}
