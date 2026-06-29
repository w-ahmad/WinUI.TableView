# Conditional cell styling

Conditional cell styling lets you apply a `Style` to individual cells based on a predicate function. This is useful for highlighting out-of-range values, flagging errors, or color-coding categories.

## When to use it

Use conditional cell styling when you want specific cells to look different based on their value or the row's data — for example, making negative prices appear in red, or highlighting expired dates.

## Basic example

Define a [`TableViewConditionalCellStyle`](xref:WinUI.TableView.TableViewConditionalCellStyle) with a `Predicate` and a `Style`. Add it to the [`ConditionalCellStyles`](xref:WinUI.TableView.TableView.ConditionalCellStyles) collection on the `TableView` or on an individual column.

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.ConditionalCellStyles>
        <tv:TableViewConditionalCellStylesCollection>
            <tv:TableViewConditionalCellStyle Predicate="{x:Bind IsLowStock}">
                <Style TargetType="tv:TableViewCell">
                    <Setter Property="Background" Value="#FFEBE9" />
                    <Setter Property="Foreground" Value="#D1242F" />
                </Style>
            </tv:TableViewConditionalCellStyle>
        </tv:TableViewConditionalCellStylesCollection>
    </tv:TableView.ConditionalCellStyles>
</tv:TableView>
```

The `Predicate` property takes a `Predicate<TableViewConditionalCellStyleContext>`, which provides the column and data item for evaluation.

## Defining predicates in code

The easiest way to supply predicates is from the code-behind or ViewModel. With `x:Bind`, you can bind to a method directly:

```csharp
// In MainWindow.xaml.cs
public bool IsLowStock(TableViewConditionalCellStyleContext ctx)
{
    // Apply only to the Stock column
    if (ctx.Column.Header?.ToString() != "Stock")
        return false;

    return ctx.DataItem is Product p && p.Stock < 10;
}
```

```xml
<tv:TableViewConditionalCellStyle Predicate="{x:Bind IsLowStock}">
    <Style TargetType="tv:TableViewCell">
        <Setter Property="Background" Value="#FFF3CD" />
    </Style>
</tv:TableViewConditionalCellStyle>
```

## Per-column conditional styles

You can also add [`ConditionalCellStyles`](xref:WinUI.TableView.TableView.ConditionalCellStyles) on a specific column instead of the whole table. This keeps the predicate logic scoped to the column:

```xml
<tv:TableViewNumberColumn Header="Price" Binding="{Binding Price}">
    <tv:TableViewNumberColumn.ConditionalCellStyles>
        <tv:TableViewConditionalCellStylesCollection>
            <tv:TableViewConditionalCellStyle Predicate="{x:Bind IsPriceNegative}">
                <Style TargetType="tv:TableViewCell">
                    <Setter Property="Foreground" Value="Red" />
                    <Setter Property="FontWeight" Value="Bold" />
                </Style>
            </tv:TableViewConditionalCellStyle>
        </tv:TableViewConditionalCellStylesCollection>
    </tv:TableViewNumberColumn.ConditionalCellStyles>
</tv:TableViewNumberColumn>
```

```csharp
public bool IsPriceNegative(TableViewConditionalCellStyleContext ctx) =>
    ctx.DataItem is Product p && p.Price < 0;
```

## TableViewConditionalCellStyleContext

The predicate receives a [`TableViewConditionalCellStyleContext`](xref:WinUI.TableView.TableViewConditionalCellStyleContext) value with these members:

| Property | Type | Description |
|---|---|---|
| `Column` | [`TableViewColumn`](xref:WinUI.TableView.TableViewColumn) | The column of the cell being evaluated |
| `DataItem` | `object?` | The data item for the row |

## Multiple conditional styles

You can add multiple [`TableViewConditionalCellStyle`](xref:WinUI.TableView.TableViewConditionalCellStyle) entries. They are evaluated in order; if multiple predicates return `true`, the **last** matching style wins.

```xml
<tv:TableView.ConditionalCellStyles>
    <tv:TableViewConditionalCellStylesCollection>
        <!-- Applied first (lower priority) -->
        <tv:TableViewConditionalCellStyle Predicate="{x:Bind IsExpiringSoon}">
            <Style TargetType="tv:TableViewCell">
                <Setter Property="Background" Value="#FFF9C4" />
            </Style>
        </tv:TableViewConditionalCellStyle>
        <!-- Applied second (higher priority if both match) -->
        <tv:TableViewConditionalCellStyle Predicate="{x:Bind IsExpired}">
            <Style TargetType="tv:TableViewCell">
                <Setter Property="Background" Value="#FFCDD2" />
            </Style>
        </tv:TableViewConditionalCellStyle>
    </tv:TableViewConditionalCellStylesCollection>
</tv:TableView.ConditionalCellStyles>
```

## Setting ConditionalCellStyles in code

```csharp
tableView.ConditionalCellStyles = new TableViewConditionalCellStylesCollection
{
    new TableViewConditionalCellStyle
    {
        Predicate = ctx => ctx.DataItem is Product p && p.Stock < 10,
        Style = lowStockStyle
    }
};
```

## Notes and limitations

- Conditional styles are evaluated each time a cell is rendered or refreshed. Avoid expensive operations inside predicates on large datasets.
- Conditional styles apply to the [`TableViewCell`](xref:WinUI.TableView.TableViewCell) container, not the inner display element (e.g. `TextBlock`). To style the inner element, use [`ElementStyle`](xref:WinUI.TableView.TableViewBoundColumn.ElementStyle) on the column.
- Table-level and column-level [`ConditionalCellStyles`](xref:WinUI.TableView.TableView.ConditionalCellStyles) both apply. Column-level styles take precedence over table-level styles when both match.

## Related articles

- [Styling rows, cells, and headers](styling.md)
- [Column types](column-types.md)
