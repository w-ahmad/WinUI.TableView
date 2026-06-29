# Events and commands reference

This page lists all public events exposed by `TableView` and related types.

## TableView events

### Data and columns

| Event | Args type | Description |
|---|---|---|
| `AutoGeneratingColumn` | `TableViewAutoGeneratingColumnEventArgs` | Fires for each column being auto-generated. Can be cancelled. |

### Editing lifecycle

| Event | Args type | Description |
|---|---|---|
| `BeginningEdit` | `TableViewBeginningEditEventArgs` | Fires before a cell enters edit mode. Set `Cancel = true` to block. |
| `PreparingCellForEdit` | `TableViewPreparingCellForEditEventArgs` | Fires after the editing element is created. |
| `CellEditEnding` | `TableViewCellEditEndingEventArgs` | Fires just before a commit or cancel. Set `Cancel = true` to stay in edit mode. |
| `CellEditEnded` | `TableViewCellEditEndedEventArgs` | Fires after a cell edit is committed or cancelled. |
| `IsReadOnlyChanged` | `DependencyPropertyChangedEventArgs` | Fires when `IsReadOnly` changes. |

### Selection

| Event | Args type | Description |
|---|---|---|
| `CellSelectionChanged` | `TableViewCellSelectionChangedEventArgs` | Fires when the set of selected cells changes. Provides added and removed cell slots. |
| `CurrentCellChanged` | `DependencyPropertyChangedEventArgs` | Fires when `CurrentCellSlot` changes. |

### Sorting

| Event | Args type | Description |
|---|---|---|
| `Sorting` | `TableViewSortingEventArgs` | Fires before the default sort runs. Set `Handled = true` for custom sort. |
| `ClearSorting` | `TableViewClearSortingEventArgs` | Fires when a column's sort direction is cleared. |

### Clipboard and export

| Event | Args type | Description |
|---|---|---|
| `CopyToClipboard` | `TableViewCopyToClipboardEventArgs` | Fires before the default copy. Set `Handled = true` for custom copy. |
| `PasteFromClipboard` | `TableViewPasteFromClipboardEventArgs` | Fires before the default paste. Set `Handled = true` for custom paste. |
| `ExportAllContent` | `TableViewExportContentEventArgs` | Fires when "Export all" is chosen. Set `Handled = true` for custom export. |
| `ExportSelectedContent` | `TableViewExportContentEventArgs` | Fires when "Export selected" is chosen. Set `Handled = true` for custom export. |

### Context flyouts

| Event | Args type | Description |
|---|---|---|
| `RowContextFlyoutOpening` | `TableViewRowContextFlyoutEventArgs` | Fires before the row context flyout opens. Set `Handled = true` to suppress. |
| `CellContextFlyoutOpening` | `TableViewCellContextFlyoutEventArgs` | Fires before the cell context flyout opens. Set `Handled = true` to suppress. |

### User interaction

| Event | Args type | Description |
|---|---|---|
| `RowDoubleTapped` | `TableViewRowDoubleTappedEventArgs` | Fires when a row is double-tapped. |
| `CellDoubleTapped` | `TableViewCellDoubleTappedEventArgs` | Fires when a cell is double-tapped. |

### Column reordering

| Event | Args type | Description |
|---|---|---|
| `ColumnReordering` | `TableViewColumnReorderingEventArgs` | Fires before a column is moved. Set `Cancel = true` to prevent. |
| `ColumnReordered` | `TableViewColumnReorderedEventArgs` | Fires after a column has been moved. |

---

## Event args reference

### TableViewAutoGeneratingColumnEventArgs

| Property | Type | Description |
|---|---|---|
| `PropertyName` | `string` | Name of the data property |
| `PropertyType` | `Type?` | Runtime type of the property |
| `Column` | `TableViewColumn` | The column being generated; replaceable |
| `Cancel` (inherited) | `bool` | Set `true` to skip the column |

### TableViewBeginningEditEventArgs

| Property | Type | Description |
|---|---|---|
| `Cell` | `TableViewCell` | The cell entering edit mode |
| `DataItem` | `object?` | The row's data object |
| `Column` | `TableViewColumn` | The column |
| `EditingArgs` | `RoutedEventArgs` | The triggering input event |
| `Cancel` (inherited) | `bool` | Set `true` to block editing |

