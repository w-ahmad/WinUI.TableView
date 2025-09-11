using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays a ToggleSwitch.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(ToggleSwitch))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewToggleSwitchColumn : TableViewBoundColumn
{
    /// <summary>
    /// Initializes a new instance of the TableViewToggleSwitchColumn class.
    /// </summary>
    public TableViewToggleSwitchColumn()
    {
        UseSingleElement = true;
    }

    /// <summary>
    /// Generates a ToggleSwitch element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A ToggleSwitch element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var toggleSwitch = new ToggleSwitch
        {
            OnContent = OnContent,
            OffContent = OffContent,
            UseSystemFocusVisuals = false,
            Margin = new Thickness(12, 0, 12, 0)
        };

        toggleSwitch.SetBinding(ToggleSwitch.IsOnProperty, Binding);
        UpdateToggleButtonState(toggleSwitch);

        return toggleSwitch;
    }

    /// <inheritdoc/>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void UpdateElementState(TableViewCell cell, object? dataItem)
    {
        if (cell?.Content is ToggleSwitch toggleSwitch)
        {
            UpdateToggleButtonState(toggleSwitch);
        }
    }

    /// <summary>
    /// Updates the state of the ToggleSwitch element.
    /// </summary>
    /// <param name="toggleSwitch">The ToggleSwitch element to update.</param>
    private void UpdateToggleButtonState(ToggleSwitch toggleSwitch)
    {
        toggleSwitch.IsHitTestVisible = TableView?.IsReadOnly is false && !IsReadOnly;
    }

    /// <summary>
    /// Gets or sets the content to display when the ToggleSwitch is on.
    /// </summary>
    public object? OnContent
    {
        get => GetValue(OnContentProperty);
        set => SetValue(OnContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display when the ToggleSwitch is off.
    /// </summary>
    public object? OffContent
    {
        get => GetValue(OffContentProperty);
        set => SetValue(OffContentProperty, value);
    }

    /// <summary>
    /// Identifies the OnContent dependency property.
    /// </summary>
    public static readonly DependencyProperty OnContentProperty = DependencyProperty.Register(nameof(OnContent), typeof(object), typeof(TableViewToggleSwitchColumn), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the OffContent dependency property.
    /// </summary>
    public static readonly DependencyProperty OffContentProperty = DependencyProperty.Register(nameof(OffContent), typeof(object), typeof(TableViewToggleSwitchColumn), new PropertyMetadata(null));
}
