# Context flyouts

`TableView` supports context flyouts at both the row level and the cell level. These flyouts open when the user right-clicks a row or cell (or long-presses on touch).

## When to use it

Use context flyouts to provide quick actions that apply to the right-clicked row or cell — for example, **Edit**, **Delete**, **Copy**, or **View details**.

## Row context flyout

Assign a `FlyoutBase` to `RowContextFlyout`. It opens when the user right-clicks any row:

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

Assign a `FlyoutBase` to `CellContextFlyout`. It opens when the user right-clicks any cell:

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

Use the `RowContextFlyoutOpening` and `CellContextFlyoutOpening` events to access the row/cell context before the flyout opens. You can also use `ForceRowOrCellSelectionOnContextRequested` to ensure the right-clicked item is selected, making `SelectedItem` / `CurrentCellSlot` reliable inside the flyout handler:

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

`TableViewRowContextFlyoutEventArgs` properties:

| Property | Description |
|---|---|
| `Index` | Zero-based index of the row |
| `Row` | The `TableViewRow` control |
| `Item` | The data item for the row |
| `Flyout` | The flyout that will be shown |
| `Handled` | Set `true` to suppress the flyout |

### CellContextFlyoutOpening event args

`TableViewCellContextFlyoutEventArgs` exposes:

| Property | Description |
|---|---|
| `Cell` | The `TableViewCell` control |
| `DataItem` | The data item for the row |
| `Column` | The column of the cell |
| `Flyout` | The flyout that will be shown |
| `Handled` | Set `true` to suppress the flyout |

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
| `RowContextFlyout` | `FlyoutBase` shown on row right-click |
| `CellContextFlyout` | `FlyoutBase` shown on cell right-click |
| `RowContextFlyoutOpening` | Fires before the row flyout opens |
| `CellContextFlyoutOpening` | Fires before the cell flyout opens |
| `ForceRowOrCellSelectionOnContextRequested` | Selects the row/cell before the flyout opens |

## Notes and limitations

- `RowContextFlyout` and `CellContextFlyout` are mutually available. Both can be set at the same time; the cell flyout takes precedence when right-clicking a cell.
- If neither a `RowContextFlyout` nor a `CellContextFlyout` is set, no flyout appears on right-click.
- The opening events fire every time the flyout is about to open, including repeated right-clicks on the same item.

## Related articles

- [Selection](selection.md)
- [Editing](editing.md)
- [Clipboard and copy/paste](clipboard.md)
