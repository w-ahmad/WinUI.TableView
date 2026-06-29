# Selection

`TableView` supports selecting individual cells, whole rows, or a combination of both. You control the selection behavior through [`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) and [`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit).

## When to use it

- Use **row selection** for list-style UIs where you act on entire records.
- Use **cell selection** for spreadsheet-style UIs where you need to reference or copy individual values.
- Use **combined** modes for data grids where users can do both.

## SelectionMode

[`SelectionMode`](xref:WinUI.TableView.TableView.SelectionMode) inherits from `ListViewSelectionMode` and controls how many rows (or cells) can be selected at once:

| Value | Description |
|---|---|
| `None` | Selection is disabled |
| `Single` | Only one row/cell can be selected at a time |
| `Multiple` | Multiple items can be selected individually |
| `Extended` (default) | Click to select; Shift+Click to range-select; Ctrl+Click to toggle |

```xml
<tv:TableView SelectionMode="Single" />
```

## SelectionUnit

[`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit) determines what the user can select:

| Value | Description |
|---|---|
| `CellOrRow` (default) | Clicking a cell selects that cell; clicking a row header selects the row. Cell selection and row selection are independent. |
| `CellWithRow` | Clicking a cell selects the cell and also selects the owning row. |
| `Cell` | Only cells can be selected; row headers do not select rows. |
| `Row` | Only rows can be selected; clicking a cell selects the row. |

```xml
<tv:TableView SelectionUnit="Row" SelectionMode="Single" />
```

## Reading selected items

For row selection, use the standard `ListView` properties:

```csharp
// Single selected item
var item = tableView.SelectedItem as Product;

// Multiple selected items
foreach (var item in tableView.SelectedItems.OfType<Product>())
{
    // ...
}
```

For cell selection, use `SelectedCells` or listen to [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged):

```csharp
tableView.CellSelectionChanged += (s, e) =>
{
    foreach (var slot in e.AddedCells)
    {
        int row = slot.Row;
        int col = slot.Column;
        Console.WriteLine($"Selected cell ({row}, {col})");
    }

    foreach (var slot in e.RemovedCells)
    {
        Console.WriteLine($"Deselected cell ({row}, {col})");
    }
};
```

## TableViewCellSlot

A [`TableViewCellSlot`](xref:WinUI.TableView.TableViewCellSlot) is a lightweight `record struct` that identifies a cell by its row and column indices:

```csharp
public readonly record struct TableViewCellSlot(int Row, int Column);
```

```csharp
// Navigate to a specific cell
tableView.CurrentCellSlot = new TableViewCellSlot(2, 1);
```

## CurrentCellSlot

[`CurrentCellSlot`](xref:WinUI.TableView.TableView.CurrentCellSlot) is a nullable dependency property that identifies the currently focused cell:

```csharp
var current = tableView.CurrentCellSlot;
if (current.HasValue)
{
    Console.WriteLine($"Current cell: row {current.Value.Row}, column {current.Value.Column}");
}
```

Handle [`CurrentCellChanged`](xref:WinUI.TableView.TableView.CurrentCellChanged) to react when focus moves to a different cell:

```csharp
tableView.CurrentCellChanged += (s, e) =>
{
    var slot = tableView.CurrentCellSlot;
};
```

## Corner button (Select All)

Set [`CornerButtonMode`](xref:WinUI.TableView.TableView.CornerButtonMode) to `SelectAll` to show a button in the top-left corner that selects all rows:

```xml
<tv:TableView CornerButtonMode="SelectAll" />
```

| Value | Description |
|---|---|
| `None` | No corner button |
| `SelectAll` | Shows a select-all button |
| `Options` (default) | Shows a table-level options menu |

## ForceRowOrCellSelectionOnContextRequested

When showing a context menu, you may want to ensure the right-clicked row or cell becomes selected. Set this property to `true`:

```xml
<tv:TableView ForceRowOrCellSelectionOnContextRequested="True" />
```

## Cell selection changed event

```csharp
tableView.CellSelectionChanged += (s, e) =>
{
    // e.AddedCells: newly selected cells
    // e.RemovedCells: newly deselected cells
};
```

## Notes and limitations

- When [`SelectionUnit`](xref:WinUI.TableView.TableView.SelectionUnit) is `Row`, the `SelectedCells` collection and [`CellSelectionChanged`](xref:WinUI.TableView.TableView.CellSelectionChanged) are not used.
- Drag selection is supported for cell selection. The selection rectangle is shown by default; disable it with `ShowDragRectangle = false`.
- In `CellOrRow` mode, row and cell selections are tracked independently. A selected row does not mean its cells are in `SelectedCells`.

## Related articles

- [Row headers](row-headers.md)
- [Clipboard and copy/paste](clipboard.md)
- [Editing](editing.md)
