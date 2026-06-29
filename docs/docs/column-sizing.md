# Column sizing

`TableView` gives you fine-grained control over how columns measure and size themselves. You can set fixed widths, star widths, auto widths, and per-column size constraints.

## When to use it

Use column sizing to make your table fit its container, give important columns more space, or enforce minimum and maximum widths so the layout stays readable at any window size.

## Basic example

```xml
<tv:TableView ItemsSource="{x:Bind Products}" AutoGenerateColumns="False">
    <tv:TableView.Columns>
        <!-- Fixed pixel width -->
        <tv:TableViewTextColumn   Header="SKU"   Binding="{Binding Sku}"   Width="80" />
        <!-- Star (proportional) width -->
        <tv:TableViewTextColumn   Header="Name"  Binding="{Binding Name}"  Width="2*" />
        <!-- Auto – size to content -->
        <tv:TableViewNumberColumn Header="Price" Binding="{Binding Price}" Width="Auto" />
    </tv:TableView.Columns>
</tv:TableView>
```

## Width values

`Width` is a `GridLength`, the same type used in `Grid.ColumnDefinitions`:

| Value | Meaning |
|---|---|
| `80` or `80px` | Fixed 80 device-independent pixels |
| `Auto` | Size to fit the column's content (see `ColumnAutoWidthMode`) |
| `*` | One star unit — shares remaining space equally |
| `2*` | Two star units — gets twice as much space as `1*` |

## ColumnAutoWidthMode

When `Width="Auto"`, the `ColumnAutoWidthMode` property (available on both the `TableView` and individual columns) controls what content is measured:

| Value | Description |
|---|---|
| `Both` (default) | Width is the maximum of the header width and the widest cell content |
| `Cells` | Width is determined by the widest cell content only |
| `Header` | Width is determined by the header content only |

Set a default for all auto columns at the table level, and override it per column:

```xml
<tv:TableView ColumnAutoWidthMode="Cells">
    <tv:TableView.Columns>
        <!-- Uses table default (Cells) -->
        <tv:TableViewTextColumn Header="Name" Binding="{Binding Name}" Width="Auto" />
        <!-- Override to Both for this column -->
        <tv:TableViewNumberColumn Header="Long Header Name" Binding="{Binding Price}"
                                  Width="Auto"
                                  ColumnAutoWidthMode="Both" />
    </tv:TableView.Columns>
</tv:TableView>
```

## Per-column minimum and maximum width

Use `MinWidth` and `MaxWidth` on a column to clamp its size:

```xml
<tv:TableViewTextColumn Header="Description"
                        Binding="{Binding Description}"
                        Width="*"
                        MinWidth="120"
                        MaxWidth="400" />
```

## Table-level minimum and maximum column width

Set a global constraint that applies to all columns:

```xml
<tv:TableView MinColumnWidth="60" MaxColumnWidth="300" />
```

Individual column `MinWidth` / `MaxWidth` values override the table-level defaults.

## User column resizing

Users can drag column dividers to resize columns. This is enabled by default.

```xml
<!-- Disable resizing for all columns -->
<tv:TableView CanResizeColumns="False" />
```

Disable resizing for a specific column:

```xml
<tv:TableViewTextColumn Header="ID" Binding="{Binding Id}" CanResize="False" />
```

## Row and header row heights

Control row heights with the following `TableView` properties:

| Property | Default | Description |
|---|---|---|
| `RowHeight` | `NaN` (auto) | Fixed height for all data rows |
| `RowMinHeight` | `40` | Minimum height for data rows |
| `RowMaxHeight` | `∞` | Maximum height for data rows |
| `HeaderRowHeight` | `NaN` (auto) | Fixed height for the header row |
| `HeaderRowMinHeight` | `32` | Minimum header row height |
| `HeaderRowMaxHeight` | `∞` | Maximum header row height |

```xml
<tv:TableView RowHeight="48" HeaderRowHeight="40" />
```

## Notes and limitations

- Star widths require the `TableView` to have a defined width (or be inside a container that constrains it). In an unconstrained panel they fall back to `Auto`.
- `ActualWidth` is a read-only property on each column that reflects the column's rendered width.
- `ColumnAutoWidthMode` measures visible cells only. If the widest content is in a row that has not been rendered yet (due to virtualization), the column may be narrower than expected.

## Related articles

- [Defining columns](defining-columns.md)
- [Column types](column-types.md)
- [Column reordering](column-reordering.md)
