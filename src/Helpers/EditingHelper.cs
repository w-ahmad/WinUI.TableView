using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Helper class for handling ESC/Enter key functionality in editable columns
/// Only TextBox editing elements are supported
/// </summary>
internal static class EditingHelper
{
    /// <summary>
    /// Adds ESC/Enter key handling to a TextBox editing element
    /// </summary>
    /// <param name="textBox">The TextBox editing element</param>
    /// <param name="cell">The TableView cell</param>
    /// <param name="dataItem">The data item</param>
    /// <param name="propertyPath">The property path for binding</param>
    public static void AddKeyHandling(TextBox textBox, TableViewCell cell, object? dataItem, string? propertyPath)
    {
        // Store original value for ESC cancellation  
        var originalValue = GetPropertyValue(dataItem, propertyPath);
        textBox.Tag = originalValue;
        
        // Set initial text value
        textBox.Text = originalValue?.ToString() ?? string.Empty;
        
        // Handle ESC and Enter key events
        textBox.KeyDown += (sender, args) =>
        {
            if (args.Key == VirtualKey.Escape)
            {
                // Cancel editing: restore original value
                textBox.Text = textBox.Tag?.ToString() ?? string.Empty;
                args.Handled = true;
                EndEditing(cell);
            }
            else if (args.Key == VirtualKey.Enter)
            {
                // Commit changes: update data source manually
                CommitTextValue(textBox, dataItem, propertyPath);
                args.Handled = true;
                EndEditing(cell);
            }
        };
        
        // Commit changes when focus is lost
        textBox.LostFocus += (sender, args) =>
        {
            if (cell.TableView?.IsEditing == true)
            {
                CommitTextValue(textBox, dataItem, propertyPath);
            }
        };
    }
    
    /// <summary>
    /// Ends editing mode and refreshes the cell
    /// /// </summary>
    private static void EndEditing(TableViewCell cell)
    {
        cell.TableView?.SetIsEditing(false);
        cell.SetElement();
    }
    
    /// <summary>
    /// Commits TextBox value to the data source
    /// </summary>
    private static void CommitTextValue(TextBox textBox, object? dataItem, string? propertyPath)
    {
        if (dataItem == null || string.IsNullOrEmpty(propertyPath))
            return;
            
        try
        {
            var property = dataItem.GetType().GetProperty(propertyPath);
            if (property != null && property.CanWrite)
            {
                var convertedValue = ConvertValue(textBox.Text, property.PropertyType);
                property.SetValue(dataItem, convertedValue);
            }
        }
        catch
        {
            // Ignore conversion errors
        }
    }    
    
    /// <summary>
    /// Converts string value to target property type
    /// </summary>
    private static object? ConvertValue(string textValue, Type targetType)
    {
        if (string.IsNullOrEmpty(textValue))
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
        
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        if (underlyingType == typeof(string))
            return textValue;
            
        try
        {
            return Convert.ChangeType(textValue, underlyingType);
        }
        catch
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }   
    
    /// <summary>
    /// Helper method to get property value from data item
    /// </summary>
    private static object? GetPropertyValue(object? dataItem, string? propertyPath)
    {
        if (dataItem == null || string.IsNullOrEmpty(propertyPath))
            return null;
        
        try
        {
            var property = dataItem.GetType().GetProperty(propertyPath);
            return property?.GetValue(dataItem);
        }
        catch
        {
            return null;
        }
    }
}