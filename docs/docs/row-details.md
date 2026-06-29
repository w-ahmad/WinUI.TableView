# Row details

Row details provide an expandable panel below each data row that can display additional information about that row's item. The panel is defined with a `DataTemplate` and can be shown automatically or on demand.

## When to use it

Use row details when each row's data has more information than fits in the columns — for example, an order list where clicking a row reveals the order line items, or a customer list where the detail panel shows contact info and notes.

## Basic example

Provide a `RowDetailsTemplate` and choose a visibility mode:

```xml
<tv:TableView ItemsSource="{x:Bind Orders}"
              RowDetailsVisibilityMode="VisibleWhenSelected">
    <tv:TableView.RowDetailsTemplate>
        <DataTemplate>
            <StackPanel Padding="16" Spacing="4">
                <TextBlock Text="{Binding Notes}" TextWrapping="Wrap" />
                <TextBlock>
                    <Run Text="Shipped to: " FontWeight="SemiBold" />
                    <Run Text="{Binding ShippingAddress}" />
                </TextBlock>
            </StackPanel>
        </DataTemplate>
    </tv:TableView.RowDetailsTemplate>
    <tv:TableView.Columns>
        <tv:TableViewTextColumn   Header="Order #"   Binding="{Binding OrderNumber}" />
        <tv:TableViewDateColumn   Header="Date"      Binding="{Binding OrderDate}" />
        <tv:TableViewNumberColumn Header="Total"     Binding="{Binding Total}" />
    </tv:TableView.Columns>
</tv:TableView>
```

## RowDetailsVisibilityMode

| Value | Description |
|---|---|
| `Collapsed` | Details pane is never shown |
| `Visible` | Details pane is always shown for every row |
| `VisibleWhenSelected` | Details pane is shown for the currently selected row(s) |
| `VisibleWhenExpanded` (default) | Details pane is shown when the user expands a row using the toggle button in the row header |

```xml
<!-- Always visible -->
<tv:TableView RowDetailsVisibilityMode="Visible" />

<!-- Show for selected row -->
<tv:TableView RowDetailsVisibilityMode="VisibleWhenSelected" />
```

## Using RowDetailsTemplateSelector

Choose a different template for different row types:

```csharp
public class OrderDetailsSelector : DataTemplateSelector
{
    public DataTemplate? StandardTemplate { get; set; }
    public DataTemplate? PriorityTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is Order { IsPriority: true } ? PriorityTemplate : StandardTemplate;
}
```

```xml
<tv:TableView ItemsSource="{x:Bind Orders}"
              RowDetailsVisibilityMode="VisibleWhenSelected">
    <tv:TableView.RowDetailsTemplateSelector>
        <local:OrderDetailsSelector
            StandardTemplate="{StaticResource StandardOrderDetailsTemplate}"
            PriorityTemplate="{StaticResource PriorityOrderDetailsTemplate}" />
    </tv:TableView.RowDetailsTemplateSelector>
</tv:TableView>
```

## Frozen row details

By default the row details panel scrolls horizontally with the row content. Set `AreRowDetailsFrozen` to `true` to keep the details panel anchored at the left edge so it does not scroll:

```xml
<tv:TableView AreRowDetailsFrozen="True"
              RowDetailsVisibilityMode="VisibleWhenSelected">
    <tv:TableView.RowDetailsTemplate>
        <DataTemplate>
            <Border Padding="16">
                <TextBlock Text="{Binding Description}" TextWrapping="Wrap" />
            </Border>
        </DataTemplate>
    </tv:TableView.RowDetailsTemplate>
</tv:TableView>
```

## Common options

| Property | Type | Default | Description |
|---|---|---|---|
| `RowDetailsTemplate` | `DataTemplate` | `null` | Template for the details panel |
| `RowDetailsTemplateSelector` | `DataTemplateSelector` | `null` | Selector for per-row templates |
| `RowDetailsVisibilityMode` | `TableViewRowDetailsVisibilityMode` | `VisibleWhenExpanded` | When the details pane is shown |
| `AreRowDetailsFrozen` | `bool` | `false` | Prevents the details panel from scrolling horizontally |

## Notes and limitations

- When `RowDetailsVisibilityMode` is `VisibleWhenExpanded`, the row header shows an expand/collapse toggle button. Make sure `HeadersVisibility` includes `Rows` (or `All`) so the toggle is visible.
- Row details panels increase the height of each row. In `Visible` mode, every row will be taller even if the details content is empty.
- The `DataContext` of the details template is the row's data item, the same as for cells.

## Related articles

- [Row headers](row-headers.md)
- [Styling rows, cells, and headers](styling.md)
- [Selection](selection.md)
