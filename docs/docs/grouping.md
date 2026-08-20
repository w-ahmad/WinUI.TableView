# Grouping

`TableView` supports multi-level column grouping: collapsing rows into collapsible headers by one or more column values, with expand/collapse control per group and full integration with column sorting.

> **Note:** Grouping is currently a **Windows-only** feature. It is not available when running on non-Windows Uno Platform targets (macOS, Linux, WASM, iOS, Android). `CanGroupColumns` and the "Group" column header option are hidden/no-ops on those targets.

## When to use it

Use grouping when users need to see data organized into categories - for example, orders grouped by status, or employees grouped by department and then by role. Combine it with [sorting](sorting.md) to control the order of both the groups and the items inside them.

## Basic example

Grouping is off by default. The simplest way to enable it is through the column header's options menu - open a column's options flyout and choose **Group**:

```xml
<tv:TableView ItemsSource="{x:Bind Products}" />
```

Clicking **Group** on the **Category** column collapses the rows into headers, one per distinct category value.

## Grouping programmatically

Grouping descriptions live on the underlying collection view, similar to [`SortDescriptions`](sorting.md). Because grouping is a Windows-only capability, the group-related API is exposed on the concrete `WinUI.TableView.CollectionView` type rather than on the `ICollectionView` interface returned by `TableView.CollectionView` - cast to it first:

```csharp
if (tableView.CollectionView is WinUI.TableView.CollectionView collectionView)
{
    collectionView.GroupDescriptions.Add(new GroupDescription("Category"));
}
```

Add a second `GroupDescription` to group by more than one level - each additional description nests inside the previous one:

```csharp
collectionView.GroupDescriptions.Add(new GroupDescription("Department"));
collectionView.GroupDescriptions.Add(new GroupDescription("Role"));
```

This produces a two-level hierarchy: a header per department, and inside each, a header per role.

Remove all grouping:

```csharp
tableView.UngroupAll();
```

Or clear the `GroupDescriptions` collection directly - both have the same effect, and both reset the sort indicator on any column that was driving a group.

