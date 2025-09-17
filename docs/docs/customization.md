# Customization

You can customize the appearance and behavior of the `TableView` by modifying its properties, templates, and styles. For example:

- **Column Customization**: Define custom columns based on data types.
- **Is ReadOnly**: You can make any column or the TableView itself read only.
- **Sorting and Filtering**: Enable sorting and filtering on specific columns or for the all columns.
- **Corner Button Mode**: Use the `CornerButtonMode` property to configure the corner button's behavior. You can select from:
  - `None`: No corner button.
  - `SelectAll`: Displays a "Select All" button.
  - `Options`: Displays an options menu.
- **Column Header and Cell Styles**: Customize the styles for column headers and cells to match your application's theme or specific design requirements.

```xml
<tv:TableView x:Name="MyTableView"
              ItemsSource="{x:Bind ViewModel.Items}"
              AutoGenerateColumns="False"
              xmlns:tv="using:WinUI.TableView">
    <tv:TableView.Columns>
        <tv:TableViewTextColumn Header="Name" Binding="{Binding Name}" />
        <tv:TableViewNumberColumn Header="Price" Binding="{Binding Price}" />
        <tv:TableViewTemplateColumn Header="Quantity">
            <tv:TableViewTemplateColumn.CellTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Quantity}" />
                </DataTemplate>
            </tv:TableViewTemplateColumn.CellTemplate>
            <tv:TableViewTemplateColumn.EditingTemplate>
                <DataTemplate>
                    <NumberBox Value="{Binding Quantity, Mode=TwoWay}" />
                </DataTemplate>
            </tv:TableViewTemplateColumn.EditingTemplate>
        </tv:TableViewTemplateColumn>
    </tv:TableView.Columns>
</tv:TableView>
```

#### 👉 Use the [Sample App](https://github.com/w-ahmad/WinUI.TableView.SampleApp.git) to easily try out and change customization options.

## Editing properties in C#
To customize your TableView using C#, first give it a name:
```xml
<tv:TableView x:Name="TViev">
```
then you can edit its properties in code like this:
```csharp
TView.Opacity = 1;
```

### Useful Properties
- `TableView.Columns` allows you to edit columns by using `.Add()` to add a column, `.Remove()` to remove one, `.Clear()` to clear all and more
- `TableView.ScrollIntoView(`any column item`)` Scrolls to the chosen item
- `TableView.SelectAll()` Selects all the items