### TableViewCellEditEndingEventArgs

| Property | Type | Description |
|---|---|---|
| `Cell` | `TableViewCell` | The cell in edit mode |
| `DataItem` | `object?` | The row's data object |
| `Column` | `TableViewColumn` | The column |
| `EditingElement` | `FrameworkElement` | The editing control |
| `EditAction` | `TableViewEditAction` | `Commit` or `Cancel` |
| `Cancel` (inherited) | `bool` | Set `true` to stay in edit mode |

### TableViewCellSelectionChangedEventArgs

| Property | Type | Description |
|---|---|---|
| `AddedCells` | `IList<TableViewCellSlot>` | Newly selected cells |
| `RemovedCells` | `IList<TableViewCellSlot>` | Newly deselected cells |

### TableViewSortingEventArgs

| Property | Type | Description |
|---|---|---|
| `Column` | `TableViewColumn` | The column being sorted |
| `Handled` (inherited) | `bool` | Set `true` to suppress default sort |

### TableViewCopyToClipboardEventArgs

| Property | Type | Description |
|---|---|---|
| `IncludeHeaders` | `bool` | Whether headers are included in the copy |
| `Handled` (inherited) | `bool` | Set `true` to suppress default copy |

### TableViewRowContextFlyoutEventArgs

| Property | Type | Description |
|---|---|---|
| `Index` | `int` | Row index |
| `Row` | `TableViewRow` | The row control |
| `Item` | `object` | The data item |
| `Flyout` | `FlyoutBase?` | The flyout to be shown |
| `Handled` (inherited) | `bool` | Set `true` to suppress the flyout |

### TableViewColumnReorderingEventArgs

| Property | Type | Description |
|---|---|---|
| `Column` | `TableViewColumn` | The column being moved |
| `Cancel` (inherited) | `bool` | Set `true` to prevent the move |

### TableViewColumnReorderedEventArgs

| Property | Type | Description |
|---|---|---|
| `Column` | `TableViewColumn` | The column that was moved |
| `OldIndex` | `int` | Previous index in `Columns` |
| `NewIndex` | `int` | New index in `Columns` |

---

## Enums

### TableViewEditAction

| Value | Description |
|---|---|
| `Commit` | The edit was confirmed |
| `Cancel` | The edit was cancelled |

### TableViewSelectionUnit

| Value | Description |
|---|---|
| `CellOrRow` | Cell and row selection are independent |
| `CellWithRow` | Selecting a cell also selects its row |
| `Cell` | Cell-only selection |
| `Row` | Row-only selection |

### TableViewGridLinesVisibility

| Value | Description |
|---|---|
| `All` | Both horizontal and vertical lines |
| `Horizontal` | Horizontal lines only |
| `Vertical` | Vertical lines only |
| `None` | No lines |

### TableViewRowDetailsVisibilityMode

| Value | Description |
|---|---|
| `Collapsed` | Details never shown |
| `Visible` | Details always shown |
| `VisibleWhenSelected` | Details shown for selected row(s) |
| `VisibleWhenExpanded` | Details shown when expanded via toggle |

### TableViewHeadersVisibility

| Value | Description |
|---|---|
| `All` | Both row and column headers |
| `Columns` | Column headers only |
| `Rows` | Row headers only |
| `None` | No headers |

### TableViewCornerButtonMode

| Value | Description |
|---|---|
| `None` | No corner button |
| `SelectAll` | Select-all button |
| `Options` | Options menu button |

### TableViewColumnAutoWidthMode

| Value | Description |
|---|---|
| `Both` | Max of header and cell widths |
| `Cells` | Cell content width only |
| `Header` | Header content width only |

## Related articles

- [Editing](editing.md)
- [Selection](selection.md)
- [Sorting](sorting.md)
- [Filtering](filtering.md)
- [Clipboard and copy/paste](clipboard.md)
- [Export to CSV](export.md)
- [Context flyouts](context-flyouts.md)
- [Column reordering](column-reordering.md)
