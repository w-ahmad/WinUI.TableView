# Clipboard and copy/paste

`TableView` supports copying selected rows or cells to the clipboard and pasting tab-delimited clipboard content back into the table.

## When to use it

Use clipboard support to let users:
- Copy table data into Excel, Notepad, or other applications.
- Paste data from Excel or other tab-delimited sources into the table.

## Copying

By default, users can press **Ctrl+C** to copy the selected rows or cells to the clipboard as tab-delimited text. To copy with column headers included, users can press **Ctrl+Shift+C** or choose **Copy with Headers** from the context menu.

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

The [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) event fires before the copy operation. Set `e.Handled = true` to replace the default copy with your own implementation:

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

[`TableViewCopyToClipboardEventArgs`](xref:WinUI.TableView.TableViewCopyToClipboardEventArgs) properties:

| Property | Description |
|---|---|
| `IncludeHeaders` | `true` when the user chose **Copy with Headers** from the context menu or pressed **Ctrl+Shift+C** |
| `Handled` | Set `true` to suppress the default clipboard write |

## PasteFromClipboard event

The [`PasteFromClipboard`](xref:WinUI.TableView.TableView.PasteFromClipboard) event fires before the paste operation. Set `e.Handled = true` to suppress the default paste:

```csharp
tableView.PasteFromClipboard += (s, e) =>
{
    // Custom paste logic
    e.Handled = true;
};
```

## Customizing clipboard content per column

Override clipboard content per column using [`ClipboardContentBinding`](xref:WinUI.TableView.TableViewColumn.ClipboardContentBinding). This lets you specify a different value for clipboard operations than what is displayed in the cell:

```xml
<tv:TableViewTextColumn Header="Price"
                        Binding="{Binding Price}">
    <tv:TableViewTextColumn.ClipboardContentBinding>
        <Binding Path="Price" StringFormat="C" />
    </tv:TableViewTextColumn.ClipboardContentBinding>
</tv:TableViewTextColumn>
```

The [`OperationContentBinding`](xref:WinUI.TableView.TableViewColumn.OperationContentBinding) property on [`TableViewColumn`](xref:WinUI.TableView.TableViewColumn) also controls the value used for sort, filter, and clipboard operations when set separately from [`Binding`](xref:WinUI.TableView.TableViewBoundColumn.Binding).

## Common options

| Property / Event | Description |
|---|---|
| [`CanCopy`](xref:WinUI.TableView.TableView.CanCopy) | Enables or disables Ctrl+C copy (default `true`) |
| [`CanPaste`](xref:WinUI.TableView.TableView.CanPaste) | Enables or disables Ctrl+V paste (default `true`) |
| [`CopyToClipboard`](xref:WinUI.TableView.TableView.CopyToClipboard) | Fires before copying; set `Handled = true` for custom behavior |
| [`PasteFromClipboard`](xref:WinUI.TableView.TableView.PasteFromClipboard) | Fires before pasting; set `Handled = true` for custom behavior |

## Notes and limitations

- Copy output is tab-delimited text, which pastes naturally into Excel and similar tools.
- Paste parses tab-delimited text. Each column in the clipboard text is mapped to the corresponding column in the table starting from the current cell.
- Columns where `SetClipboardContent` returns `false` (e.g., no binding path) are skipped during paste.
- The paste operation calls `SetClipboardContent` on [`TableViewColumn`](xref:WinUI.TableView.TableViewColumn). If you have a [`TableViewTemplateColumn`](xref:WinUI.TableView.TableViewTemplateColumn) without [`OperationContentBinding`](xref:WinUI.TableView.TableViewColumn.OperationContentBinding), paste will not write values to those columns.

## Related articles

- [Export to CSV](export.md)
- [Selection](selection.md)
- [Column types](column-types.md)
