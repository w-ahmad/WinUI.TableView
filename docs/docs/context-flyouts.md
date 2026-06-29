# Context flyouts

`TableView` supports context flyouts at both the row level and the cell level. These flyouts open when the user right-clicks a row or cell (or long-presses on touch).

## When to use it

Use context flyouts to provide quick actions that apply to the right-clicked row or cell — for example, **Edit**, **Delete**, **Copy**, or **View details**.

## Row context flyout

Assign a `FlyoutBase` to [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout). It opens when the user right-clicks any row:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.RowContextFlyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="Edit"   Click="OnEditRow" />
            <MenuFlyoutItem Text="Delete" Click="OnDeleteRow" />
            <MenuFlyoutSeparator />
            <MenuFlyoutItem Text="Copy Row" Click="OnCopyRow" />
        </MenuFlyout>
    </tv:TableView.RowContextFlyout>
</tv:TableView>
```

## Cell context flyout

Assign a `FlyoutBase` to [`CellContextFlyout`](xref:WinUI.TableView.TableView.CellContextFlyout). It opens when the user right-clicks any cell:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.CellContextFlyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="Copy Cell Value" Click="OnCopyCellValue" />
        </MenuFlyout>
    </tv:TableView.CellContextFlyout>
</tv:TableView>
```

## Accessing the row or cell from a flyout handler

Use the [`RowContextFlyoutOpening`](xref:WinUI.TableView.TableView.RowContextFlyoutOpening) and [`CellContextFlyoutOpening`](xref:WinUI.TableView.TableView.CellContextFlyoutOpening) events to access the row/cell context before the flyout opens. You can also use [`ForceRowOrCellSelectionOnContextRequested`](xref:WinUI.TableView.TableView.ForceRowOrCellSelectionOnContextRequested) to ensure the right-clicked item is selected, making `SelectedItem` / [`CurrentCellSlot`](xref:WinUI.TableView.TableView.CurrentCellSlot) reliable inside the flyout handler:

```xml
<tv:TableView ForceRowOrCellSelectionOnContextRequested="True"
              RowContextFlyoutOpening="OnRowContextFlyoutOpening">
    <tv:TableView.RowContextFlyout>
        <MenuFlyout>
            <MenuFlyoutItem x:Name="DeleteMenuItem" Text="Delete" Click="OnDeleteRow" />
        </MenuFlyout>
    </tv:TableView.RowContextFlyout>
</tv:TableView>
```

```csharp
private void OnRowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
{
    // Access the row and item
    var item = e.Item as Product;
    var rowIndex = e.Index;

    // Disable delete for locked items
    DeleteMenuItem.IsEnabled = item?.IsLocked == false;
}
```

### RowContextFlyoutOpening event args

[`TableViewRowContextFlyoutEventArgs`](xref:WinUI.TableView.TableViewRowContextFlyoutEventArgs) properties:

| Property | Description |
|---|---|
| [`Index`](xref:WinUI.TableView.TableViewRowContextFlyoutEventArgs.Index) | Zero-based index of the row |
| [`Row`](xref:WinUI.TableView.TableViewRowContextFlyoutEventArgs.Row) | The [`TableViewRow`](xref:WinUI.TableView.TableViewRow) control |
| [`Item`](xref:WinUI.TableView.TableViewRowContextFlyoutEventArgs.Item) | The data item for the row |
| [`Flyout`](xref:WinUI.TableView.TableViewRowContextFlyoutEventArgs.Flyout) | The flyout that will be shown |
| `Handled` | Set `true` to suppress the flyout |

### CellContextFlyoutOpening event args

[`TableViewCellContextFlyoutEventArgs`](xref:WinUI.TableView.TableViewCellContextFlyoutEventArgs) exposes:

| Property | Description |
|---|---|
| [`Slot`](xref:WinUI.TableView.TableViewCellContextFlyoutEventArgs.Slot) | The cell slot (row and column indices) |
| [`Cell`](xref:WinUI.TableView.TableViewCellContextFlyoutEventArgs.Cell) | The [`TableViewCell`](xref:WinUI.TableView.TableViewCell) control |
| [`Item`](xref:WinUI.TableView.TableViewCellContextFlyoutEventArgs.Item) | The data item for the row |
| [`Flyout`](xref:WinUI.TableView.TableViewCellContextFlyoutEventArgs.Flyout) | The flyout that will be shown |
| `Handled`| Set `true` to suppress the flyout |

## Suppressing the flyout conditionally

Set `e.Handled = true` in the opening event to prevent the flyout from appearing:

```csharp
tableView.RowContextFlyoutOpening += (s, e) =>
{
    if (e.Item is Product { IsLocked: true })
    {
        e.Handled = true; // No flyout for locked products
    }
};
```

## Common options

| Property / Event | Description |
|---|---|
| [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout) | `FlyoutBase` shown on row right-click |
| [`CellContextFlyout`](xref:WinUI.TableView.TableView.CellContextFlyout) | `FlyoutBase` shown on cell right-click |
| [`RowContextFlyoutOpening`](xref:WinUI.TableView.TableView.RowContextFlyoutOpening) | Fires before the row flyout opens |
| [`CellContextFlyoutOpening`](xref:WinUI.TableView.TableView.CellContextFlyoutOpening) | Fires before the cell flyout opens |
| [`ForceRowOrCellSelectionOnContextRequested`](xref:WinUI.TableView.TableView.ForceRowOrCellSelectionOnContextRequested) | Selects the row/cell before the flyout opens |

## Notes and limitations

- [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout) and [`CellContextFlyout`](xref:WinUI.TableView.TableView.CellContextFlyout) are mutually available. Both can be set at the same time; the cell flyout takes precedence when right-clicking a cell.
- If neither a [`RowContextFlyout`](xref:WinUI.TableView.TableView.RowContextFlyout) nor a [`CellContextFlyout`](xref:WinUI.TableView.TableView.CellContextFlyout) is set, no flyout appears on right-click.
- The opening events fire every time the flyout is about to open, including repeated right-clicks on the same item.

## Related articles

- [Selection](selection.md)
- [Editing](editing.md)
- [Clipboard and copy/paste](clipboard.md)
