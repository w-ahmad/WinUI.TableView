# Accessibility

`TableView` exposes a complete UI Automation (UIA) tree so that screen readers, keyboard-only users, and accessibility testing tools can navigate and interact with the control. Custom `AutomationPeer` classes are registered for every interactive element: the table itself, rows, cells, column headers, and row headers.

## When to use it

You do not need to opt in. Accessibility support is always active. This page explains what is exposed, how to improve automation names for custom content, and what limitations apply.

## Supported UIA patterns

| Element | UIA patterns |
|---|---|
| [`TableView`](xref:WinUI.TableView.TableView) | `GridPattern`, `TablePattern`, `SelectionPattern` (inherited from `ListView`) |
| [`TableViewRow`](xref:WinUI.TableView.TableViewRow) | `SelectionItemPattern` (inherited), `ExpandCollapsePattern` when `RowDetailsVisibilityMode` is `VisibleWhenExpanded` |
| [`TableViewCell`](xref:WinUI.TableView.TableViewCell) | `GridItemPattern`, `TableItemPattern`, `SelectionItemPattern` |
| [`TableViewColumnHeader`](xref:WinUI.TableView.TableViewColumnHeader) | `InvokePattern` when [`CanSort`](xref:WinUI.TableView.TableViewColumn.CanSort) is `true` |
| `TableViewRowHeader` | Structural header element |

### GridPattern / TablePattern (TableView)

`TableView` implements `IGridProvider` and `ITableProvider`:

- **`RowCount`** — number of data rows currently loaded
- **`ColumnCount`** — number of visible columns
- **`GetItem(row, column)`** — returns the automation element for the specified cell (only for realized, on-screen cells)
- **`GetColumnHeaders()`** — returns automation elements for all visible column headers
- **`RowOrColumnMajor`** — always `RowMajor`

### GridItemPattern / TableItemPattern (cells)

Each `TableViewCell` exposes `IGridItemProvider` and `ITableItemProvider`:

- **`Row`** — zero-based row index
- **`Column`** — zero-based column index
- **`RowSpan` / `ColumnSpan`** — always 1
- **`ContainingGrid`** — the owning `TableView`
- **`GetColumnHeaderItems()`** — the column header for this cell

### SelectionItemPattern (cells and rows)

- `IsSelected` reflects the current selection state
- `Select()`, `AddToSelection()`, and `RemoveFromSelection()` manipulate the selection programmatically
- `SelectionContainer` returns the owning `TableView`
- Row `SelectionItemPattern` is provided automatically by the base `ListView` infrastructure

### InvokePattern (column headers)

When a column is sortable ([`CanSort`](xref:WinUI.TableView.TableViewColumn.CanSort) is `true`), its header exposes `IInvokeProvider`. Invoking the header cycles the sort direction: ascending → descending → unsorted.

### ExpandCollapsePattern (row details)

When [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode) is `VisibleWhenExpanded`, each row exposes `IExpandCollapseProvider` so automation clients can expand or collapse the details pane programmatically.

## Automation names

### TableView

Uses [`AutomationProperties.Name`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties.name) if set; otherwise inherits from the base `ListView` peer.

```xml
<tv:TableView AutomationProperties.Name="Products table" />
```

### Column headers

Format: `"{headerText}"`, with an optional sort-state suffix and filter suffix when applicable.

| State | Automation name example |
|---|---|
| Unsorted, unfiltered | `"Name"` |
| Sorted ascending | `"Name, Sort Ascending"` |
| Sorted descending | `"Name, Sort Descending"` |
| Sorted and filtered | `"Name, Sort Descending, Filtered"` |

### Rows

Format: `"Row {N}"` (1-based index). The help text contains a summary of all cells in the row, for example `"Name: Waheed Ahmad, Age: 30, City: London"`.

### Cells

Format: `"{columnHeader}, Row {N}, {cellValue}"`.

Examples:

- `"Name, Row 3, Waheed Ahmad"`
- `"Salary, Row 7, 95000"`

Override the name for a specific column by setting `AutomationProperties.Name` through a `CellStyle`:

```xml
<tv:TableViewTextColumn Header="Status">
    <tv:TableViewTextColumn.CellStyle>
        <Style TargetType="tv:TableViewCell">
            <Setter Property="AutomationProperties.Name"
                    Value="{Binding StatusDescription}" />
        </Style>
    </tv:TableViewTextColumn.CellStyle>
</tv:TableViewTextColumn>
```

