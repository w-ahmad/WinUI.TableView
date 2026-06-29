# Frozen columns

Frozen columns stay in place on the left side of the table while the user scrolls the remaining columns horizontally. They are useful for keeping identifying information (such as a name or ID) visible at all times.

## When to use it

Use frozen columns when the table has more columns than fit on screen and the leftmost columns contain key identifying information that the user needs to keep in view while scrolling right.

## Basic example

Set `FrozenColumnCount` to the number of columns to freeze from the left:

```xml
<tv:TableView ItemsSource="{x:Bind Employees}"
              FrozenColumnCount="2"
              AutoGenerateColumns="False">
    <tv:TableView.Columns>
        <!-- These two columns will be frozen -->
        <tv:TableViewTextColumn Header="ID"         Binding="{Binding Id}"         Width="60" />
        <tv:TableViewTextColumn Header="Full Name"  Binding="{Binding FullName}"   Width="160" />
        <!-- These columns scroll -->
        <tv:TableViewTextColumn   Header="Department"  Binding="{Binding Department}" />
        <tv:TableViewTextColumn   Header="Title"       Binding="{Binding Title}" />
        <tv:TableViewNumberColumn Header="Salary"      Binding="{Binding Salary}" />
        <tv:TableViewDateColumn   Header="Hire Date"   Binding="{Binding HireDate}" />
        <tv:TableViewTextColumn   Header="Location"    Binding="{Binding Location}" />
    </tv:TableView.Columns>
</tv:TableView>
```

When the user scrolls right, the **ID** and **Full Name** columns stay fixed.

## Setting frozen column count in code

```csharp
tableView.FrozenColumnCount = 1;
```

Setting `FrozenColumnCount` to `0` (the default) unfreezes all columns.

## Common options

| Property | Type | Default | Description |
|---|---|---|---|
| `FrozenColumnCount` | `int` | `0` | The number of columns to freeze from the left |

## Notes and limitations

- `FrozenColumnCount` counts from the left of the displayed column order. If column reordering is enabled, the frozen columns are still the first `N` columns in the current display order.
- The row header (if visible) is always frozen independently of `FrozenColumnCount`.
- Frozen columns cannot be moved past the frozen boundary by dragging unless `FrozenColumnCount` is changed in code.
- Setting `FrozenColumnCount` larger than the total number of columns has no effect.

## Related articles

- [Column reordering](column-reordering.md)
- [Column sizing](column-sizing.md)
- [Defining columns](defining-columns.md)
