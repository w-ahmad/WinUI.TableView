# Feature index

This page lists all WinUI.TableView features with links to the relevant documentation.

## Getting started

| Topic | Description |
|---|---|
| [Installation and quick start](getting-started.md) | Install the NuGet package and create a basic table |
| [Getting started with Uno Platform](getting-started-with-uno.md) | Set up WinUI.TableView in an Uno Platform project |
| [Overview and concepts](overview.md) | Architecture, key concepts, and terminology |

## Data

| Topic | Description |
|---|---|
| [Binding data](binding-data.md) | Bind `ObservableCollection<T>`, `INotifyPropertyChanged`, live shaping |
| [Defining columns](defining-columns.md) | Auto-generate or manually define columns; `AutoGeneratingColumn` event |
| [Column types](column-types.md) | Text, Number, CheckBox, ToggleSwitch, ComboBox, Date, Time, Hyperlink, Template |
| [Column sizing](column-sizing.md) | Fixed, star, and auto widths; min/max constraints; row heights |

## Interaction

| Topic | Description |
|---|---|
| [Sorting](sorting.md) | Built-in column sort; `SortDescriptions`; custom sort via `Sorting` event |
| [Filtering](filtering.md) | Excel-like filter flyout; `FilterDescriptions`; custom filter handler |
| [Editing](editing.md) | In-place cell editing; `BeginningEdit`, `CellEditEnding`, validation |
| [Selection](selection.md) | `SelectionMode`, `SelectionUnit`, cell slots, `CellSelectionChanged` |

## Layout

| Topic | Description |
|---|---|
| [Row headers](row-headers.md) | Custom row header templates; `HeadersVisibility`; row header sizing |
| [Row details](row-details.md) | Expandable detail panels; `RowDetailsVisibilityMode`; frozen details |
| [Frozen columns](frozen-columns.md) | Pin columns to the left with `FrozenColumnCount` |
| [Column reordering](column-reordering.md) | Drag columns; `ColumnReordering` / `ColumnReordered` events |

## Data operations

| Topic | Description |
|---|---|
| [Clipboard and copy/paste](clipboard.md) | `CanCopy`, `CanPaste`, `CopyToClipboard` event, custom clipboard content |
| [Export to CSV](export.md) | `ShowExportOptions`, `ExportAllContent`, `ExportSelectedContent` |

## Appearance

| Topic | Description |
|---|---|
| [Styling rows, cells, and headers](styling.md) | `CellStyle`, `ColumnHeaderStyle`, `ElementStyle`, grid lines, alternate rows |
| [Conditional cell styling](conditional-styling.md) | Apply styles based on predicates; per-table and per-column styles |
| [Context flyouts](context-flyouts.md) | `RowContextFlyout`, `CellContextFlyout`, opening events |

## Reference

| Topic | Description |
|---|---|
| [Events and commands reference](commands-events.md) | All events, event args, and enum values |
| [Performance guidance](performance.md) | Virtualization, live shaping, large dataset tips |
| [API Reference](../api/index.md) | Auto-generated API reference |

## Migration

| Topic | Description |
|---|---|
| [Migrating from WPF DataGrid](migration-wpf.md) | Property, event, and column type mapping from WPF |
| [Migrating from WCT DataGrid](migration-wct.md) | Property, event, and column type mapping from WCT |
| [Feature comparison](datagrid-feature-comparison.md) | Side-by-side feature comparison table |

## Common scenarios

| Scenario | Where to look |
|---|---|
| Show data from a database | [Binding data](binding-data.md) |
| Define specific columns | [Defining columns](defining-columns.md) |
| Let users sort by clicking headers | [Sorting](sorting.md) |
| Let users filter rows | [Filtering](filtering.md) |
| Make some columns read-only | [Editing](editing.md) |
| Select individual cells | [Selection](selection.md) |
| Show extra row info on expand | [Row details](row-details.md) |
| Keep ID column in view while scrolling | [Frozen columns](frozen-columns.md) |
| Let users copy to Excel | [Clipboard and copy/paste](clipboard.md) |
| Export data to CSV | [Export to CSV](export.md) |
| Highlight negative values in red | [Conditional cell styling](conditional-styling.md) |
| Add right-click menu | [Context flyouts](context-flyouts.md) |
| Migrate from WPF DataGrid | [Migrating from WPF DataGrid](migration-wpf.md) |
| Migrate from WCT DataGrid | [Migrating from WCT DataGrid](migration-wct.md) |
