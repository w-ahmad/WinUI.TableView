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

## [Samples App](https://github.com/w-ahmad/WinUI.TableView.SampleApp)

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
| Auto-generating columns | Automatically generate columns based on the data source properties |
| Column types | Text, Number, CheckBox, ComboBox, ToggleSwitch, Date, Time, Hyperlink, Template |
| Sorting | Built-in column sorting with sort direction indicators |
| Filtering | Excel-like column filter flyout |
| Editing | Double-tap a cell to edit; full editing lifecycle events |
| Selection | Row, cell, or combined selection modes |
| Frozen columns | Pin columns to the left while the rest scroll |
| Row details | Expandable detail panels per row |
| Row headers | Configurable row header with custom templates |
| Clipboard | Copy and paste rows/cells; customizable content |
| CSV export | Built-in export to CSV |
| Conditional styling | Apply cell styles based on custom predicates |
| Context flyouts | Row-level and cell-level context menus |
| Grid lines | Configurable horizontal and vertical grid lines |
| Alternate rows | Alternating row background and foreground colors |
| Localization | Multiple language support |
| Uno Platform | Cross-platform: Windows, macOS, Linux, WASM, iOS, Android |

## Documentation

### Getting started
- [Installation and quick start](docs/getting-started.md)
- [Getting started with Uno Platform](docs/getting-started-with-uno.md)
- [Overview and concepts](docs/overview.md)

### Features
- [Binding data](docs/binding-data.md)
- [Defining columns](docs/defining-columns.md)
- [Column types](docs/column-types.md)
- [Column sizing](docs/column-sizing.md)
- [Sorting](docs/sorting.md)
- [Filtering](docs/filtering.md)
- [Editing](docs/editing.md)
- [Selection](docs/selection.md)
- [Row headers](docs/row-headers.md)
- [Row details](docs/row-details.md)
- [Frozen columns](docs/frozen-columns.md)
- [Column reordering](docs/column-reordering.md)
- [Clipboard and copy/paste](docs/clipboard.md)
- [Export to CSV](docs/export.md)
- [Styling rows, cells, and headers](docs/styling.md)
- [Conditional cell styling](docs/conditional-styling.md)
- [Context flyouts](docs/context-flyouts.md)
- [Events and commands reference](docs/commands-events.md)
- [Performance guidance](docs/performance.md)

### Migration
- [Migrating from WPF DataGrid](docs/migration-wpf.md)
- [Migrating from WCT DataGrid](docs/migration-wct.md)
- [Feature comparison](docs/datagrid-feature-comparison.md)

## Uno Platform support

`WinUI.TableView` is compatible with the [Uno Platform](https://platform.uno/), enabling you to target Windows, macOS, Linux, WebAssembly, iOS, and Android from a single codebase.
