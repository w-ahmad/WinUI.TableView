# Row headers

Row headers appear as a narrow strip on the left side of each row. They can show row numbers, selection indicators, expand/collapse toggles for row details, or any custom content.

## When to use it

Use row headers when you need a persistent left-side control for each row that is separate from the data columns — such as a row number, a selection checkbox, or a status icon.

## Showing and hiding headers

Use [`HeadersVisibility`](xref:WinUI.TableView.TableView.HeadersVisibility) to control which headers are visible:

| Value | Description |
|---|---|
| [`All`](xref:WinUI.TableView.TableViewHeadersVisibility.All) (default) | Both row headers and column headers are visible |
| [`Columns`](xref:WinUI.TableView.TableViewHeadersVisibility.Columns) | Only column headers are visible |
| [`Rows`](xref:WinUI.TableView.TableViewHeadersVisibility.Rows) | Only row headers are visible |
| [`None`](xref:WinUI.TableView.TableViewHeadersVisibility.None) | No headers are visible |

```xml
<!-- Show only column headers, no row headers -->
<tv:TableView HeadersVisibility="Columns" />
```

## Custom row header content

Provide a `DataTemplate` via [`RowHeaderTemplate`](xref:WinUI.TableView.TableView.RowHeaderTemplate) to display custom content in each row header:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.RowHeaderTemplate>
        <DataTemplate>
            <Border MinWidth="40">
                <FontIcon Glyph="&#xE72A;" />
            </Border>
        </DataTemplate>
    </tv:TableView.RowHeaderTemplate>
</tv:TableView>
```

![Custom row header with icon](../images/row-headers-custom.png)

> **Note:** The `Tag` of the `TableViewRowHeader` is always set to the row's number (see [Row numbering](#row-numbering) below), whether or not a custom `RowHeaderTemplate` is set. Bind to it with `{Binding Tag, RelativeSource={RelativeSource Mode=TemplatedParent}}` to incorporate the number into a custom template.

### Selecting a template per row

Use [`RowHeaderTemplateSelector`](xref:WinUI.TableView.TableView.RowHeaderTemplateSelector) to choose different templates for different rows:

```csharp
public class StatusHeaderSelector : DataTemplateSelector
{
    public DataTemplate? ActiveTemplate { get; set; }
    public DataTemplate? InactiveTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is Product p)
            return p.IsActive ? ActiveTemplate : InactiveTemplate;
        return base.SelectTemplateCore(item);
    }
}
```

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.RowHeaderTemplateSelector>
        <local:StatusHeaderSelector
            ActiveTemplate="{StaticResource ActiveRowHeaderTemplate}"
            InactiveTemplate="{StaticResource InactiveRowHeaderTemplate}" />
    </tv:TableView.RowHeaderTemplateSelector>
</tv:TableView>
```

## Row numbering

Set [`ShowRowNumbers`](xref:WinUI.TableView.TableView.ShowRowNumbers) to `true` to display each row's number at the very start of the row, in its own column ahead of the row header content and the row details expander toggle:

```xml
<tv:TableView ItemsSource="{x:Bind Products}" ShowRowNumbers="True" />
```

The row number is a real, stable position among all rows - it does not renumber when a [group](grouping.md) is collapsed and hides other rows, unlike a naively counted display position. It coexists with `RowHeaderTemplate`, `RowHeaderTemplateSelector`, and the row details expander toggle without taking space from any of them.

Customize its appearance with these theme resources:

| Resource | Type | Description |
|---|---|---|
| `TableViewRowNumberFontSize` | `double` | Font size of the row number text |
| `TableViewRowNumberForeground` | `Brush` | Foreground brush of the row number text |
| `TableViewRowNumberMargin` | `Thickness` | Margin around the row number text |
| `TableViewRowNumberMinWidth` | `double` | Minimum width of the row number column |

## Sizing the row header

| Property | Default | Description |
|---|---|---|
| [`RowHeaderWidth`](xref:WinUI.TableView.TableView.RowHeaderWidth) | `NaN` (auto) | Fixed width for the row header column |
| [`RowHeaderMinWidth`](xref:WinUI.TableView.TableView.RowHeaderMinWidth) | `16` | Minimum row header width |
| [`RowHeaderMaxWidth`](xref:WinUI.TableView.TableView.RowHeaderMaxWidth) | `∞` | Maximum row header width |
| [`RowHeaderActualWidth`](xref:WinUI.TableView.TableView.RowHeaderActualWidth) | read-only | The rendered width of the row header column |

```xml
<tv:TableView RowHeaderWidth="40" />
```

## Row details expand/collapse

When [`RowDetailsVisibilityMode`](xref:WinUI.TableView.TableView.RowDetailsVisibilityMode) is set to `VisibleWhenExpanded`, the row header shows an expand/collapse toggle button automatically. See [Row details](row-details.md) for the full row details feature.

## Notes and limitations

- Row headers are always frozen — they do not scroll horizontally.
- The row header width is shared across all rows; you cannot set a different width per row.

## Related articles

- [Row details](row-details.md)
- [Selection](selection.md)
- [Styling rows, cells, and headers](styling.md)
