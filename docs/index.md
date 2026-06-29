# WinUI.TableView

**WinUI.TableView** is a lightweight and fast data grid control for [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/winui3) apps with support for the [Uno Platform](https://platform.uno/). It is derived from `ListView`, giving it a fluent Fluent Design look and feel, and is designed to handle large datasets efficiently.

Use WinUI.TableView when you need a tabular data display with sorting, filtering, editing, row details, and clipboard support in a WinUI 3 or Uno Platform application.

## Quick start

Install the NuGet package:

```bash
Install-Package WinUI.TableView
```

Add to your XAML:

```xml
<Window
    xmlns:tv="using:WinUI.TableView">
    <Grid>
        <tv:TableView ItemsSource="{x:Bind Products}" AutoGenerateColumns="False">
            <tv:TableView.Columns>
                <tv:TableViewTextColumn   Header="Name"     Binding="{Binding Name}" />
                <tv:TableViewNumberColumn Header="Price"    Binding="{Binding Price}" />
                <tv:TableViewCheckBoxColumn Header="In Stock" Binding="{Binding InStock}" />
            </tv:TableView.Columns>
        </tv:TableView>
    </Grid>
</Window>
```

Add the model and data to your code-behind or view model:

```csharp
public record Product(string Name, double Price, bool InStock);

public ObservableCollection<Product> Products { get; } = new()
{
    new Product("Widget A", 9.99,  true),
    new Product("Widget B", 24.99, false),
    new Product("Widget C", 4.49,  true),
};
```

## [Samples App](https://github.com/w-ahmad/WinUI.TableView/tree/main/samples/WinUI.TableView.SampleApp)

Explore interactive samples with code snippets in the Samples App on Microsoft Store or Uno Platform WASM:

<a href="https://apps.microsoft.com/detail/9ntgb6lmjzx3?referrer=appbadge&mode=direct">
  <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

<br>

<a href="https://tableview.samples.w-ahmad.dev/">
   <img align=center width="18%" src="https://raw.githubusercontent.com/unoplatform/styleguide/master/logo/uno-platform-logo-with-text.png" />
</a>

####

![WinUI TableView SampleApp](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView.SampleApp/main/WinUI.TableView%20SampleApp.gif)

## Key features

| Feature | Description |
|---|---|
| [Auto-generating columns](docs/defining-columns.md) | Automatically generate columns based on the data source properties |
| [Column types](docs/column-types.md) | Text, Number, CheckBox, ComboBox, ToggleSwitch, Date, Time, Hyperlink, Template |
| [Sorting](docs/sorting.md) | Built-in column sorting with sort direction indicators |
| [Filtering](docs/filtering.md) | Excel-like column filter flyout |
| [Editing](docs/editing.md) | Double-tap a cell to edit; full editing lifecycle events |
| [Selection](docs/selection.md) | Row, cell, or combined selection modes |
| [Frozen columns](docs/frozen-columns.md) | Pin columns to the left while the rest scroll |
| [Row details](docs/row-details.md) | Expandable detail panels per row |
| [Row headers](docs/row-headers.md) | Configurable row header with custom templates |
| [Clipboard](docs/clipboard.md) | Copy and paste rows/cells; customizable content |
| [CSV export](docs/export.md) | Built-in export to CSV |
| [Conditional styling](docs/conditional-styling.md) | Apply cell styles based on custom predicates |
| [Context flyouts](docs/context-flyouts.md) | Row-level and cell-level context menus |
| [Styling](docs/styling.md) | Grid lines, alternate rows, and custom cell/header styles |
| [Column reordering](docs/column-reordering.md) | Drag columns to reorder; configurable per-column |
| [Column sizing](docs/column-sizing.md) | Fixed, star, auto, and manual resize modes |
| Localization | Multiple language support |
| [Uno Platform](docs/getting-started-with-uno.md) | Cross-platform: Windows, macOS, Linux, WASM, iOS, Android |

## Documentation

### Getting started
- [Installation and quick start](docs/getting-started.md)
- [Getting started with Uno Platform](docs/getting-started-with-uno.md)
- [Overview and concepts](docs/overview.md)

### Migration
- [Migrating from WPF DataGrid](docs/migration-wpf.md)
- [Migrating from WCT DataGrid](docs/migration-wct.md)
- [Feature comparison](docs/datagrid-feature-comparison.md)

## Uno Platform support

`WinUI.TableView` is compatible with the [Uno Platform](https://platform.uno/), enabling you to target Windows, macOS, Linux, WebAssembly, iOS, and Android from a single codebase.
