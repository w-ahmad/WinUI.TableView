# Overview

WinUI.TableView is a data grid control for WinUI and Uno Platform applications. It provides a familiar, Excel-like tabular UI for displaying, sorting, filtering, and editing structured data.

## Architecture

`TableView` derives from `Control` and owns its own row hosting: rows live in an `ItemsRepeater` driven by a
TableView-specific virtualizing layout. That means the control — not a base class — decides which rows exist,
where they are, how selection is stored, and how a point on screen maps to a row and column.

```
TableView (derives from Control)
├── TableViewHeaderRow             ← sticky header above the scrollable content
│   └── TableViewColumnHeader      ← one per column; handles sort, filter, resize, reorder
├── ScrollViewer
│   └── ItemsRepeater              ← hosts only the rows the viewport needs
│       └── TableViewRow           ← one per *visible* item, recycled as you scroll
│           ├── TableViewRowHeader ← optional left gutter per row
│           ├── TableViewCell[]    ← one per column
│           └── RowDetails panel   ← optional collapsible detail area
└── selection model                ← selected rows stored as index ranges
```

### What this buys you

- The number of realized `TableViewRow` elements is proportional to the viewport height, not the item count. A
  thousand items and a million items produce the same number of row elements.
- Selecting a large range is a range operation: no row has to exist for it, and
  [`SelectedItems`](xref:WinUI.TableView.TableView.SelectedItems) projects onto the items on demand.
- Hit testing during drag selection is index arithmetic rather than a scan over rows.

## Key concepts

### Columns

Each column is an object that lives in `TableView.Columns`. Columns define:

- which data property to display ([`Binding`](xref:WinUI.TableView.TableViewBoundColumn.Binding))
- which editing control to show when a cell enters edit mode
- styling, sizing, and sorting behavior

All built-in column types inherit from [`TableViewColumn`](xref:WinUI.TableView.TableViewColumn). Columns that bind to a data property inherit from [`TableViewBoundColumn`](xref:WinUI.TableView.TableViewBoundColumn).

See [Column types](column-types.md) and [Defining columns](defining-columns.md).

### Items source

`TableView.ItemsSource` accepts any `IEnumerable`. Internally the control wraps the source in its own collection view that supports sorting, filtering, and live shaping without mutating the original collection.

See [Binding data](binding-data.md).

### Selection

The control supports row selection, cell selection, or both, through [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) and [`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit). Selected cells are tracked via [`TableViewCellSlot`](xref:WinUI.TableView.TableViewCellSlot) (a `(Row, Column)` record struct).

See [Selection](selection.md).

### Editing

Double-tapping a cell, or pressing F2, enters edit mode for that cell. The column's `GenerateEditingElement` method provides the editing control. The lifecycle fires [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit), [`PreparingCellForEdit`](xref:WinUI.TableView.TableView.PreparingCellForEdit), [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding), and [`CellEditEnded`](xref:WinUI.TableView.TableView.CellEditEnded).

See [Editing](editing.md).

### Collection view

`TableView` exposes a [`CollectionView`](xref:WinUI.TableView.TableView.CollectionView) property that gives direct access to the internal `ICollectionView`. You can programmatically add sort or filter descriptions to this view.

```csharp
tableView.SortDescriptions.Add(new SortDescription("Price", SortDirection.Ascending));
tableView.FilterDescriptions.Add(new FilterDescription("Name", new PredicateFilter(x => x.ToString()!.StartsWith("A"))));
```

## Namespace and XAML prefix

```xml
xmlns:tv="using:WinUI.TableView"
```

All types are in the `WinUI.TableView` namespace.

## Uno Platform

On Uno Platform targets some behaviors differ slightly from the Windows target, particularly around data binding and focus management. See [Getting started with Uno](getting-started-with-uno.md) for platform-specific notes.

## Related articles

- [Getting started](getting-started.md)
- [Binding data](binding-data.md)
- [Defining columns](defining-columns.md)
- [Column types](column-types.md)
