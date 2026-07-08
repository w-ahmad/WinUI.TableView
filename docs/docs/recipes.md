# Common recipes

This page collects practical, copy-and-paste-ready solutions to common `TableView` scenarios.

---

## Create a read-only table

Set `IsReadOnly="True"` on the `TableView` to prevent any cell from entering edit mode.

```xml
<tv:TableView ItemsSource="{x:Bind Products}" IsReadOnly="True" />
```

To make only specific columns read-only while leaving others editable, set `IsReadOnly` per column:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.Columns>
        <tv:TableViewTextColumn Header="ID"    Binding="{Binding Id}"    IsReadOnly="True" />
        <tv:TableViewTextColumn Header="Name"  Binding="{Binding Name}" />
        <tv:TableViewNumberColumn Header="Price" Binding="{Binding Price}" />
    </tv:TableView.Columns>
</tv:TableView>
```

---

## Enable Excel-like copy and paste

Copy is enabled by default. Pressing **Ctrl+C** copies selected cells as tab-separated text. To also allow pasting:

```xml
<tv:TableView ItemsSource="{x:Bind Products}"
              ClipboardCopyMode="IncludeHeader"
              CanPasteToNewRows="True" />
```

See [Clipboard and copy/paste](clipboard.md) for a full reference.

---

## Use a template column with sorting, filtering, copy, and export

`TableViewTemplateColumn` has `CanSort` and `CanFilter` set to `false` by default because there is no bound property path. Use `OperationContentBinding` to point to the underlying property so sort, filter, clipboard, and export operations all work:

```xml
<tv:TableViewTemplateColumn Header="Rating"
                             CanSort="True"
                             CanFilter="True">
    <tv:TableViewTemplateColumn.OperationContentBinding>
        <Binding Path="Rating" />
    </tv:TableViewTemplateColumn.OperationContentBinding>
    <tv:TableViewTemplateColumn.CellTemplate>
        <DataTemplate>
            <RatingControl Value="{Binding Rating}" IsReadOnly="True" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.CellTemplate>
    <tv:TableViewTemplateColumn.EditingTemplate>
        <DataTemplate>
            <RatingControl Value="{Binding Rating, Mode=TwoWay}" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.EditingTemplate>
</tv:TableViewTemplateColumn>
```

See [Column types](column-types.md) for more details on `OperationContentBinding`.

---

## Add row details

Show additional information below each row using `RowDetailsTemplate`. Set `RowDetailsVisibilityMode` to control when details are visible.

```xml
<tv:TableView ItemsSource="{x:Bind Orders}"
              RowDetailsVisibilityMode="VisibleWhenSelected">
    <tv:TableView.RowDetailsTemplate>
        <DataTemplate>
            <StackPanel Padding="16" Spacing="4">
                <TextBlock Text="{Binding Notes}" TextWrapping="Wrap" />
            </StackPanel>
        </DataTemplate>
    </tv:TableView.RowDetailsTemplate>
</tv:TableView>
```

Options for `RowDetailsVisibilityMode`:

| Value | Behavior |
|---|---|
| `Collapsed` | Row details never shown |
| `Visible` | Row details always shown |
| `VisibleWhenSelected` | Row details shown for the selected row |
| `VisibleWhenExpanded` | Row details shown when the row is explicitly expanded |

See [Row details](row-details.md) for a full reference.

---

## Highlight cells based on value

Use conditional cell styling to apply a background or foreground brush when a property meets a condition:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.ConditionalStyles>
        <tv:TableViewConditionalStyle>
            <tv:TableViewConditionalStyle.Predicate>
                <!-- Highlight cells in the Price column when price > 50 -->
            </tv:TableViewConditionalStyle.Predicate>
        </tv:TableViewConditionalStyle>
    </tv:TableView.ConditionalStyles>
</tv:TableView>
```

Or apply it in code:

```csharp
tableView.CellStyleRules.Add(new TableViewCellStyleRule
{
    Predicate = ctx => ctx.DataItem is Product p && p.Price > 50,
    Style = (Style)Resources["HighlightCellStyle"]
});
```

See [Conditional cell styling](conditional-styling.md) for a full reference and working examples.

---

## Use custom sorting

Handle the `Sorting` event and set `e.Handled = true` to replace the default sort behavior:

```csharp
tableView.Sorting += (s, e) =>
{
    if (e.Column.Header?.ToString() == "Name")
    {
        var direction = e.Column.SortDirection == SortDirection.Ascending
            ? SortDirection.Descending
            : SortDirection.Ascending;

        tableView.ClearAllSorting();
        tableView.SortDescriptions.Add(new SortDescription(
            "Name",
            direction,
            StringComparer.OrdinalIgnoreCase));

        e.Column.SortDirection = direction;
        e.Handled = true;
    }
};
```

See [Sorting](sorting.md) for more patterns, including `SortMemberPath`.

---

## Use custom filtering

Replace the built-in column filter logic with your own `IColumnFilterHandler`:

```csharp
public class MyFilterHandler : IColumnFilterHandler
{
    // Implement IColumnFilterHandler members
}

tableView.FilterHandler = new MyFilterHandler();
```

For programmatic filtering without a custom handler:

```csharp
// Show only rows where InStock is true
tableView.FilterDescriptions.Add(
    new FilterDescription("InStock", new PredicateFilter(v => v is true)));

// Clear all filters
tableView.ClearAllFilters();
```

See [Filtering](filtering.md) for a full reference.

---

## Programmatically select a cell

```csharp
// Select a specific cell (row 2, column 1 — zero-based)
var slot = new TableViewCellSlot(2, 1);
tableView.SelectedCells.Add(slot);
```

To move focus to a cell, scroll it into view first:

```csharp
tableView.ScrollIntoView(tableView.Items[2], tableView.Columns[1]);
```

See [Selection](selection.md) for a full reference.

---

## Export selected rows to CSV

`TableView` supports exporting the current view to CSV. By default, all rows and visible columns are included. To export only selected rows, use the overload that accepts a `TableViewExportOptions`:

```csharp
await tableView.ExportToCSVAsync();
```

See [Export to CSV](export.md) for configuration options.

---

## Use TableView with MVVM (CommunityToolkit.Mvvm)

A clean MVVM setup using `CommunityToolkit.Mvvm`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

public partial class ProductsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Product> _products = [];
}
```

In your view:

```xml
<tv:TableView ItemsSource="{x:Bind ViewModel.Products}" />
```

Wire up the ViewModel in your page:

```csharp
public sealed partial class ProductsPage : Page
{
    public ProductsViewModel ViewModel { get; } = new ProductsViewModel();

    public ProductsPage()
    {
        this.InitializeComponent();
    }
}
```

If you publish with Native AOT, also add `[WinRT.GeneratedBindableCustomProperty]` to your model class. See [Native AOT compatibility](aot-compatibility.md) for details.

---

## Related articles

- [Binding data](binding-data.md)
- [Column types](column-types.md)
- [Sorting](sorting.md)
- [Filtering](filtering.md)
- [Selection](selection.md)
- [Editing](editing.md)
- [Clipboard and copy/paste](clipboard.md)
- [Export to CSV](export.md)
- [Row details](row-details.md)
- [Conditional cell styling](conditional-styling.md)
- [Native AOT compatibility](aot-compatibility.md)
