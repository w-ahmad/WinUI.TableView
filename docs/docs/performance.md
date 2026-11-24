# Performance guidance

`TableView` hosts its rows in a virtualizing `ItemsRepeater`, so only the rows currently visible on screen exist as elements. The following guidance helps you get the best performance when working with large datasets.

## Row virtualization

Row virtualization is always active and needs no configuration. Only the rows in the visible viewport plus a small
cache exist as elements; scrolling recycles them rather than creating more.

The layout estimates the scroll extent from the rows it has realized instead of measuring every row, so the cost of
scrolling, of jumping to a distant row, and of reporting the extent are all independent of how many items there
are. Concretely, with the same viewport size, 1,000 items and 1,000,000 items realize the same number of row
elements.

### Selection scales with ranges, not items

Selected rows are stored as index ranges. Selecting every row of a million-row source is one range, and no row
needs to be realized for it. [`SelectedItems`](xref:WinUI.TableView.TableView.SelectedItems) is a projection over
those ranges rather than a materialised list, so reading its `Count` is cheap; enumerating it still walks the
selection, so avoid doing that on a hot path when the selection is large. Prefer
[`SelectedRanges`](xref:WinUI.TableView.TableView.SelectedRanges) or
[`IsRowSelected`](xref:WinUI.TableView.TableView.IsRowSelected*) when you only need to test membership.

Cell selection is stored the same way, as rectangles, so selecting all cells is one rectangle rather than one
entry per cell.

## Collection type

For the best performance with large collections:

- Use `List<T>` or `ObservableCollection<T>` as the source type.
- Avoid `IQueryable<T>` sources that trigger database queries for every property access.
- If your collection has tens of thousands of items, consider loading data in pages and using a virtualized source.

## INotifyPropertyChanged

Implement `INotifyPropertyChanged` on your model to ensure only cells whose data has changed are re-rendered. Without it, the control cannot detect property changes and may not update cell values.

## Live shaping

Live shaping re-evaluates sort and filter criteria when item properties change. This is convenient but has a cost on large collections:

```csharp
// Disable unless you need items to resort/refilter automatically (AllowLiveShaping is enabled by default)
tableView.AllowLiveShaping = false;
```

Enable it only when users expect items to move or disappear in real time after edits.

## Auto-generated columns

