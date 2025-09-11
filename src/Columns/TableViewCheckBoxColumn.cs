using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays a CheckBox.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(CheckBox))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewCheckBoxColumn : TableViewBoundColumn
{
    /// <summary>
    /// Initializes a new instance of the TableViewCheckBoxColumn class.
    /// </summary>
    public TableViewCheckBoxColumn()
    {
        UseSingleElement = true;
    }

    /// <summary>
    /// Generates a CheckBox element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A CheckBox element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var checkBox = new CheckBox
        {
            MinWidth = 20,
            MaxWidth = 20,
            Margin = new Thickness(12, 0, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            UseSystemFocusVisuals = false,
        };

        checkBox.SetBinding(ToggleButton.IsCheckedProperty, Binding);
        UpdateCheckBoxState(checkBox);

        return checkBox;
    }

    /// <inheritdoc/>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void UpdateElementState(TableViewCell cell, object? dataItem)
    {
        if (cell?.Content is CheckBox checkBox)
        {
            UpdateCheckBoxState(checkBox);
        }
    }

    /// <summary>
    /// Updates the state of the CheckBox element.
    /// </summary>
    /// <param name="checkBox">The CheckBox element to update.</param>
    private void UpdateCheckBoxState(CheckBox checkBox)
    {
        checkBox.IsHitTestVisible = TableView?.IsReadOnly is false && !IsReadOnly;
    }
}
