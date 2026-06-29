# Defining columns

You can let `TableView` generate columns automatically from the data source, or define them explicitly in XAML or code.

## When to use it

- Use **auto-generated columns** for rapid prototyping or admin-style UIs where every property should appear as a column.
- Use **explicit columns** when you need to control the column order, use specific column types, set headers, widths, or configure sorting and filtering per column.

## Auto-generated columns

Set `AutoGenerateColumns="True"` (the default). The control inspects the public properties of the first item in the source and creates one column per property. The column type is chosen based on the property type:

| Property type | Generated column type |
|---|---|
| `string` | [`TableViewTextColumn`](xref:WinUI.TableView.TableViewTextColumn) |
| numeric types (`int`, `double`, etc.) | [`TableViewNumberColumn`](xref:WinUI.TableView.TableViewNumberColumn) |
| `bool` | [`TableViewCheckBoxColumn`](xref:WinUI.TableView.TableViewCheckBoxColumn) |
| `DateOnly`, `DateTime`, `DateTimeOffset` | [`TableViewDateColumn`](xref:WinUI.TableView.TableViewDateColumn) |
| `TimeOnly`, `TimeSpan` | [`TableViewTimeColumn`](xref:WinUI.TableView.TableViewTimeColumn) |
| `Uri` | [`TableViewHyperlinkColumn`](xref:WinUI.TableView.TableViewHyperlinkColumn) |
| Other | [`TableViewTextColumn`](xref:WinUI.TableView.TableViewTextColumn) |

```xml
<tv:TableView ItemsSource="{x:Bind Products}" AutoGenerateColumns="True" />
```

### Handling AutoGeneratingColumn

Use the [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) event to customize or cancel individual columns:

```csharp
tableView.AutoGeneratingColumn += (s, e) =>
{
    // Skip the Id property
    if (e.PropertyName == "Id")
    {
        e.Cancel = true;
        return;
    }

    // Rename the header
    if (e.PropertyName == "Price")
    {
        e.Column.Header = "Unit Price ($)";
    }
};
```

[`TableViewAutoGeneratingColumnEventArgs`](xref:WinUI.TableView.TableViewAutoGeneratingColumnEventArgs) properties:

| Property | Description |
|---|---|
| [`PropertyName`](xref:WinUI.TableView.TableViewAutoGeneratingColumnEventArgs.PropertyName) | Name of the data property |
| [`PropertyType`](xref:WinUI.TableView.TableViewAutoGeneratingColumnEventArgs.PropertyType) | Runtime type of the property |
| [`Column`](xref:WinUI.TableView.TableViewAutoGeneratingColumnEventArgs.Column) | The column being generated; you can replace it with a different column instance |
| `Cancel` | Set `true` to skip this column |

## Explicit columns

Set `AutoGenerateColumns="False"` and populate `TableView.Columns` in XAML:

```xml
<tv:TableView ItemsSource="{x:Bind Products}" AutoGenerateColumns="False">
    <tv:TableView.Columns>
        <tv:TableViewTextColumn   Header="Product Name" Binding="{Binding Name}"    Width="200" />
        <tv:TableViewNumberColumn Header="Price"        Binding="{Binding Price}"   Width="100" />
        <tv:TableViewCheckBoxColumn Header="In Stock"   Binding="{Binding InStock}" Width="80" />
    </tv:TableView.Columns>
</tv:TableView>
```

## Column order

Columns render in the order they appear in `TableView.Columns`. You can also set the [`Order`](xref:WinUI.TableView.TableViewColumn.Order) property on any column to explicitly control its position:

```xml
<tv:TableViewTextColumn Header="Name" Binding="{Binding Name}" Order="0" />
<tv:TableViewTextColumn Header="SKU"  Binding="{Binding Sku}"  Order="2" />
```

Columns without an explicit [`Order`](xref:WinUI.TableView.TableViewColumn.Order) fall after columns that have one.

## Adding columns in code

```csharp
tableView.Columns.Add(new TableViewTextColumn
{
    Header = "Name",
    Binding = new Binding { Path = new PropertyPath("Name") }
});
```

## Common column properties

These properties are available on every column type ([`TableViewColumn`](xref:WinUI.TableView.TableViewColumn) base class):

| Property | Type | Description |
|---|---|---|
| [`Header`](xref:WinUI.TableView.TableViewColumn.Header) | `object` | Column header content |
| [`Width`](xref:WinUI.TableView.TableViewColumn.Width) | `GridLength` | Column width (`Auto`, `*`, or pixel value) |
| [`MinWidth`](xref:WinUI.TableView.TableViewColumn.MinWidth) | `double?` | Minimum column width |
| [`MaxWidth`](xref:WinUI.TableView.TableViewColumn.MaxWidth) | `double?` | Maximum column width |
| [`IsReadOnly`](xref:WinUI.TableView.TableView.IsReadOnly) | `bool` | Overrides TableView.IsReadOnly for this column |
| [`CanSort`](xref:WinUI.TableView.TableViewColumn.CanSort) | `bool` | Whether this column can be sorted |
| [`CanFilter`](xref:WinUI.TableView.TableViewColumn.CanFilter) | `bool` | Whether this column can be filtered |
| [`CanResize`](xref:WinUI.TableView.TableViewColumn.CanResize) | `bool` | Whether users can resize this column |
| [`CanReorder`](xref:WinUI.TableView.TableViewColumn.CanReorder) | `bool` | Whether users can drag this column to a new position |
| `Visibility` | `Visibility` | Show or hide the column |
| [`Order`](xref:WinUI.TableView.TableViewColumn.Order) | `int?` | Display order override |
| `Tag` | `object` | Custom tag object |
| [`HeaderStyle`](xref:WinUI.TableView.TableViewColumn.HeaderStyle) | `Style` | Style for the column header |
| [`CellStyle`](xref:WinUI.TableView.TableView.CellStyle) | `Style` | Style applied to every cell in this column |
| [`IsAutoGenerated`](xref:WinUI.TableView.TableViewColumn.IsAutoGenerated) | `bool` | `true` when the column was created by [`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) |

## Notes and limitations

- [`AutoGenerateColumns`](xref:WinUI.TableView.TableView.AutoGenerateColumns) and explicit [`Columns`](xref:WinUI.TableView.TableView.Columns) are mutually exclusive. Setting `AutoGenerateColumns="False"` and also populating [`Columns`](xref:WinUI.TableView.TableView.Columns) in XAML is the standard pattern for explicit definitions.
- You can mix auto-generated and manually added columns: set `AutoGenerateColumns="True"`, handle [`AutoGeneratingColumn`](xref:WinUI.TableView.TableView.AutoGeneratingColumn) to cancel certain properties, and then add custom columns in code.
- Columns are not virtualized; all column header controls are created even if they are off-screen horizontally.

## Related articles

- [Column types](column-types.md)
- [Column sizing](column-sizing.md)
- [Sorting](sorting.md)
- [Filtering](filtering.md)
