using System;
using System.Collections;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Describes a sort operation applied to TableView items.
/// </summary>
public class SortDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortDescription"/> class that describes
    /// a sort on the object itself
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="direction">The direction of the sort.</param>
    /// <param name="comparer">An optional comparer to use for sorting.</param>
    /// <param name="valueDelegate">An optional delegate to extract the value to sort by.</param>
    public SortDescription(string propertyName,
                           SortDirection direction,
                           IComparer? comparer = null,
                           Func<object?, object?>? valueDelegate = null)
    {
        PropertyName = propertyName;
        Direction = direction;
        Comparer = comparer;
        ValueDelegate = valueDelegate;
    }

    /// <summary>
    /// Gets the value of the specified property from the given item.
    /// </summary>
    /// <param name="item">The item to get the property value from.</param>
    /// <returns>The value of the property.</returns>
    public virtual object? GetPropertyValue(object? item)
    {
        if (ValueDelegate is not null)
        {
            return ValueDelegate(item);
        }
        else
        {
            return item?.GetType()
                        .GetProperty(PropertyName)?
                        .GetValue(item);
        }
    }

    /// <summary>
    /// Compares two objects based on the sort description.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>An integer that indicates the relative order of the objects being compared.</returns>
    public int Compare(object? x, object? y)
    {
        return (Comparer ?? Comparer<object>.Default).Compare(x, y);
    }

    /// <summary>
    /// Gets the name of the property to sort by.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the direction of the sort.
    /// </summary>
    public SortDirection Direction { get; }

    /// <summary>
    /// Gets the comparer to use for sorting.
    /// </summary>
    public IComparer? Comparer { get; }

    /// <summary>
    /// Gets the delegate to extract the value to sort by.
    /// </summary>
    public Func<object?, object?>? ValueDelegate { get; }
}
