using System.Collections;

namespace WinUI.TableView;

/// <summary>
/// Describes a grouping operation applied to TableView items. Extends <see cref="SortDescription"/> since
/// grouping and sorting share the same "extract a value, then order/compare by it" shape - a group's own
/// items, and the groups themselves, are ordered using this description's <see cref="SortDescription.Direction"/>
/// and <see cref="SortDescription.Comparer"/>.
/// </summary>
public class GroupDescription : SortDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDescription"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property to group by.</param>
    /// <param name="direction">The direction groups are ordered in.</param>
    /// <param name="comparer">An optional comparer to use for ordering groups.</param>
    /// <param name="valueDelegate">An optional delegate to extract the value to group by.</param>
    public GroupDescription(string? propertyName,
                            SortDirection direction = SortDirection.Ascending,
                            IComparer? comparer = null,
                            Func<object?, object?>? valueDelegate = null)
        : base(propertyName, direction, comparer, valueDelegate)
    {
    }

    /// <summary>
    /// Gets or sets what groups at this level are ordered by - their key value (default) or the number of
    /// items they contain. <see cref="SortDescription.Direction"/> still controls ascending/descending of
    /// whichever this is set to.
    /// </summary>
    public GroupSortMode SortMode { get; set; } = GroupSortMode.Key;
}
