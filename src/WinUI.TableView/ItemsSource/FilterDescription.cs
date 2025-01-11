using System;

namespace WinUI.TableView;

/// <summary>
/// Describes a filter operation applied to TableView items.
/// </summary>
public class FilterDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterDescription"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property to filter by.</param>
    /// <param name="predicate">The predicate to apply for filtering.</param>
    public FilterDescription(string propertyName,
                             Predicate<object?> predicate)
    {
        PropertyName = propertyName;
        Predicate = predicate;
    }

    /// <summary>
    /// Gets the name of the property to filter by.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the predicate to apply for filtering.
    /// </summary>
    public Predicate<object?> Predicate { get; }
}
