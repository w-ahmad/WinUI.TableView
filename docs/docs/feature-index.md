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
| [Defining columns](defining-columns.md) | Auto-generate or manually define columns; [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) event |
| [Column types](column-types.md) | Text, Number, CheckBox, ToggleSwitch, ComboBox, Date, Time, Hyperlink, Template |
| [Column sizing](column-sizing.md) | Fixed, star, and auto widths; min/max constraints; row heights |

## Interaction

| Topic | Description |
|---|---|
| [Sorting](sorting.md) | Built-in column sort; [`SortDescriptions`](xref:WinUI.TableView.TableView.SortDescriptions); custom sort via [`Sorting`](xref:WinUI.TableView.TableView.Sorting) event |
| [Filtering](filtering.md) | Excel-like filter flyout; [`FilterDescriptions`](xref:WinUI.TableView.TableView.FilterDescriptions); custom filter handler |
| [Editing](editing.md) | In-place cell editing; [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit), [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding), validation |
| [Selection](selection.md) | [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode), [`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit), cell slots, [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged) |

## Layout

| Topic | Description |
|---|---|
| [Row headers](row-headers.md) | Custom row header templates; [`HeadersVisibility`](xref:WinUI.TableView.TableView.HeadersVisibility); row header sizing |
| [Row details](row-details.md) | Expandable detail panels; [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode); frozen details |
| [Frozen columns](frozen-columns.md) | Pin columns to the left with [`FrozenColumnCount`](xref:WinUI.TableView.TableView.FrozenColumnCount) |
| [Column reordering](column-reordering.md) | Drag columns; [`ColumnReordering`](xref:WinUI.TableView.TableView.ColumnReordering) / [`ColumnReordered`](xref:WinUI.TableView.TableView.ColumnReordered) events |

## Data operations

| Topic | Description |
|---|---|
| [Clipboard and copy/paste](clipboard.md) | [`CanCopy`](xref:WinUI.TableView.TableView.CanCopy), [`CanPaste`](xref:WinUI.TableView.TableView.CanPaste), [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) event, custom clipboard content |
| [Export to CSV](export.md) | [`ShowExportOptions`](xref:WinUI.TableView.TableView.ShowExportOptions), [`ExportAllContent`](xref:WinUI.TableView.TableView.ExportAllContent), [`ExportSelectedContent`](xref:WinUI.TableView.TableView.ExportSelectedContent) |

## Appearance

| Topic | Description |
|---|---|
| [Styling rows, cells, and headers](styling.md) | [`CellStyle`](xref:WinUI.TableView.TableView.CellStyle), [`ColumnHeaderStyle`](xref:WinUI.TableView.TableView.ColumnHeaderStyle), [`ElementStyle`](xref:WinUI.TableView.TableViewBoundColumn.ElementStyle), grid lines, alternate rows |
| [Conditional cell styling](conditional-styling.md) | Apply styles based on predicates; per-table and per-column styles |
| [Context flyouts](context-flyouts.md) | [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout), [`CellContextFlyout`](xref:WinUI.TableView.TableView.CellContextFlyout), opening events |

## Reference

| Topic | Description |
|---|---|
| [Events and commands reference](commands-events.md) | All events, event args, and enum values |
| [Performance guidance](performance.md) | Virtualization, live shaping, large dataset tips |
| [API Reference](xref:WinUI.TableView.TableView) | Auto-generated API reference for `TableView` |

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
