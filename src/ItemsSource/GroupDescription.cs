using System;
using System.Collections;

namespace WinUI.TableView;

/// <summary>
/// Describes a grouping level applied to TableView items.
/// </summary>
/// <remarks>
/// A group description is a <see cref="SortDescription"/>: the value it extracts is both the group key and the
/// key the items are ordered by, so grouping needs no sorting mechanism of its own. Group descriptions are
/// applied before <see cref="CollectionView.SortDescriptions"/>, so items are laid out group by group and sorted
/// within each group.
/// </remarks>
public class GroupDescription : SortDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDescription"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property to group by.</param>
    /// <param name="direction">The direction the groups are ordered in.</param>
    /// <param name="comparer">An optional comparer used to order the groups.</param>
    /// <param name="valueDelegate">An optional delegate that produces the group key for an item.</param>
    public GroupDescription(string? propertyName,
                           SortDirection direction = SortDirection.Ascending,
                           IComparer? comparer = null,
                           Func<object?, object?>? valueDelegate = null)
        : base(propertyName, direction, comparer, valueDelegate)
    {
    }
}
