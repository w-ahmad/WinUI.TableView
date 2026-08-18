# Grouping

`TableView` can group rows by one or more properties. Group header rows are interleaved with the data rows and are
virtualized like any other row, so collapsing a group that contains thousands of items costs a single row element.

## Grouping by a property

Add a [`GroupDescription`](xref:WinUI.TableView.GroupDescription) to
[`GroupDescriptions`](xref:WinUI.TableView.TableView.GroupDescriptions):

```csharp
tableView.GroupDescriptions.Add(new GroupDescription(nameof(Employee.Department)));
```

Group descriptions are applied before the sort descriptions, so items land grouped and are then sorted within each
group:

```csharp
tableView.GroupDescriptions.Add(new GroupDescription(nameof(Employee.Department)));
tableView.SortDescriptions.Add(new SortDescription(nameof(Employee.LastName), SortDirection.Ascending));
```

### Multiple levels

Add one description per level, outermost first:

```csharp
tableView.GroupDescriptions.Add(new GroupDescription(nameof(Employee.Department)));
tableView.GroupDescriptions.Add(new GroupDescription(nameof(Employee.Location)));
```

### Ordering and custom keys

`GroupDescription` derives from [`SortDescription`](xref:WinUI.TableView.SortDescription), so the same options
apply: a direction, a custom `IComparer`, or a delegate that produces the key.

```csharp
// Order the groups descending.
tableView.GroupDescriptions.Add(new GroupDescription(nameof(Order.Status), SortDirection.Descending));

// Group by a computed key rather than a property.
tableView.GroupDescriptions.Add(new GroupDescription(
    propertyName: null,
    valueDelegate: item => ((Order)item!).Placed.Year));
```

## Customising the header

[`GroupHeaderTemplate`](xref:WinUI.TableView.TableView.GroupHeaderTemplate) presents the group. Its data context is
the [`TableViewGroup`](xref:WinUI.TableView.TableViewGroup):

```xml
<tv:TableView ItemsSource="{x:Bind Employees}">
    <tv:TableView.GroupHeaderTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{Binding Key}" FontWeight="SemiBold" />
                <TextBlock Text="{Binding ItemCount}" Opacity="0.7" />
            </StackPanel>
        </DataTemplate>
    </tv:TableView.GroupHeaderTemplate>
</tv:TableView>
```

Use [`GroupHeaderTemplateSelector`](xref:WinUI.TableView.TableView.GroupHeaderTemplateSelector) to vary the
template, for example by [`Level`](xref:WinUI.TableView.TableViewGroup.Level).

## Expanding and collapsing

Users toggle a group by clicking its header, or by pressing Space or Enter when it has focus. In code:

```csharp
tableView.CollapseGroup(tableView.Groups[0]);
tableView.ExpandGroup(tableView.Groups[0]);

tableView.CollapseAllGroups();
tableView.ExpandAllGroups();
```

Set [`AreGroupsExpandedByDefault`](xref:WinUI.TableView.TableView.AreGroupsExpandedByDefault) to `false` to start
collapsed.

Collapse state is remembered by the group's key path rather than by position, so it survives re-sorting,
re-filtering and re-grouping. Only collapsed groups are tracked, so the common all-expanded case costs nothing.

## Inspecting the groups

[`Groups`](xref:WinUI.TableView.TableView.Groups) returns the groups in the order their headers appear — a group
always precedes the groups and items it contains:

```csharp
foreach (var group in tableView.Groups)
{
    Debug.WriteLine($"{new string(' ', group.Level * 2)}{group.Key}: {group.ItemCount} items");
}
```

A `TableViewGroup` describes a *span* of items rather than holding them:

| Member | Description |
|---|---|
| [`Key`](xref:WinUI.TableView.TableViewGroup.Key) | The value the group description produced |
| [`KeyPath`](xref:WinUI.TableView.TableViewGroup.KeyPath) | The keys from the outermost group down to this one |
| [`Level`](xref:WinUI.TableView.TableViewGroup.Level) | Zero-based nesting level |
| [`Parent`](xref:WinUI.TableView.TableViewGroup.Parent) | The enclosing group, or `null` |
| [`FirstItemIndex`](xref:WinUI.TableView.TableViewGroup.FirstItemIndex) / [`ItemCount`](xref:WinUI.TableView.TableViewGroup.ItemCount) | The span of [`Items`](xref:WinUI.TableView.TableView.Items) the group covers |

Because nothing is copied, grouping a million rows costs memory proportional to the number of groups.

## Grouping and the rest of the control

- **Selection and cell slots are unaffected.** They address items, not displayed rows, so collapsing a group never
  changes which items are selected or what a [`TableViewCellSlot`](xref:WinUI.TableView.TableViewCellSlot) points
  at.
- **Group headers are not selectable** and carry no cell slot. Arrow-key navigation moves between data rows and
  steps over them.
- **Alternate row colours** follow the data row index, so the striping does not re-phase when a group above is
  collapsed.
- **Live shaping moves items between groups.** If an item's group key changes and
  [`AllowLiveShaping`](xref:WinUI.TableView.TableView.AllowLiveShaping) is on, it moves to the right group; empty
  groups disappear.
- **Group headers do not scroll horizontally** with the columns.

## Removing grouping

```csharp
tableView.GroupDescriptions.Clear();
```

## Related articles

- [Sorting](sorting.md)
- [Filtering](filtering.md)
- [Selection](selection.md)
- [Performance guidance](performance.md)