Ungrouping and later re-grouping the same column starts every group at [`DefaultGroupState`](#default-expandedcollapsed-state) again - any groups a user had individually expanded or collapsed before ungrouping do not carry over.

## Group headers: name and count

By default, each group header shows the group's key and item count, e.g. `Electronics (12)`. This comes from `TableView.DefaultGroupStyle`, which is a plain `GroupStyle` applied automatically unless you add your own entry to `TableView.GroupStyle` (a `ListViewBase.GroupStyle` collection):

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.GroupStyle>
        <GroupStyle>
            <GroupStyle.HeaderTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <FontIcon Glyph="&#xE8B7;" />
                        <TextBlock Text="{Binding Key}" FontWeight="Bold" />
                        <TextBlock Text="{Binding Count}" Opacity="0.6" />
                    </StackPanel>
                </DataTemplate>
            </GroupStyle.HeaderTemplate>
        </GroupStyle>
    </tv:TableView.GroupStyle>
</tv:TableView>
```

The header's `DataContext` is a `TableViewGroupInfo` with these members:

| Member | Type | Description |
|---|---|---|
| `Key` | `object?` | The group's key value |
| `Count` | `int` | Total item count in the group, including nested subgroups |
| `IsExpanded` | `bool` | Whether the group's items are currently shown |

## Expanding and collapsing groups

Each group header has a built-in expand/collapse button. Clicking it toggles `IsExpanded` for that group and shows or hides its items (or, for a non-leaf group in a multi-level hierarchy, its descendant subgroups and items).

### Default expanded/collapsed state

Control whether newly created groups start out expanded or collapsed with `TableView.DefaultGroupState`:

```xml
<tv:TableView DefaultGroupState="Collapsed" />
```

| Value | Description |
|---|---|
| `Expanded` | Groups start expanded (all items visible) |
| `Collapsed` | Groups start collapsed (only headers visible) - the default |

Changing `DefaultGroupState` at runtime re-applies it to every group that has not been individually toggled away from the previous default. A group a user has explicitly expanded or collapsed keeps that state until toggled again.

### Toggling a group programmatically

Each entry in `CollectionGroups` implements the standard `ICollectionViewGroup` interface; its `Group` property is the `TableViewGroupInfo` whose `IsExpanded` you can set directly:

```csharp
var firstGroup = collectionView.CollectionGroups?
    .Select(g => ((ICollectionViewGroup)g).Group)
    .OfType<TableViewGroupInfo>()
    .FirstOrDefault();

if (firstGroup is not null)
{
    firstGroup.IsExpanded = !firstGroup.IsExpanded;
}
```

### Hiding the expand/collapse button

To let groups start collapsed/expanded via `DefaultGroupState` without exposing a user-facing toggle, hide the button with `ShowGroupExpandCollapseButton`:

```xml
<tv:TableView ShowGroupExpandCollapseButton="False" />
```

Groups can still be toggled programmatically while the button is hidden.

### Sticky group headers

Enable sticky group headers so the current group's header stays pinned to the top of the viewport while scrolling through its items:

```xml
<tv:TableView AreStickyGroupHeadersEnabled="True" />
```

> **Note:** This only pins a single level of header at a time - with multi-level grouping, ancestor headers above the innermost one do not stack.

## Sorting and grouping together

Grouping a column and sorting it are unified: grouping a column also drives its order, so a grouped column shows a sort direction indicator like any other sorted column, and clicking it toggles that direction instead of adding a separate sort.

- If the column already had its own sort applied when it's grouped, that sort description is kept (not discarded) and seeds the group's initial direction. Clicking the header keeps cycling through it three-state (ascending → descending → cleared) until it's eventually cleared - only then does ordering hand off fully to the group, which cycles two-state (ascending ↔ descending) from that point on. "Clear Sorting" is available up until that hand-off, as a shortcut past the three-state cycle.
- Grouping a column with no prior sort starts the group directly in two-state mode with a default ascending direction - there's no independent sort description to cycle through first.
- Either way, the chosen direction reorders both the group headers and the items inside each group.
- Ungrouping a column (via the header's **Ungroup** option, or `UngroupAll()`) removes any sort description tied to it and clears its sort direction indicator.

```csharp
// Groups by Category, then reorders both headers and each group's items descending
if (tableView.CollectionView is WinUI.TableView.CollectionView collectionView)
{
    var categoryGroup = collectionView.GroupDescriptions
        .OfType<GroupDescription>()
        .First();

    categoryGroup.Direction = SortDirection.Descending;
    collectionView.RefreshGrouping();
}
```

## Refreshing groups

Call `RefreshGrouping()` on the `WinUI.TableView.CollectionView` to rebuild groups from the current data without user interaction - useful after mutating data outside an `ObservableCollection`:

```csharp
collectionView.RefreshGrouping();
```

## Row and header indentation

For multi-level grouping, row cells and column headers automatically shift right by 24px for each grouping level beyond the first, keeping the deepest group header's content aligned with the columns beneath it. The first grouping level adds no indent.

## The Grouping event

Handle `TableView.Grouping` to intercept or supplement the built-in group logic when a column's **Group** option is used. Setting `e.Handled = true` prevents the default grouping from running:

```csharp
tableView.Grouping += (s, e) =>
{
    if (e.Column.Header?.ToString() == "Id")
    {
        // Don't allow grouping by Id
        e.Handled = true;
    }
};
```

`TableViewGroupingEventArgs` properties:

| Property | Description |
|---|---|
| `Column` | The column being grouped |
| `Handled` | Set `true` to suppress default grouping behavior |

## Checking grouping state

```csharp
bool isGrouped = tableView.IsGrouped; // true if any GroupDescription is applied
```

## Common options

| Property / Method / Event | Description |
|---|---|
| [`CanGroupColumns`](xref:WinUI.TableView.TableView.CanGroupColumns) | Enables or disables grouping for all columns |
| [`CanGroup`](xref:WinUI.TableView.TableViewColumn.CanGroup) | Per-column grouping toggle |
| `GroupDescriptions` (on `WinUI.TableView.CollectionView`) | Collection of active group descriptions |
| [`DefaultGroupStyle`](xref:WinUI.TableView.TableView.DefaultGroupStyle) | The `GroupStyle` applied when no custom `GroupStyle` entry is set |
| [`DefaultGroupState`](xref:WinUI.TableView.TableView.DefaultGroupState) | Whether new groups start expanded or collapsed |
| [`ShowGroupExpandCollapseButton`](xref:WinUI.TableView.TableView.ShowGroupExpandCollapseButton) | Shows or hides the per-group expand/collapse button |
| [`AreStickyGroupHeadersEnabled`](xref:WinUI.TableView.TableView.AreStickyGroupHeadersEnabled) | Pins the current group header to the top while scrolling |
| [`IsGrouped`](xref:WinUI.TableView.TableView.IsGrouped) | `true` if any grouping is applied |
| [`UngroupAll()`](xref:WinUI.TableView.TableView.UngroupAll) | Removes all grouping and resets affected columns' sort indicators |
| `RefreshGrouping()` (on `WinUI.TableView.CollectionView`) | Rebuilds groups from the current data without user interaction |
| [`Grouping`](xref:WinUI.TableView.TableView.Grouping) | Fires before the default group action runs; can be handled/suppressed |

## Notes and limitations

- Grouping is Windows-only; it does not run on non-Windows Uno Platform targets.
- A grouped column's order always needs a direction, so it never truly clears to unsorted the way an ungrouped column's third click does - see [Sorting and grouping together](#sorting-and-grouping-together) for the three-state-until-cleared-then-two-state cycle.
- Grouping and its implicit sort operate on the internal collection view. They do not mutate the original collection.
- Sticky group headers pin only one header level at a time.

## Related articles

- [Sorting](sorting.md)
- [Filtering](filtering.md)
- [Styling rows, cells, and headers](styling.md)
- [Events and commands reference](commands-events.md)
