# Sorting

`TableView` has built-in column sorting. Clicking a column header cycles the sort direction for that column: ascending → descending → cleared.

## When to use it

Use sorting whenever users need to find the highest or lowest value in a column, or locate items alphabetically. Sorting is enabled by default and requires no extra code for the standard use case.

## Basic example

Sorting is on by default. No extra configuration is needed:

```xml
<tv:TableView ItemsSource="{x:Bind Products}" />
```

Clicking the **Price** column header sorts the rows by price.

## Disabling sorting

Disable sorting for the entire table:

```xml
<tv:TableView CanSortColumns="False" />
```

Disable sorting for a specific column:

```xml
<tv:TableViewNumberColumn Header="Price" Binding="{Binding Price}" CanSort="False" />
```

## Sorting programmatically

Add sort descriptions to `TableView.SortDescriptions`:

```csharp
// Sort by price ascending
tableView.SortDescriptions.Add(new SortDescription("Price", SortDirection.Ascending));

// Sort by name then price
tableView.SortDescriptions.Clear();
tableView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
tableView.SortDescriptions.Add(new SortDescription("Price", SortDirection.Ascending));
```

The [`SortDescriptions`](xref:WinUI.TableView.TableView.SortDescriptions) collection comes from the internal `AdvancedCollectionView`. Changes take effect immediately.

Clear all sorting:

```csharp
tableView.SortDescriptions.Clear();
```

## The Sorting event

Handle the [`Sorting`](xref:WinUI.TableView.TableView.Sorting) event to replace or supplement the built-in sort logic. Setting `e.Handled = true` prevents the default sort from running:

```csharp
tableView.Sorting += (s, e) =>
{
    // Custom sort: case-insensitive name sort
    if (e.Column.Header?.ToString() == "Name")
    {
        var direction = e.Column.SortDirection == SortDirection.Ascending
            ? SortDirection.Descending
            : SortDirection.Ascending;

        tableView.SortDescriptions.Clear();
        tableView.SortDescriptions.Add(new SortDescription(
            "Name",
            direction,
            StringComparer.OrdinalIgnoreCase));

        e.Column.SortDirection = direction;
        e.Handled = true;
    }
};
```

[`TableViewSortingEventArgs`](xref:WinUI.TableView.TableViewSortingEventArgs) properties:

| Property | Description |
|---|---|
| `Column` | The column being sorted |
| `Handled` | Set `true` to suppress default sort behavior |

## The ClearSorting event

[`ClearSorting`](xref:WinUI.TableView.TableView.ClearSorting) fires when the sort is removed from a column (third click cycles back to no sort):

```csharp
tableView.ClearSorting += (s, e) =>
{
    Console.WriteLine($"Sort cleared on column: {e.Column.Header}");
};
```

[`TableViewClearSortingEventArgs`](xref:WinUI.TableView.TableViewClearSortingEventArgs) properties:

| Property | Description |
|---|---|
| `Column` | The column whose sort was cleared |

## Multi-column sort

Ctrl+Click a column header to add it as a secondary sort. The sort indicators show numbers when multiple columns are sorted.

## Checking sort state

```csharp
bool isSorted = tableView.IsSorted; // true if any column has a sort direction
var descriptions = tableView.SortDescriptions; // the active SortDescription list
```

## Common options

| Property / Event | Description |
|---|---|
| [`CanSortColumns`](xref:WinUI.TableView.TableView.CanSortColumns) | Enables or disables sorting for all columns |
| `TableViewColumn.CanSort` | Per-column sort toggle |
| `TableViewColumn.SortDirection` | Current sort direction (`Ascending`, `Descending`, or `null`) |
| [`SortDescriptions`](xref:WinUI.TableView.TableView.SortDescriptions) | Collection of active sort descriptions |
| [`IsSorted`](xref:WinUI.TableView.TableView.IsSorted) | `true` if any sort is applied |
| [`Sorting`](xref:WinUI.TableView.TableView.Sorting) | Fires before the default sort runs; can be cancelled |
| [`ClearSorting`](xref:WinUI.TableView.TableView.ClearSorting) | Fires when a column's sort is cleared |

## Notes and limitations

- [`TableViewTemplateColumn`](xref:WinUI.TableView.TableViewTemplateColumn) has `CanSort = false` by default because there is no bound property path. Set [`OperationContentBinding`](xref:WinUI.TableView.TableViewColumn.OperationContentBinding) to enable sorting on template columns.
- Sorting is applied to the internal `AdvancedCollectionView`. It does not mutate the original collection.

## Related articles

- [Filtering](filtering.md)
- [Binding data](binding-data.md)
- [Defining columns](defining-columns.md)
