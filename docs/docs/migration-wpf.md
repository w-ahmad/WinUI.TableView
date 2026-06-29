# Migrating from WPF DataGrid

This guide helps developers who are migrating a WPF application to WinUI 3 and need a replacement for `System.Windows.Controls.DataGrid`. WinUI.TableView is one of the community-maintained DataGrid alternatives recommended in Microsoft's WPF-to-WinUI migration guidance.

## Column type mapping

| WPF DataGrid | WinUI.TableView | Notes |
|---|---|---|
| `DataGridTextColumn` | `TableViewTextColumn` | Same purpose. Binding syntax differs slightly (see below). |
| `DataGridCheckBoxColumn` | `TableViewCheckBoxColumn` | Behaves the same. |
| `DataGridComboBoxColumn` | `TableViewComboBoxColumn` | Similar. Use `ItemsSource`, `DisplayMemberPath`, `SelectedValuePath`. |
| `DataGridTemplateColumn` | `TableViewTemplateColumn` | Use `CellTemplate` and `EditingTemplate`. |
| `DataGridHyperlinkColumn` | `TableViewHyperlinkColumn` | Use `Binding` for `NavigateUri` and optional `ContentBinding` for display text. |
| *(no equivalent)* | `TableViewNumberColumn` | Edits with `NumberBox`. Not in WPF DataGrid. |
| *(no equivalent)* | `TableViewDateColumn` | Edits with a date picker. Not in WPF DataGrid. |
| *(no equivalent)* | `TableViewTimeColumn` | Edits with a time picker. Not in WPF DataGrid. |
| *(no equivalent)* | `TableViewToggleSwitchColumn` | Fluent toggle switch. Not in WPF DataGrid. |

## Property mapping

| WPF DataGrid property | WinUI.TableView equivalent | Notes |
|---|---|---|
| `AutoGenerateColumns` | `AutoGenerateColumns` | Same behavior. |
| `IsReadOnly` | `IsReadOnly` | Same behavior. Per-column `IsReadOnly` is also supported. |
| `CanUserSortColumns` | `CanSortColumns` | Same behavior. |
| `CanUserReorderColumns` | `CanReorderColumns` | Same behavior. |
| `CanUserResizeColumns` | `CanResizeColumns` | Same behavior. |
| `FrozenColumnCount` | `FrozenColumnCount` | Same behavior. |
| `RowDetailsTemplate` | `RowDetailsTemplate` | Same behavior. |
| `RowDetailsTemplateSelector` | `RowDetailsTemplateSelector` | Same behavior. |
| `RowDetailsVisibilityMode` | `RowDetailsVisibilityMode` | Enum values map directly. |
| `AreRowDetailsFrozen` | `AreRowDetailsFrozen` | Same behavior. |
| `HeadersVisibility` | `HeadersVisibility` | `TableViewHeadersVisibility` mirrors `DataGridHeadersVisibility`. |
| `RowHeight` | `RowHeight` | Same behavior. |
| `ColumnHeaderHeight` | `HeaderRowHeight` | Renamed. |
| `MinRowHeight` | `RowMinHeight` | Renamed. |
| `MinColumnWidth` | `MinColumnWidth` | Same behavior. |
| `MaxColumnWidth` | `MaxColumnWidth` | Same behavior. |
| `GridLinesVisibility` | `GridLinesVisibility` | Enum values differ slightly (see below). |
| `HorizontalGridLinesBrush` | `HorizontalGridLinesStroke` | Renamed. |
| `VerticalGridLinesBrush` | `VerticalGridLinesStroke` | Renamed. |
| `AlternatingRowBackground` | `AlternateRowBackground` | Renamed. |
| `SelectionMode` | `SelectionMode` | Uses `ListViewSelectionMode` instead of `DataGridSelectionMode`. |
| `SelectionUnit` | `SelectionUnit` | `TableViewSelectionUnit` — similar values. |
| `CurrentCell` | `CurrentCellSlot` | `TableViewCellSlot(Row, Column)` struct instead of `DataGridCellInfo`. |
| `ClipboardCopyMode` | `CanCopy` + `CopyToClipboard` event | WinUI.TableView uses a bool and an event rather than an enum. |
| *(no equivalent)* | `CanPaste` | WPF DataGrid does not have built-in paste. |
| *(no equivalent)* | `ShowExportOptions` | WPF DataGrid does not have built-in CSV export. |
| *(no equivalent)* | `RowContextFlyout` | WPF DataGrid does not have built-in context flyout support. |
| *(no equivalent)* | `ConditionalCellStyles` | WPF DataGrid does not have this. Use row/cell `Style` triggers instead. |

## Event mapping

| WPF DataGrid event | WinUI.TableView equivalent | Notes |
|---|---|---|
| `AutoGeneratingColumn` | `AutoGeneratingColumn` | Same purpose. Args include `PropertyName`, `PropertyType`, and `Column`. |
| `BeginningEdit` | `BeginningEdit` | Same purpose. Args include `Cell`, `DataItem`, `Column`. Cancelable. |
| `PreparingCellForEdit` | `PreparingCellForEdit` | Same purpose. |
| `CellEditEnding` | `CellEditEnding` | Same purpose. `EditAction` is `Commit` or `Cancel`. |
| `CellEditEnded` | `CellEditEnded` | Fires after commit or cancel. |
| `Sorting` | `Sorting` | Same purpose. Set `Handled = true` for custom sort. |
| `SelectionChanged` | `SelectionChanged` (inherited) + `CellSelectionChanged` | `SelectionChanged` is from `ListView`. `CellSelectionChanged` is new. |
| `CurrentCellChanged` | `CurrentCellChanged` | Similar. |
| `CopyingRowClipboardContent` | `CopyToClipboard` | Renamed. Set `Handled = true` for custom content. |
| `ColumnReordered` | `ColumnReordered` | Same purpose. |

## Binding differences

WPF DataGrid column bindings use `Binding="{Binding PropertyName}"` and are typically `TwoWay` by default.

WinUI.TableView bindings also use `Binding="{Binding PropertyName}"` (standard `{Binding}`), **not** `{x:Bind}`. The column sets the binding mode to `TwoWay` automatically:

```xml
<!-- WPF DataGrid -->
<DataGridTextColumn Header="Name" Binding="{Binding Name}" />

<!-- WinUI.TableView -->
<tv:TableViewTextColumn Header="Name" Binding="{Binding Name}" />
```

## Features not available in WinUI.TableView

| WPF DataGrid feature | Status in WinUI.TableView |
|---|---|
| Add new row (NewItemPlaceholder) | ❌ Not supported |
| Delete row (Del key) | ❌ Not supported — implement in a context flyout |
| Row grouping | ❌ Not supported |
| Cell and row validation | ❌ No built-in validation — use `BeginningEdit` / `CellEditEnding` |
| Row resize | ❌ Not supported |
| `DataGridRowHeader` custom style | ⚠️ Use `RowHeaderTemplate` instead |

## XAML namespace

WPF:
```xml
xmlns:dg="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
<!-- DataGrid is in the default WPF namespace -->
```

WinUI.TableView:
```xml
xmlns:tv="using:WinUI.TableView"
```

## References

- [Microsoft WPF to WinUI 3 migration guidance](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/wpf-patterns-winui3)
- [WPF DataGrid API reference](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.datagrid)

## Related articles

- [Migrating from WCT DataGrid](migration-wct.md)
- [Feature comparison](datagrid-feature-comparison.md)
- [Getting started](getting-started.md)
