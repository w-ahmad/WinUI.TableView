using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Grouping for <see cref="TableView"/>.
/// </summary>
public partial class TableView
{
    private readonly HashSet<TableViewGroupPath> _collapsedGroups = [];

    /// <summary>
    /// Identifies the <see cref="GroupHeaderTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GroupHeaderTemplateProperty = DependencyProperty.Register(
        nameof(GroupHeaderTemplate), typeof(DataTemplate), typeof(TableView), new PropertyMetadata(null, OnGroupHeaderTemplateChanged));

    /// <summary>
    /// Identifies the <see cref="GroupHeaderTemplateSelector"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GroupHeaderTemplateSelectorProperty = DependencyProperty.Register(
        nameof(GroupHeaderTemplateSelector), typeof(DataTemplateSelector), typeof(TableView), new PropertyMetadata(null, OnGroupHeaderTemplateChanged));

    /// <summary>
    /// Identifies the <see cref="AreGroupsExpandedByDefault"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AreGroupsExpandedByDefaultProperty = DependencyProperty.Register(
        nameof(AreGroupsExpandedByDefault), typeof(bool), typeof(TableView), new PropertyMetadata(true));

    /// <summary>
    /// Gets the collection of group descriptions applied to the items, outermost level first.
    /// </summary>
    public IList<GroupDescription> GroupDescriptions => _collectionView.GroupDescriptions;

    /// <summary>
    /// Gets a value indicating whether the items are grouped.
    /// </summary>
    public bool IsGrouped => _collectionView.IsGrouped;

    /// <summary>
    /// Gets the groups currently produced by the grouping, in the order their header rows appear.
    /// </summary>
    public IReadOnlyList<TableViewGroup> Groups => _collectionView.Groups;

    /// <summary>
    /// Gets or sets the template used to present a group header's key.
    /// </summary>
    public DataTemplate? GroupHeaderTemplate
    {
        get => (DataTemplate?)GetValue(GroupHeaderTemplateProperty);
        set => SetValue(GroupHeaderTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template selector used to present a group header's key.
    /// </summary>
    public DataTemplateSelector? GroupHeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(GroupHeaderTemplateSelectorProperty);
        set => SetValue(GroupHeaderTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether groups start out expanded.
    /// </summary>
    public bool AreGroupsExpandedByDefault
    {
        get => (bool)GetValue(AreGroupsExpandedByDefaultProperty);
        set => SetValue(AreGroupsExpandedByDefaultProperty, value);
    }

    /// <summary>
    /// Re-applies the group header template to the realized group header rows.
    /// </summary>
    private static void OnGroupHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            // The template is resolved per group as the header row is prepared, so a full reprojection is the
            // simplest way to push a new template onto the headers that are already on screen.
            tableView.RefreshVisualRows();
        }
    }

    /// <summary>
    /// Re-evaluates the grouping applied to the items.
    /// </summary>
    public void RefreshGrouping()
    {
        _collectionView.RefreshGrouping();
    }

    /// <summary>
    /// Shows the contents of the specified group.
    /// </summary>
    /// <param name="group">The group to expand.</param>
    public void ExpandGroup(TableViewGroup group)
    {
        SetGroupExpanded(group, isExpanded: true);
    }

    /// <summary>
    /// Hides the contents of the specified group.
    /// </summary>
    /// <param name="group">The group to collapse.</param>
    public void CollapseGroup(TableViewGroup group)
    {
        SetGroupExpanded(group, isExpanded: false);
    }

    /// <summary>
    /// Shows the contents of every group.
    /// </summary>
    public void ExpandAllGroups()
    {
        if (_collapsedGroups.Count is 0)
        {
            return;
        }

        _collapsedGroups.Clear();
        RefreshVisualRows();
    }

    /// <summary>
    /// Hides the contents of every group.
    /// </summary>
    public void CollapseAllGroups()
    {
        var changed = false;

        foreach (var group in _collectionView.Groups)
        {
            changed |= _collapsedGroups.Add(new TableViewGroupPath(group.KeyPath));
        }

        if (changed)
        {
            RefreshVisualRows();
        }
    }

    /// <summary>
    /// Determines whether the specified group's contents are hidden.
    /// </summary>
    internal bool IsGroupCollapsed(TableViewGroup group)
    {
        return _collapsedGroups.Count > 0 && _collapsedGroups.Contains(new TableViewGroupPath(group.KeyPath));
    }

    /// <summary>
    /// Expands the specified group when collapsed and collapses it when expanded.
    /// </summary>
    internal void ToggleGroup(TableViewGroup group)
    {
        SetGroupExpanded(group, IsGroupCollapsed(group));
    }

    /// <summary>
    /// Resolves the template a group header row should present its key with.
    /// </summary>
    internal DataTemplate? ResolveGroupHeaderTemplate(TableViewGroup group)
    {
        return GroupHeaderTemplateSelector?.SelectTemplate(group) ?? GroupHeaderTemplate;
    }

    /// <summary>
    /// Expands or collapses a group.
    /// </summary>
    /// <remarks>
    /// A group's header row sits above everything the collapse adds or removes, so its own offset does not move
    /// and it stays put on screen without any scroll correction. Only the content below it shifts, which is what
    /// collapsing a group is supposed to look like.
    /// </remarks>
    private void SetGroupExpanded(TableViewGroup group, bool isExpanded)
    {
        var path = new TableViewGroupPath(group.KeyPath);
        var changed = isExpanded ? _collapsedGroups.Remove(path) : _collapsedGroups.Add(path);

        if (changed)
        {
            RefreshVisualRows();
        }
    }
}
