# Migrating from WCT DataGrid

The Windows Community Toolkit (WCT) DataGrid (`CommunityToolkit.WinUI.UI.Controls.DataGrid`) was available for UWP and early Uno Platform apps but is now archived and no longer maintained. The archived Toolkit documentation recommends using newer alternatives such as WinUI.TableView for new development.

This guide helps developers moving from the WCT DataGrid to WinUI.TableView.

## NuGet package change

| | Package |
|---|---|
| WCT DataGrid | `CommunityToolkit.WinUI.UI.Controls.DataGrid` (archived) |
| WinUI.TableView | `WinUI.TableView` |

## XAML namespace change

```xml
<!-- WCT DataGrid -->
xmlns:controls="using:CommunityToolkit.WinUI.UI.Controls"
<controls:DataGrid ... />

<!-- WinUI.TableView -->
xmlns:tv="using:WinUI.TableView"
<tv:TableView ... />
```

## Column type mapping

| WCT DataGrid | WinUI.TableView | Notes |
|---|---|---|
| `DataGridTextColumn` | [`TableViewTextColumn`](xref:WinUI.TableView.TableViewTextColumn) | Same purpose. |
| `DataGridCheckBoxColumn` | [`TableViewCheckBoxColumn`](xref:WinUI.TableView.TableViewCheckBoxColumn) | Same purpose. |
| `DataGridComboBoxColumn` | [`TableViewComboBoxColumn`](xref:WinUI.TableView.TableViewComboBoxColumn) | Similar. |
| `DataGridTemplateColumn` | [`TableViewTemplateColumn`](xref:WinUI.TableView.TableViewTemplateColumn) | Same purpose. Uses [`CellTemplate`](xref:WinUI.TableView.TableViewTemplateColumn.CellTemplate) / [`EditingTemplate`](xref:WinUI.TableView.TableViewTemplateColumn.EditingTemplate). |
| *(no equivalent)* | [`TableViewNumberColumn`](xref:WinUI.TableView.TableViewNumberColumn) | Uses `NumberBox` for editing. |
| *(no equivalent)* | [`TableViewDateColumn`](xref:WinUI.TableView.TableViewDateColumn) | Date picker editing. |
| *(no equivalent)* | [`TableViewTimeColumn`](xref:WinUI.TableView.TableViewTimeColumn) | Time picker editing. |
| *(no equivalent)* | [`TableViewToggleSwitchColumn`](xref:WinUI.TableView.TableViewToggleSwitchColumn) | Fluent toggle switch. |
| *(no equivalent)* | [`TableViewHyperlinkColumn`](xref:WinUI.TableView.TableViewHyperlinkColumn) | Hyperlink display. |

## Property mapping

| WCT DataGrid property | WinUI.TableView equivalent | Notes |
|---|---|---|
| [`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) | [`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) | Same behavior. |
| [`IsReadOnly`](xref:WinUI.TableView.TableView.IsReadOnly) | [`IsReadOnly`](xref:WinUI.TableView.TableView.IsReadOnly) | Same behavior. |
| `CanUserSortColumns` | [`CanSortColumns`](xref:WinUI.TableView.TableView.CanSortColumns) | Same behavior. |
| `CanUserReorderColumns` | [`CanReorderColumns`](xref:WinUI.TableView.TableView.CanReorderColumns) | Same behavior. |
| `CanUserResizeColumns` | [`CanResizeColumns`](xref:WinUI.TableView.TableView.CanResizeColumns) | Same behavior. |
| [`FrozenColumnCount`](xref:WinUI.TableView.TableView.FrozenColumnCount) | [`FrozenColumnCount`](xref:WinUI.TableView.TableView.FrozenColumnCount) | Same behavior. |
| [`RowDetailsTemplate`](xref:WinUI.TableView.TableView.RowDetailsTemplate) | [`RowDetailsTemplate`](xref:WinUI.TableView.TableView.RowDetailsTemplate) | Same behavior. |
| [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode) | [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode) | Enum values map directly. |
| [`AreRowDetailsFrozen`](xref:WinUI.TableView.TableView.AreRowDetailsFrozen) | [`AreRowDetailsFrozen`](xref:WinUI.TableView.TableView.AreRowDetailsFrozen) | Same behavior. |
| [`HeadersVisibility`](xref:WinUI.TableView.TableView.HeadersVisibility) | [`HeadersVisibility`](xref:WinUI.TableView.TableView.HeadersVisibility) | [`TableViewHeadersVisibility`](xref:WinUI.TableView.TableViewHeadersVisibility) mirrors `DataGridHeadersVisibility`. |
| [`RowHeight`](xref:WinUI.TableView.TableView.RowHeight) | [`RowHeight`](xref:WinUI.TableView.TableView.RowHeight) | Same behavior. |
| `ColumnHeaderHeight` | [`HeaderRowHeight`](xref:WinUI.TableView.TableView.HeaderRowHeight) | Renamed. |
| `MinRowHeight` | [`RowMinHeight`](xref:WinUI.TableView.TableView.RowMinHeight) | Renamed. |
| [`MinColumnWidth`](xref:WinUI.TableView.TableView.MinColumnWidth) | [`MinColumnWidth`](xref:WinUI.TableView.TableView.MinColumnWidth) | Same behavior. |
| [`MaxColumnWidth`](xref:WinUI.TableView.TableView.MaxColumnWidth) | [`MaxColumnWidth`](xref:WinUI.TableView.TableView.MaxColumnWidth) | Same behavior. |
| [`GridLinesVisibility`](xref:WinUI.TableView.TableView.GridLinesVisibility) | [`GridLinesVisibility`](xref:WinUI.TableView.TableView.GridLinesVisibility) | Similar enum values. |
| `HorizontalGridLinesBrush` | [`HorizontalGridLinesStroke`](xref:WinUI.TableView.TableView.HorizontalGridLinesStroke) | Renamed. |
| `VerticalGridLinesBrush` | [`VerticalGridLinesStroke`](xref:WinUI.TableView.TableView.VerticalGridLinesStroke) | Renamed. |
| `AlternatingRowBackground` | [`AlternateRowBackground`](xref:WinUI.TableView.TableView.AlternateRowBackground) | Renamed. |
| [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) | [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) | Uses `ListViewSelectionMode`. |
| `CurrentColumn` | *(not directly available)* | Use `CurrentCellSlot.Column` index. |
| `ClipboardCopyMode` | [`CanCopy`](xref:WinUI.TableView.TableView.CanCopy) + [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) event | Simpler model. |
| *(no equivalent)* | [`CanPaste`](xref:WinUI.TableView.TableView.CanPaste) | WCT DataGrid does not have built-in paste. WinUI.TableView does. |
| *(no equivalent)* | [`ShowExportOptions`](xref:WinUI.TableView.TableView.ShowExportOptions) | WCT DataGrid does not have built-in CSV export. |
| *(no equivalent)* | [`ConditionalCellStyles`](xref:WinUI.TableView.TableView.ConditionalCellStyles) | WCT DataGrid requires manual style triggers. |
| *(no equivalent)* | [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout) / [`CellContextFlyout`](xref:WinUI.TableView.TableView.CellContextFlyout) | WCT DataGrid has no built-in context flyout. |
| *(no equivalent)* | Column filtering | WCT DataGrid has no built-in filtering. WinUI.TableView has an Excel-like filter flyout. |

