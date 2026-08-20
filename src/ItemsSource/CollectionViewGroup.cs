#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation.Collections;
using WinUI.TableView.Collections;

namespace WinUI.TableView;

/// <summary>
/// A single flattened entry in <see cref="CollectionView.CollectionGroups"/>. For a leaf-level group,
/// <see cref="GroupItems"/> holds the actual items; for a non-leaf (parent) group it's always empty, since
/// <see cref="CollectionView"/> flattens the group tree depth-first so a plain <see cref="ICollectionView.CollectionGroups"/>
/// consumer (like a grouped ListView) can render nested grouping without needing to understand the hierarchy itself.
/// </summary>
internal partial class CollectionViewGroup : DependencyObject, ICollectionViewGroup
{
    /// <summary>
    /// Gets the <see cref="TableViewGroupInfo"/> describing this group (its name, nesting level, item count,
    /// and expanded/collapsed state).
    /// </summary>
    public object? Group { get; init; }

    /// <summary>
    /// Gets the items belonging to this group. Empty for a non-leaf (parent) group or a collapsed group.
    /// </summary>
    public IObservableVector<object?>? GroupItems { get; init; }
}
#endif
