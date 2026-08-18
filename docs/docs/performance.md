# Performance guidance

`TableView` hosts its rows in an `ItemsRepeater` with a virtualizing layout it owns, so only the rows currently visible on screen exist as elements. The following guidance helps you get the best performance when working with large datasets.

## Row virtualization

Row virtualization is always active and needs no configuration. Only the rows in the visible viewport plus a small
cache exist as elements; scrolling recycles them rather than creating more.

The layout computes the scroll extent from the row heights arithmetically instead of measuring rows, so the cost of
scrolling, of jumping to a distant row, and of reporting the extent are all independent of how many items there
are. Concretely, with the same viewport size, 1,000 items and 1,000,000 items realize the same number of row
elements.

### Selection scales with ranges, not items

Selected rows are stored as index ranges. Selecting every row of a million-row source is one range, and no row
needs to be realized for it. [`SelectedItems`](xref:WinUI.TableView.TableView.SelectedItems) is a projection over
those ranges rather than a materialised list, so reading its `Count` is cheap; enumerating it still walks the
selection, so avoid doing that on a hot path when the selection is large. Prefer
[`SelectedRanges`](xref:WinUI.TableView.TableView.SelectedRanges) or
[`IsRowSelected`](xref:WinUI.TableView.TableView.IsRowSelected) when you only need to test membership.

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

## Uno Platform

On Uno Platform targets, data binding and layout passes may have slightly different performance characteristics than on the Windows target. Test performance on each target platform before shipping.

## Notes

- `TableView` supports incremental loading when the items source implements `ISupportIncrementalLoading`. See [Incremental loading](incremental-loading.md).
- The `CellsHorizontalOffset` property (default `16`) adds padding to the left of the cells area. This is separate from column widths.

## Related articles

- [Binding data](binding-data.md)
- [Filtering](filtering.md)
- [Sorting](sorting.md)
- [Conditional cell styling](conditional-styling.md)