## Event mapping

| WCT DataGrid event | WinUI.TableView equivalent | Notes |
|---|---|---|
| [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) | [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) | Same purpose. |
| [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit) | [`BeginningEdit`](xref:WinUI.TableView.TableView.BeginningEdit) | Same purpose. Cancelable. |
| [`PreparingCellForEdit`](xref:WinUI.TableView.TableView.PreparingCellForEdit) | [`PreparingCellForEdit`](xref:WinUI.TableView.TableView.PreparingCellForEdit) | Same purpose. |
| [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding) | [`CellEditEnding`](xref:WinUI.TableView.TableView.CellEditEnding) | Same purpose. |
| [`CellEditEnded`](xref:WinUI.TableView.TableView.CellEditEnded) | [`CellEditEnded`](xref:WinUI.TableView.TableView.CellEditEnded) | Same purpose. |
| [`Sorting`](xref:WinUI.TableView.TableView.Sorting) (custom event) | [`Sorting`](xref:WinUI.TableView.TableView.Sorting) | In WCT DataGrid, sorting is not built-in; the event fires but no sort is applied. In WinUI.TableView, the default sort is applied unless `Handled = true`. |
| `SelectionChanged` | `SelectionChanged` + [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged) | Inherited `SelectionChanged` + new [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged). |
| `CopyingRowClipboardContent` | [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) | Renamed. |
| [`ColumnReordered`](xref:WinUI.TableView.TableView.ColumnReordered) | [`ColumnReordered`](xref:WinUI.TableView.TableView.ColumnReordered) | Same purpose. |
| `LoadingRowGroup` | *(no equivalent)* | WinUI.TableView does not support row grouping. |
| `LoadingRow` | *(no equivalent)* | WinUI.TableView does not have a `LoadingRow` event. Use `ContainerContentChanging` inherited from `ListView`. |

## Sorting

A key difference: the WCT DataGrid fires sorting events but does **not** apply sorting itself — your code must sort the source collection. WinUI.TableView applies sorting automatically via the internal `AdvancedCollectionView`.

**WCT DataGrid pattern (manual sort):**
```csharp
dataGrid.Sorting += (s, e) =>
{
    // You must sort the collection yourself
    var sorted = e.Column.SortDirection == DataGridSortDirection.Ascending
        ? items.OrderBy(i => GetValue(i, e.Column))
        : items.OrderByDescending(i => GetValue(i, e.Column));
    dataGrid.ItemsSource = sorted.ToList();
    e.Column.SortDirection = ...;
};
```

**WinUI.TableView (built-in sort):**
```xml
<!-- No code needed for default sort behavior -->
<tv:TableView ItemsSource="{x:Bind Products}" />
```

For custom sort, set `e.Handled = true` in the [`Sorting`](xref:WinUI.TableView.TableView.Sorting) event and apply your own [`SortDescription`](xref:WinUI.TableView.SortDescription).

## Filtering

WCT DataGrid has no built-in filtering. You had to filter the source collection manually.

WinUI.TableView has a built-in Excel-like column filter flyout with no extra code required:

```xml
<tv:TableView CanFilterColumns="True" /> <!-- CanFilterColumns is true by default -->
```

## Cell selection

WCT DataGrid does not support cell selection — only row selection. WinUI.TableView supports individual cell selection, ranges, and combined cell+row modes. See [Selection](selection.md).

## Features not available in WinUI.TableView

| WCT DataGrid feature | Status in WinUI.TableView |
|---|---|
| Row grouping | ❌ Not supported |
| Add new row | ❌ Not supported |
| Delete row | ❌ Not supported — implement via context flyout |
| Row resize | ❌ Not supported |
| Accessible / Narrator support | ❌ Not verified in WinUI.TableView |

## References

- [Archived WCT DataGrid documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/archive/windows/datagrid)
- [WCT DataGrid API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.toolkit.uwp.ui.controls.datagrid)

## Related articles

- [Migrating from WPF DataGrid](migration-wpf.md)
- [Feature comparison](datagrid-feature-comparison.md)
- [Getting started](getting-started.md)
