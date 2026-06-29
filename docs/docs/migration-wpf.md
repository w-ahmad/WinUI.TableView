# Migrating from WPF DataGrid

This guide helps developers who are migrating a WPF application to WinUI 3 and need a replacement for `System.Windows.Controls.DataGrid`. WinUI.TableView is a community-maintained DataGrid control for WinUI 3 that covers most of the DataGrid scenarios found in WPF.

## Column type mapping

| WPF DataGrid | WinUI.TableView | Notes |
|---|---|---|
| `DataGridTextColumn` | [`TableViewTextColumn`](xref:WinUI.TableView.TableViewTextColumn) | Same purpose. Binding syntax differs slightly (see below). |
| `DataGridCheckBoxColumn` | [`TableViewCheckBoxColumn`](xref:WinUI.TableView.TableViewCheckBoxColumn) | Behaves the same. |
| `DataGridComboBoxColumn` | [`TableViewComboBoxColumn`](xref:WinUI.TableView.TableViewComboBoxColumn) | Similar. Use [`ItemsSource`](xref:WinUI.TableView.TableView.ItemsSource), [`DisplayMemberPath`](xref:WinUI.TableView.TableViewComboBoxColumn.DisplayMemberPath), [`SelectedValuePath`](xref:WinUI.TableView.TableViewComboBoxColumn.SelectedValuePath). |
| `DataGridTemplateColumn` | [`TableViewTemplateColumn`](xref:WinUI.TableView.TableViewTemplateColumn) | Use [`CellTemplate`](xref:WinUI.TableView.TableViewTemplateColumn.CellTemplate) and [`EditingTemplate`](xref:WinUI.TableView.TableViewTemplateColumn.EditingTemplate). |
| `DataGridHyperlinkColumn` | [`TableViewHyperlinkColumn`](xref:WinUI.TableView.TableViewHyperlinkColumn) | Use [`Binding`](xref:WinUI.TableView.TableViewBoundColumn.Binding) for `NavigateUri` and optional [`ContentBinding`](xref:WinUI.TableView.TableViewHyperlinkColumn.ContentBinding) for display text. |
| *(no equivalent)* | [`TableViewNumberColumn`](xref:WinUI.TableView.TableViewNumberColumn) | Edits with `NumberBox`. Not in WPF DataGrid. |
| *(no equivalent)* | [`TableViewDateColumn`](xref:WinUI.TableView.TableViewDateColumn) | Edits with a date picker. Not in WPF DataGrid. |
| *(no equivalent)* | [`TableViewTimeColumn`](xref:WinUI.TableView.TableViewTimeColumn) | Edits with a time picker. Not in WPF DataGrid. |
| *(no equivalent)* | [`TableViewToggleSwitchColumn`](xref:WinUI.TableView.TableViewToggleSwitchColumn) | Fluent toggle switch. Not in WPF DataGrid. |

## Property mapping