[`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) uses reflection to inspect the item type. For types with many properties, or in hot-path scenarios, prefer explicit columns to avoid reflection overhead:

```xml
<tv:TableView AutoGenerateColumns="False">
    <tv:TableView.Columns>
        <!-- Explicit columns -->
    </tv:TableView.Columns>
</tv:TableView>
```

## Conditional cell styles

Conditional style predicates are called for every rendered cell during layout passes. Keep predicates fast:

```csharp
// Good: simple property check
ctx.DataItem is Product p && p.Stock < 10

// Avoid: LINQ or string operations inside the predicate on a hot path
ctx.DataItem is Product p && p.Tags.Any(t => t.StartsWith("clearance"))
```

## Column auto-width

[`ColumnAutoWidthMode`](xref:WinUI.TableView.TableView.ColumnAutoWidthMode) measures cell content to determine the column width. On large virtualized lists, only visible cells are measured. This means the initial auto-width may be narrower than the actual maximum value width. If accuracy matters, consider using a fixed or star width instead.

## Filtering and sorting

Filtering and sorting operate on the internal collection view. These run on the UI thread. For very large collections (100,000+ items), consider pre-filtering in your ViewModel before setting [`ItemsSource`](xref:WinUI.TableView.TableView.ItemsSource).

## Grouping

Group headers are virtualized with the rows: only the headers and rows in the viewport are realized, and mapping between row indexes and display positions is a binary search over the visible groups, so scrolling a grouped table costs the same as an ungrouped one.

What does cost time is rebuilding the groups, which happens from the full filtered and sorted item set on every regroup, add, remove, or expand/collapse toggle. Collapsing a group removes its items from the view entirely, so collapsing large groups by default (`DefaultGroupState="Collapsed"`, the default) keeps both that rebuild and the initial render cheap for grouped data. See [Grouping](grouping.md).

## Refreshing the view after bulk data changes

When you modify items in your source collection in-place (e.g., changing a property without `INotifyPropertyChanged`, or replacing items in a `List<T>`) the view may not update automatically. Use the refresh methods to force the control to re-evaluate:

| Method | Description |
|---|---|
| [`RefreshView()`](xref:WinUI.TableView.TableView.RefreshView) | Re-renders the items view; use after bulk data changes |
| [`RefreshSorting()`](xref:WinUI.TableView.TableView.RefreshSorting) | Re-applies active sort descriptions without user interaction |
| [`RefreshFilter()`](xref:WinUI.TableView.TableView.RefreshFilter) | Re-evaluates active filter descriptions without user interaction |

```csharp
// After modifying items in bulk outside of ObservableCollection:
foreach (var item in products)
{
    item.Price *= 0.9; // Apply a discount
}
tableView.RefreshView();

// Re-apply sort after external data update:
tableView.RefreshSorting();

// Re-run filter after external data update:
tableView.RefreshFilter();
```

> **Tip**: Prefer `ObservableCollection<T>` with `INotifyPropertyChanged` models over manual refresh calls whenever possible, as it is more efficient and requires less code.

## Column resize drag performance

By default, dragging a column divider ([`ColumnResizeMode="Live"`](xref:WinUI.TableView.TableView.ColumnResizeMode)) relayouts every visible row's cells on every pointer-move frame. On grids with many visible rows this can make the drag itself feel less smooth, even though the final committed width is unaffected. Set `ColumnResizeMode="Preview"` to use a lightweight visual preview during the drag instead — no row layout runs until the pointer is released, so the drag stays smooth regardless of row count. See [Column sizing](column-sizing.md#columnresizemode).

## Horizontal scrolling and column count

Unlike rows, columns are not virtualized — every visible column produces a cell in every realized row. A very
large number of columns (100+) therefore costs `realized rows × columns` elements and may affect horizontal
scroll performance. In practice, most data grids have far fewer columns than rows.

The horizontal scroll extent is computed from the column widths rather than from whichever rows happen to be
realized, so the horizontal scrollbar does not change size as you scroll vertically.

## Garbage collection pauses on Windows

A WinUI app's blocking gen2 collections pause the process for longer the more XAML objects are alive, and a page
of tables is many thousands of them. A pause that lands mid-scroll shows as a dropped frame or a visible hitch.
This is the app's to tune, not the control's, and both of the following are optional. The Sample App does both,
which is worth knowing when you compare its scrolling with your own app's:

- **Sustained low latency.** `GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency`, set once at startup,
  has the runtime prefer background collections over blocking ones. The cost is a larger heap between collections.
- **Collect after navigating.** Leaving a page frees a lot at once, and the collection that follows tends to land
  in the first scroll on the next page. Calling `GC.Collect()` a moment after navigation (the Sample App waits
  500 ms) takes that pause while nothing is moving.

Measure before adopting either: they trade memory and an explicit pause for smoother scrolling, and an app with
few XAML objects alive gains nothing from them.

## Uno Platform

On Uno Platform targets, data binding and layout passes may have slightly different performance characteristics than on the Windows target. Test performance on each target platform before shipping.

## Notes

- `TableView` supports incremental loading when the items source implements `ISupportIncrementalLoading`. See [Incremental loading](incremental-loading.md).
- The `CellsHorizontalOffset` property (default `16`) adds padding to the left of the cells area. This is separate from column widths.

## Related articles

- [Binding data](binding-data.md)
- [Filtering](filtering.md)
- [Sorting](sorting.md)
- [Grouping](grouping.md)
- [Conditional cell styling](conditional-styling.md)
