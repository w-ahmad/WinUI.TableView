# Export to CSV

`TableView` has built-in support for exporting data to a CSV file. When enabled, export options appear in the table's options menu (the corner button).

## When to use it

Use CSV export when users need to download the displayed data for use in Excel, a reporting tool, or another application.

## Enabling export options

```xml
<tv:TableView ItemsSource="{x:Bind Products}" ShowExportOptions="True" />
```

When [`ShowExportOptions`](xref:WinUI.TableView.TableView.ShowExportOptions) is `true`, the options menu in the top-left corner shows **Export all** and **Export selected** items.

## Export events

Handle the export events to take custom action instead of the default CSV save dialog. Set `e.Handled = true` to suppress the built-in behavior.

### ExportAllContent

Fires when the user chooses "Export all rows":

```csharp
tableView.ExportAllContent += (s, e) =>
{
    // Custom export: send to API instead of saving a file
    ExportToApi(tableView.ItemsSource);
    e.Handled = true;
};
```

### ExportSelectedContent

Fires when the user chooses "Export selected rows":

```csharp
tableView.ExportSelectedContent += (s, e) =>
{
    // Custom export of selected items
    var selected = tableView.SelectedItems.OfType<Product>().ToList();
    ExportToApi(selected);
    e.Handled = true;
};
```

Both events use [`TableViewExportContentEventArgs`](xref:WinUI.TableView.TableViewExportContentEventArgs) which inherits from `HandledEventArgs`.

| Property | Description |
|---|---|
| `Handled` | Set `true` to suppress the default CSV save behavior |

## Common options

| Property | Type | Default | Description |
|---|---|---|---|
| [`ShowExportOptions`](xref:WinUI.TableView.TableView.ShowExportOptions) | `bool` | `false` | Shows export options in the corner button menu |
| [`ExportAllContent`](xref:WinUI.TableView.TableView.ExportAllContent) | event | — | Fires when exporting all rows |
| [`ExportSelectedContent`](xref:WinUI.TableView.TableView.ExportSelectedContent) | event | — | Fires when exporting selected rows |

## Notes and limitations

- The default export opens a **Save As** dialog (`FileSavePicker`) and writes a UTF-8 CSV file.
- Exported values use each column's `GetClipboardContent` method, so [`ClipboardContentBinding`](xref:WinUI.TableView.TableViewColumn.ClipboardContentBinding) and [`OperationContentBinding`](xref:WinUI.TableView.TableViewColumn.OperationContentBinding) affect export output.
- [`ShowExportOptions`](xref:WinUI.TableView.TableView.ShowExportOptions) requires [`CornerButtonMode`](xref:WinUI.TableView.TableView.CornerButtonMode) to be `Options` (the default). If [`CornerButtonMode`](xref:WinUI.TableView.TableView.CornerButtonMode) is set to `None` or `SelectAll`, the options menu is not shown and the export options will not appear.

## Related articles

- [Clipboard and copy/paste](clipboard.md)
- [Selection](selection.md)