| WPF DataGrid property | WinUI.TableView equivalent | Notes |
|---|---|---|
| [`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) | [`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) | Same behavior. |
| [`IsReadOnly`](xref:WinUI.TableView.TableView.IsReadOnly) | [`IsReadOnly`](xref:WinUI.TableView.TableView.IsReadOnly) | Same behavior. Per-column [`IsReadOnly`](xref:WinUI.TableView.TableView.IsReadOnly) is also supported. |
| `CanUserSortColumns` | [`CanSortColumns`](xref:WinUI.TableView.TableView.CanSortColumns) | Same behavior. |
| `CanUserReorderColumns` | [`CanReorderColumns`](xref:WinUI.TableView.TableView.CanReorderColumns) | Same behavior. |
| `CanUserResizeColumns` | [`CanResizeColumns`](xref:WinUI.TableView.TableView.CanResizeColumns) | Same behavior. |
| [`FrozenColumnCount`](xref:WinUI.TableView.TableView.FrozenColumnCount) | [`FrozenColumnCount`](xref:WinUI.TableView.TableView.FrozenColumnCount) | Same behavior. |
| [`RowDetailsTemplate`](xref:WinUI.TableView.TableView.RowDetailsTemplate) | [`RowDetailsTemplate`](xref:WinUI.TableView.TableView.RowDetailsTemplate) | Same behavior. |
| [`RowDetailsTemplateSelector`](xref:WinUI.TableView.TableView.RowDetailsTemplateSelector) | [`RowDetailsTemplateSelector`](xref:WinUI.TableView.TableView.RowDetailsTemplateSelector) | Same behavior. |
| [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode) | [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode) | Enum values map directly. |
| [`AreRowDetailsFrozen`](xref:WinUI.TableView.TableView.AreRowDetailsFrozen) | [`AreRowDetailsFrozen`](xref:WinUI.TableView.TableView.AreRowDetailsFrozen) | Same behavior. |
| [`HeadersVisibility`](xref:WinUI.TableView.TableView.HeadersVisibility) | [`HeadersVisibility`](xref:WinUI.TableView.TableView.HeadersVisibility) | [`TableViewHeadersVisibility`](xref:WinUI.TableView.TableViewHeadersVisibility) mirrors `DataGridHeadersVisibility`. |
| [`RowHeight`](xref:WinUI.TableView.TableView.RowHeight) | [`RowHeight`](xref:WinUI.TableView.TableView.RowHeight) | Same behavior. |
| `ColumnHeaderHeight` | [`HeaderRowHeight`](xref:WinUI.TableView.TableView.HeaderRowHeight) | Renamed. |
| `MinRowHeight` | [`RowMinHeight`](xref:WinUI.TableView.TableView.RowMinHeight) | Renamed. |
| [`MinColumnWidth`](xref:WinUI.TableView.TableView.MinColumnWidth) | [`MinColumnWidth`](xref:WinUI.TableView.TableView.MinColumnWidth) | Same behavior. |
| [`MaxColumnWidth`](xref:WinUI.TableView.TableView.MaxColumnWidth) | [`MaxColumnWidth`](xref:WinUI.TableView.TableView.MaxColumnWidth) | Same behavior. |
| [`GridLinesVisibility`](xref:WinUI.TableView.TableView.GridLinesVisibility) | [`GridLinesVisibility`](xref:WinUI.TableView.TableView.GridLinesVisibility) | Enum values differ slightly (see below). |
| `HorizontalGridLinesBrush` | [`HorizontalGridLinesStroke`](xref:WinUI.TableView.TableView.HorizontalGridLinesStroke) | Renamed. |
| `VerticalGridLinesBrush` | [`VerticalGridLinesStroke`](xref:WinUI.TableView.TableView.VerticalGridLinesStroke) | Renamed. |
| `AlternatingRowBackground` | [`AlternateRowBackground`](xref:WinUI.TableView.TableView.AlternateRowBackground) | Renamed. |
| [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) | [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) | Uses `ListViewSelectionMode` instead of `DataGridSelectionMode`. |
| [`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit) | [`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit) | [`TableViewSelectionUnit`](xref:WinUI.TableView.TableViewSelectionUnit) — similar values. |
| `CurrentCell` | [`CurrentCellSlot`](xref:WinUI.TableView.TableView.CurrentCellSlot) | `TableViewCellSlot(Row, Column)` struct instead of `DataGridCellInfo`. |
| `ClipboardCopyMode` | [`CanCopy`](xref:WinUI.TableView.TableView.CanCopy) + [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) event | WinUI.TableView uses a bool and an event rather than an enum. |
| *(no equivalent)* | [`CanPaste`](xref:WinUI.TableView.TableView.CanPaste) | WPF DataGrid does not have built-in paste. |
| *(no equivalent)* | [`ShowExportOptions`](xref:WinUI.TableView.TableView.ShowExportOptions) | WPF DataGrid does not have built-in CSV export. |
| *(no equivalent)* | [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout) | WPF DataGrid does not have built-in context flyout support. |
| *(no equivalent)* | [`ConditionalCellStyles`](xref:WinUI.TableView.TableView.ConditionalCellStyles) | WPF DataGrid does not have this. Use row/cell `Style` triggers instead. |

## Event mapping

| WPF DataGrid event | WinUI.TableView equivalent | Notes |
|---|---|---|
| [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) | [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) | Same purpose. Args include `PropertyName`, `PropertyType`, and `Column`. |
| [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit) | [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit) | Same purpose. Args include `Cell`, `DataItem`, `Column`. Cancelable. |
| [`PreparingCellForEdit`](xref:WinUI.TableView.TableView.PreparingCellForEdit) | [`PreparingCellForEdit`](xref:WinUI.TableView.TableView.PreparingCellForEdit) | Same purpose. |
| [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding) | [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding) | Same purpose. `EditAction` is `Commit` or `Cancel`. |
| [`CellEditEnded`](xref:WinUI.TableView.TableView.CellEditEnded) | [`CellEditEnded`](xref:WinUI.TableView.TableView.CellEditEnded) | Fires after commit or cancel. |
| [`Sorting`](xref:WinUI.TableView.TableView.Sorting) | [`Sorting`](xref:WinUI.TableView.TableView.Sorting) | Same purpose. Set `Handled = true` for custom sort. |
| `SelectionChanged` | `SelectionChanged` (inherited) + [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged) | `SelectionChanged` is from `ListView`. [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged) is new. |
| [`CurrentCellChanged`](xref:WinUI.TableView.TableView.CurrentCellChanged) | [`CurrentCellChanged`](xref:WinUI.TableView.TableView.CurrentCellChanged) | Similar. |
| `CopyingRowClipboardContent` | [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) | Renamed. Set `Handled = true` for custom content. |
| [`ColumnReordered`](xref:WinUI.TableView.TableView.ColumnReordered) | [`ColumnReordered`](xref:WinUI.TableView.TableView.ColumnReordered) | Same purpose. |

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
| Cell and row validation | ❌ No built-in validation — use [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit) / [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding) |
| Row resize | ❌ Not supported |
| `DataGridRowHeader` custom style | ⚠️ Use [`RowHeaderTemplate`](xref:WinUI.TableView.TableView.RowHeaderTemplate) instead |

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
