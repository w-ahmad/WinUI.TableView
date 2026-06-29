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
| `DataGridTextColumn` | `TableViewTextColumn` | Same purpose. |
| `DataGridCheckBoxColumn` | `TableViewCheckBoxColumn` | Same purpose. |
| `DataGridComboBoxColumn` | `TableViewComboBoxColumn` | Similar. |
| `DataGridTemplateColumn` | `TableViewTemplateColumn` | Same purpose. Uses `CellTemplate` / `EditingTemplate`. |
| *(no equivalent)* | `TableViewNumberColumn` | Uses `NumberBox` for editing. |
| *(no equivalent)* | `TableViewDateColumn` | Date picker editing. |
| *(no equivalent)* | `TableViewTimeColumn` | Time picker editing. |
| *(no equivalent)* | `TableViewToggleSwitchColumn` | Fluent toggle switch. |
| *(no equivalent)* | `TableViewHyperlinkColumn` | Hyperlink display. |

## Property mapping

| WCT DataGrid property | WinUI.TableView equivalent | Notes |
|---|---|---|
| `AutoGenerateColumns` | `AutoGenerateColumns` | Same behavior. |
| `IsReadOnly` | `IsReadOnly` | Same behavior. |
| `CanUserSortColumns` | `CanSortColumns` | Same behavior. |
| `CanUserReorderColumns` | `CanReorderColumns` | Same behavior. |
| `CanUserResizeColumns` | `CanResizeColumns` | Same behavior. |
| `FrozenColumnCount` | `FrozenColumnCount` | Same behavior. |
| `RowDetailsTemplate` | `RowDetailsTemplate` | Same behavior. |
| `RowDetailsVisibilityMode` | `RowDetailsVisibilityMode` | Enum values map directly. |
| `AreRowDetailsFrozen` | `AreRowDetailsFrozen` | Same behavior. |
| `HeadersVisibility` | `HeadersVisibility` | `TableViewHeadersVisibility` mirrors `DataGridHeadersVisibility`. |
| `RowHeight` | `RowHeight` | Same behavior. |
| `ColumnHeaderHeight` | `HeaderRowHeight` | Renamed. |
| `MinRowHeight` | `RowMinHeight` | Renamed. |
| `MinColumnWidth` | `MinColumnWidth` | Same behavior. |
| `MaxColumnWidth` | `MaxColumnWidth` | Same behavior. |
| `GridLinesVisibility` | `GridLinesVisibility` | Similar enum values. |
| `HorizontalGridLinesBrush` | `HorizontalGridLinesStroke` | Renamed. |
| `VerticalGridLinesBrush` | `VerticalGridLinesStroke` | Renamed. |
| `AlternatingRowBackground` | `AlternateRowBackground` | Renamed. |
| `SelectionMode` | `SelectionMode` | Uses `ListViewSelectionMode`. |
| `CurrentColumn` | *(not directly available)* | Use `CurrentCellSlot.Column` index. |
| `ClipboardCopyMode` | `CanCopy` + `CopyToClipboard` event | Simpler model. |
| *(no equivalent)* | `CanPaste` | WCT DataGrid does not have built-in paste. WinUI.TableView does. |
| *(no equivalent)* | `ShowExportOptions` | WCT DataGrid does not have built-in CSV export. |
| *(no equivalent)* | `ConditionalCellStyles` | WCT DataGrid requires manual style triggers. |
| *(no equivalent)* | `RowContextFlyout` / `CellContextFlyout` | WCT DataGrid has no built-in context flyout. |
| *(no equivalent)* | Column filtering | WCT DataGrid has no built-in filtering. WinUI.TableView has an Excel-like filter flyout. |

## Event mapping

| WCT DataGrid event | WinUI.TableView equivalent | Notes |
|---|---|---|
| `AutoGeneratingColumn` | `AutoGeneratingColumn` | Same purpose. |
| `BeginningEdit` | `BeginningEdit` | Same purpose. Cancelable. |
| `PreparingCellForEdit` | `PreparingCellForEdit` | Same purpose. |
| `CellEditEnding` | `CellEditEnding` | Same purpose. |
| `CellEditEnded` | `CellEditEnded` | Same purpose. |
| `Sorting` (custom event) | `Sorting` | In WCT DataGrid, sorting is not built-in; the event fires but no sort is applied. In WinUI.TableView, the default sort is applied unless `Handled = true`. |
| `SelectionChanged` | `SelectionChanged` + `CellSelectionChanged` | Inherited `SelectionChanged` + new `CellSelectionChanged`. |
| `CopyingRowClipboardContent` | `CopyToClipboard` | Renamed. |
| `ColumnReordered` | `ColumnReordered` | Same purpose. |
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

For custom sort, set `e.Handled = true` in the `Sorting` event and apply your own `SortDescription`.

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
