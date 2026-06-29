# Overview

WinUI.TableView is a data grid control for WinUI 3 and Uno Platform applications. It provides a familiar, Excel-like tabular UI for displaying, sorting, filtering, and editing structured data.

## Architecture

`TableView` inherits from `ListView`. This gives it native WinUI scrolling, virtualization, selection, and accessibility behavior out of the box. The control adds its own header row, column definitions, cell rendering, and interaction layer on top.

```
TableView (derives from ListView)
├── TableViewHeaderRow         ← sticky header above the scrollable content
│   └── TableViewColumnHeader  ← one per column; handles sort, filter, resize, reorder
└── TableViewRow (one per item)
    ├── TableViewRowHeader      ← optional left gutter per row
    ├── TableViewCell[]         ← one per column
    └── RowDetails panel        ← optional collapsible detail area
```

## Key concepts

### Columns

Each column is an object that lives in `TableView.Columns`. Columns define:

- which data property to display (`Binding`)
- which editing control to show when a cell enters edit mode
- styling, sizing, and sorting behavior

All built-in column types inherit from `TableViewColumn`. Columns that bind to a data property inherit from `TableViewBoundColumn`.

See [Column types](column-types.md) and [Defining columns](defining-columns.md).

### Items source

`TableView.ItemsSource` accepts any `IEnumerable`. Internally the control wraps the source in an `AdvancedCollectionView` (from the CommunityToolkit) that supports sorting and filtering without mutating the original collection.

See [Binding data](binding-data.md).

### Selection

The control supports row selection, cell selection, or both, through `SelectionMode` and `SelectionUnit`. Selected cells are tracked via `TableViewCellSlot` (a `(Row, Column)` record struct).

See [Selection](selection.md).

### Editing

Double-tapping a cell, or pressing F2, enters edit mode for that cell. The column's `GenerateEditingElement` method provides the editing control. The lifecycle fires `BeginningEdit`, `PreparingCellForEdit`, `CellEditEnding`, and `CellEditEnded`.

See [Editing](editing.md).

### Collection view

`TableView` exposes a `CollectionView` property that gives direct access to the internal `ICollectionView`. You can programmatically add sort or filter descriptions to this view.

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

On Uno Platform targets some behaviors differ slightly from the Windows (WinUI 3) target, particularly around data binding and focus management. See [Getting started with Uno](getting-started-with-uno.md) for platform-specific notes.

## Related articles

- [Getting started](getting-started.md)
- [Binding data](binding-data.md)
- [Defining columns](defining-columns.md)
- [Column types](column-types.md)
