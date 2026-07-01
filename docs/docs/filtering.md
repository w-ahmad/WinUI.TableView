# Filtering

`TableView` provides an Excel-like column filter flyout that lets users select which values to show in each column. Filtering is built in and requires no extra code for the standard case.

## When to use it

Use filtering when users need to narrow down a large list to items that match specific values in one or more columns. For example, filtering a product list to show only items that are "In Stock", or filtering an order list to a specific customer.

## Basic example

Filtering is on by default. Click the options button in a column header to open the filter flyout:

```xml
<tv:TableView ItemsSource="{x:Bind Products}" />
```

## Opening the filter flyout

By default, the column filter flyout opens from the options button in the column header. You can optionally open it with a right-click:

```xml
<tv:TableView UseRightClickForColumnFilter="True" />
```

## Disabling filtering

Disable filtering for the entire table:

```xml
<tv:TableView CanFilterColumns="False" />
```

Disable filtering for a specific column:

```xml
<tv:TableViewTextColumn Header="ID" Binding="{Binding Id}" CanFilter="False" />
```

## Showing item counts in the filter flyout

Show how many rows match each filter value:

```xml
<tv:TableView ShowFilterItemsCount="True" />
```

## Filtering programmatically

Add filter descriptions to `TableView.FilterDescriptions`. This uses the `AdvancedCollectionView` from the CommunityToolkit:

```csharp
using CommunityToolkit.WinUI.Collections;

// Show only in-stock items
tableView.FilterDescriptions.Add(
    new FilterDescription("InStock", new PredicateFilter(v => v is true)));

// Clear all filters
tableView.ClearAllFilters();
```

## Clearing and refreshing filters programmatically

Use [`ClearAllFilters()`](xref:WinUI.TableView.TableView.ClearAllFilters) to remove all active filter descriptions and reset every column's filter state in one call:

```csharp
tableView.ClearAllFilters();
```

Use [`RefreshFilter()`](xref:WinUI.TableView.TableView.RefreshFilter) to re-evaluate the current filter against the data without user interaction. Call this after modifying source data in-place so the view reflects the latest values:

```csharp
// Re-run the current filter after data changed in-place
tableView.RefreshFilter();
```

## Custom filter handler

The [`FilterHandler`](xref:WinUI.TableView.TableView.FilterHandler) property accepts an `IColumnFilterHandler` implementation. This allows you to replace the built-in filter logic entirely:

```csharp
public class MyFilterHandler : IColumnFilterHandler
{
    // Implement IColumnFilterHandler members
}

tableView.FilterHandler = new MyFilterHandler();
```

## Checking filter state

```csharp
bool isFiltered = tableView.IsFiltered;
// true if any FilterDescription is active OR any column reports IsFiltered = true
```

## Common options

| Property | Description |
|---|---|
| [`CanFilterColumns`](xref:WinUI.TableView.TableView.CanFilterColumns) | Enables or disables filtering for all columns (default `true`) |
| [`CanFilter`](xref:WinUI.TableView.TableViewColumn.CanFilter) | Per-column filter toggle |
| [`IsFiltered`](xref:WinUI.TableView.TableViewColumn.IsFiltered) | `true` if a filter is active on this column |
| [`FilterDescriptions`](xref:WinUI.TableView.TableView.FilterDescriptions) | The collection of active filter descriptions |
| [`IsFiltered`](xref:WinUI.TableView.TableView.IsFiltered) | `true` if any filter is applied to the view |
| [`ClearAllFilters()`](xref:WinUI.TableView.TableView.ClearAllFilters) | Removes all filter descriptions and resets column filter state |
| [`RefreshFilter()`](xref:WinUI.TableView.TableView.RefreshFilter) | Re-evaluates current filter descriptions without user interaction |
| [`ShowFilterItemsCount`](xref:WinUI.TableView.TableView.ShowFilterItemsCount) | Shows the count of matching rows next to each filter value |
| [`UseRightClickForColumnFilter`](xref:WinUI.TableView.TableView.UseRightClickForColumnFilter) | Opens the filter flyout on column header right-click |
| [`FilterHandler`](xref:WinUI.TableView.TableView.FilterHandler) | Custom filter handler implementation |

## Notes and limitations

- Filtering is applied to the internal `AdvancedCollectionView`. It does not mutate the original collection.
- [`TableViewTemplateColumn`](xref:WinUI.TableView.TableViewTemplateColumn) has `CanFilter = false` by default. Set [`OperationContentBinding`](xref:WinUI.TableView.TableViewColumn.OperationContentBinding) to enable filtering on template columns.
- When a filter is active, new items added to the source collection may not appear in the view until the filter is re-evaluated or live shaping is enabled (`AllowLiveShaping = true`).

## Related articles

- [Sorting](sorting.md)
- [Binding data](binding-data.md)
- [Performance guidance](performance.md)