### Row headers

Uses `AutomationProperties.Name` if set on the `TableViewRowHeader`, otherwise derives the name from the header's content, or falls back to `"Row {N}"`.

## Keyboard navigation

| Key | Action |
|---|---|
| Arrow keys | Move the current cell or row |
| **Tab** / **Shift+Tab** | Move to the next / previous cell |
| **Enter** | Move to the next row cell, or commit an edit |
| **Space** | Select or deselect the current cell or row |
| **F2** | Begin editing the current cell |
| **Escape** | Cancel editing and return focus to the current cell |
| **Home** / **End** | Move to the first / last column in the row |
| **Ctrl+Home** / **Ctrl+End** | Move to the first / last row |
| **Page Up** / **Page Down** | Move by one page |
| **Ctrl+A** | Select all |
| **Ctrl+Shift+A** | Deselect all |
| **Ctrl+C** | Copy to clipboard |

## Screen reader behavior

When navigating with a screen reader such as Narrator:

1. Moving to the **TableView** announces it as a table with its row and column count.
2. Moving between **rows** announces the row number and a summary of its content.
3. Moving between **cells** announces the column header, row number, and current value.
4. **Column headers** announce their header text, current sort state, and filter state.
5. **Editing**: pressing **F2** (or double-tapping) enters edit mode. Focus moves to the editing control (for example, a `TextBox`), which announces itself independently with its own value. Pressing **Escape** returns focus to the cell, keeping Narrator on the correct cell.
6. **Selection changes** are announced through `SelectionItemPattern.IsSelected`.

## Custom automation names for template columns

`TableViewTemplateColumn` cells display developer-defined content. The cell automation name defaults to `"{columnHeader}, Row {N}"` unless overridden. Set `AutomationProperties.Name` directly on the root element of the template:

```xml
<tv:TableViewTemplateColumn Header="Actions">
    <tv:TableViewTemplateColumn.CellTemplate>
        <DataTemplate>
            <Button Content="Edit"
                    AutomationProperties.Name="Edit row" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.CellTemplate>
</tv:TableViewTemplateColumn>
```

## Custom column header names

To override the automation name of a column header (for example, to provide a longer description for an abbreviated header):

```xml
<tv:TableViewTextColumn Header="DOB">
    <tv:TableViewTextColumn.HeaderStyle>
        <Style TargetType="tv:TableViewColumnHeader">
            <Setter Property="AutomationProperties.Name"
                    Value="Date of Birth" />
        </Style>
    </tv:TableViewTextColumn.HeaderStyle>
</tv:TableViewTextColumn>
```

## Notes and limitations

- **Virtualized cells**: `IGridProvider.GetItem(row, column)` returns `null` for rows that are not currently in the visual tree. Scroll the target row into view before calling `GetItem`.
- **IValueProvider on cells**: `IValueProvider` is not implemented on the cell peer. Instead, the editing element (for example, a `TextBox`) exposes `IValueProvider` while the cell is in edit mode.
- **Custom column types**: cells in `TableViewTemplateColumn` show developer-defined content. Accessible names for those cells default to the column header and row index unless the template explicitly sets `AutomationProperties.Name`.
- **Non-Windows platforms**: automation peers are compiled and run on all platforms (WinUI and Uno Platform). However, not all Uno Platform targets expose a full UIA tree to assistive technologies. On WebAssembly (WASM), the platform relies on ARIA attributes managed by Uno. On Desktop Skia targets, accessibility support depends on the native platform's accessibility APIs.
- **Group headers**: [`TableViewGroupHeaderRow`](xref:WinUI.TableView.TableViewGroupHeaderRow) does not have a dedicated automation peer; it relies on the base `ListViewHeaderItem` automation behavior. Its default automation name is derived from the group's key ([`TableViewGroupInfo.Key`](xref:WinUI.TableView.TableViewGroupInfo.Key)), not the item count shown in the UI.

## Related articles

- [Selection](selection.md)
- [Editing](editing.md)
- [Sorting](sorting.md)
- [Grouping](grouping.md)
- [Filtering](filtering.md)
- [Row details](row-details.md)
- [Row headers](row-headers.md)
- [Styling rows, cells, and headers](styling.md)

