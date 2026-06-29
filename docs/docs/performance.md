# Performance guidance

`TableView` is built on `ListView`, which provides UI virtualization out of the box. This means only the rows currently visible on screen are instantiated. The following guidance helps you get the best performance when working with large datasets.

## Row virtualization

Row virtualization is always active. `TableView` does not render all items at once; only the rows in the visible viewport (plus a small buffer) are created. When the user scrolls, rows are recycled.

You do not need to do anything to enable virtualization — it is the default behavior inherited from `ListView`.

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
// Disable unless you need items to resort/refilter automatically
tableView.AllowLiveShaping = false; // (false is the default)
```

Enable it only when users expect items to move or disappear in real time after edits.

## Auto-generated columns

`AutoGenerateColumns` uses reflection to inspect the item type. For types with many properties, or in hot-path scenarios, prefer explicit columns to avoid reflection overhead:

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

`ColumnAutoWidthMode` measures cell content to determine the column width. On large virtualized lists, only visible cells are measured. This means the initial auto-width may be narrower than the actual maximum value width. If accuracy matters, consider using a fixed or star width instead.

## Filtering and sorting

Filtering and sorting operate on the internal `AdvancedCollectionView`. These run on the UI thread. For very large collections (100,000+ items), consider pre-filtering in your ViewModel before setting `ItemsSource`.

## Horizontal scrolling and column count

Unlike rows, columns are not virtualized — all column headers are instantiated regardless of whether they are visible. A very large number of columns (100+) may affect horizontal scroll performance. In practice, most data grids have far fewer columns than rows.

## Uno Platform

On Uno Platform targets, data binding and layout passes may have slightly different performance characteristics than on the Windows WinUI 3 target. Test performance on each target platform before shipping.

## Notes

- `TableView` does not support incremental loading (i.e., `ISupportIncrementalLoading`) directly. Load pages of data manually and add them to your `ObservableCollection`.
- The `CellsHorizontalOffset` property (default `16`) adds padding to the left of the cells area. This is separate from column widths.

## Related articles

- [Binding data](binding-data.md)
- [Filtering](filtering.md)
- [Sorting](sorting.md)
- [Conditional cell styling](conditional-styling.md)
