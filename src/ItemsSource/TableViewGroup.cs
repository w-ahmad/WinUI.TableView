using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Represents one group of items produced by the <see cref="TableView"/>'s grouping.
/// </summary>
/// <remarks>
/// A group is a span of consecutive items in the item view, described by <see cref="FirstItemIndex"/> and
/// <see cref="ItemCount"/> rather than by holding the items. Nothing is copied, so grouping a million rows costs
/// memory proportional to the number of groups, and a group header row can be realized from a group without
/// touching the items it covers.
/// </remarks>
public sealed class TableViewGroup
{
    private readonly object?[] _keyPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroup"/> class.
    /// </summary>
    internal TableViewGroup(object? key, int level, int firstItemIndex, TableViewGroup? parent)
    {
        Key = key;
        Level = level;
        FirstItemIndex = firstItemIndex;
        Parent = parent;

        _keyPath = parent is null ? [key] : [.. parent.KeyPath, key];
    }

    /// <summary>
    /// Gets the group key: the value the group description produced for the items in this group.
    /// </summary>
    public object? Key { get; }

    /// <summary>
    /// Gets the zero-based grouping level, matching the index of the owning group description.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the enclosing group, or <see langword="null"/> for a top-level group.
    /// </summary>
    public TableViewGroup? Parent { get; }

    /// <summary>
    /// Gets the group keys from the top-level group down to and including this one.
    /// </summary>
    public IReadOnlyList<object?> KeyPath => _keyPath;

    /// <summary>
    /// Gets the index, in the item view, of the first item this group covers.
    /// </summary>
    public int FirstItemIndex { get; internal set; }

    /// <summary>
    /// Gets the number of items this group covers, including those covered by nested groups.
    /// </summary>
    public int ItemCount { get; internal set; }

    /// <summary>
    /// Gets the index, in the item view, of the last item this group covers.
    /// </summary>
    public int LastItemIndex => FirstItemIndex + ItemCount - 1;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Key} (level {Level}, {ItemCount} items from {FirstItemIndex})";
    }
}
