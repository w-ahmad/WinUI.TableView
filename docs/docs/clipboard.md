# Clipboard and copy/paste

`TableView` supports copying selected rows or cells to the clipboard and pasting tab-delimited clipboard content back into the table.

## When to use it

Use clipboard support to let users:
- Copy table data into Excel, Notepad, or other applications.
- Paste data from Excel or other tab-delimited sources into the table.

## Copying

By default, users can press **Ctrl+C** to copy the selected rows or cells to the clipboard as tab-delimited text. Column headers are included or excluded based on the context menu option chosen.

```xml
<!-- Copy is enabled by default -->
<tv:TableView ItemsSource="{x:Bind Products}" />
```

Disable copying:

```xml
<tv:TableView CanCopy="False" />
```

## Pasting

Paste is enabled by default. Users press **Ctrl+V** to paste tab-delimited clipboard content into the currently selected cells, starting from the top-left selected cell or current cell.

Disable pasting:

```xml
<tv:TableView CanPaste="False" />
```

## CopyToClipboard event

The `CopyToClipboard` event fires before the copy operation. Set `e.Handled = true` to replace the default copy with your own implementation:

```csharp
tableView.CopyToClipboard += (s, e) =>
{
    if (e.IncludeHeaders)
    {
        // Custom copy logic with headers
    }

    e.Handled = true; // Suppress default copy
};
```

`TableViewCopyToClipboardEventArgs` properties:

| Property | Description |
|---|---|
| `IncludeHeaders` | `true` when the user chose "Copy with headers" |
| `Handled` | Set `true` to suppress the default clipboard write |

## PasteFromClipboard event

The `PasteFromClipboard` event fires before the paste operation. Set `e.Handled = true` to suppress the default paste:

```csharp
tableView.PasteFromClipboard += (s, e) =>
{
    // Custom paste logic
    e.Handled = true;
};
```

## Customizing clipboard content per column

Override clipboard content per column using `ClipboardContentBinding`. This lets you specify a different value for clipboard operations than what is displayed in the cell:

```xml
<tv:TableViewTextColumn Header="Price"
                        Binding="{Binding Price}">
    <tv:TableViewTextColumn.ClipboardContentBinding>
        <Binding Path="Price" StringFormat="C" />
    </tv:TableViewTextColumn.ClipboardContentBinding>
</tv:TableViewTextColumn>
```

The `OperationContentBinding` property on `TableViewColumn` also controls the value used for sort, filter, and clipboard operations when set separately from `Binding`.

## Common options

| Property / Event | Description |
|---|---|
| `CanCopy` | Enables or disables Ctrl+C copy (default `true`) |
| `CanPaste` | Enables or disables Ctrl+V paste (default `true`) |
| `CopyToClipboard` | Fires before copying; set `Handled = true` for custom behavior |
| `PasteFromClipboard` | Fires before pasting; set `Handled = true` for custom behavior |

## Notes and limitations

- Copy output is tab-delimited text, which pastes naturally into Excel and similar tools.
- Paste parses tab-delimited text. Each column in the clipboard text is mapped to the corresponding column in the table starting from the current cell.
- Columns where `SetClipboardContent` returns `false` (e.g., no binding path) are skipped during paste.
- The paste operation calls `SetClipboardContent` on `TableViewColumn`. If you have a `TableViewTemplateColumn` without `OperationContentBinding`, paste will not write values to those columns.

## Related articles

- [Export to CSV](export.md)
- [Selection](selection.md)
- [Column types](column-types.md)
