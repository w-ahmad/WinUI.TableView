using Microsoft.UI.Xaml;

namespace WinUI.TableView;

/// <summary>
/// Describes a single group header shown by a <see cref="TableViewGroupHeaderRow"/> - its display name, item
/// count, nesting level, and expanded/collapsed state. This is the object bound as a <see cref="CollectionViewGroup"/>'s
/// <see cref="CollectionViewGroup.Group"/>.
/// </summary>
public partial class TableViewGroupInfo : DependencyObject
{
    private readonly object? _key;
    private readonly int _count;
    private bool _isPublished;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroupInfo"/> class.
    /// </summary>
    public TableViewGroupInfo()
    {
    }

    /// <summary>
    /// Creates a group whose <see cref="Key"/> and <see cref="Count"/> reach their dependency properties on demand.
    /// </summary>
    internal TableViewGroupInfo(object? key, int count)
    {
        _key = key;
        _count = count;
    }

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
    public object? Key
    {
        get => _key;
        init
        {
            _key = value;
            SetValue(KeyProperty, value);
        }
    }

    /// <summary>
    /// Gets the number of items in this group (including all descendant subgroups/items, if any).
    /// </summary>
    public int Count
    {
        get => _count;
        init
        {
            _count = value;
            SetValue(CountProperty, value);
        }
    }

    /// <summary>
    /// Copies <see cref="Key"/> and <see cref="Count"/> to their dependency properties, which is where a binding
    /// reads them. Deferred until a group the library built is handed out, since every rebuild creates all groups anew.
    /// </summary>
    internal void PublishBindableValues()
    {
        if (!_isPublished)
        {
            _isPublished = true;
            SetValue(KeyProperty, Key);
            SetValue(CountProperty, Count);
        }
    }

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
    /// Identifies the <see cref="Key"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(nameof(Key), typeof(object), typeof(TableViewGroupInfo), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the <see cref="Count"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(nameof(Count), typeof(int), typeof(TableViewGroupInfo), new PropertyMetadata(0));

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
