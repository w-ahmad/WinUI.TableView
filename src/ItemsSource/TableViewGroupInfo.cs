#if WINDOWS
using Microsoft.UI.Xaml;
using WinRT;

namespace WinUI.TableView;

/// <summary>
/// Describes a single group header shown by a <see cref="TableViewGroupHeaderRow"/> - its display name, item
/// count, nesting level, and expanded/collapsed state. This is the object bound as a <see cref="CollectionViewGroup"/>'s
/// <see cref="CollectionViewGroup.Group"/>.
/// </summary>
[GeneratedBindableCustomProperty]
public partial class TableViewGroupInfo : DependencyObject
{

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var info = (TableViewGroupInfo)d;
        info.CollectionView?.OnGroupExpandedChanged(info);
    }

    /// <summary>
    /// A stable identifier for this group's position in the group tree (level + key, prefixed by every
    /// ancestor's own key), used to persist its expanded/collapsed state across rebuilds.
    /// </summary>
    internal object[] GroupPath { get; set; } = [];

    /// <summary>
    /// The <see cref="GroupDescription"/> for this group's level, used to scope its persisted expanded/collapsed
    /// override to the description instance it belongs to - see <see cref="CollectionView.OnGroupExpandedChanged"/>.
    /// </summary>
    internal GroupDescription? Description { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="CollectionView"/> that owns this group, used to notify it when the group's
    /// expanded/collapsed state changes.
    /// </summary>
    internal CollectionView? CollectionView { get; set; }

    /// <summary>
    /// Gets the group's key, e.g. the value of the column/property being grouped by.
    /// </summary>
    public object? Key { get; init; }

    /// <summary>
    /// Gets the number of items in this group (including all descendant subgroups/items, if any).
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the zero-based nesting level of this group among the active <see cref="GroupDescription"/> entries.
    /// </summary>
    internal int Level { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this group's items (and, for a non-leaf group, every descendant
    /// subgroup/item) are currently shown.
    /// </summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="IsExpanded"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TableViewGroupInfo), new PropertyMetadata(true, OnIsExpandedChanged));

    /// <inheritdoc/>
    public override string? ToString()
    {
        return $"{Key}";
    }
}
#endif
