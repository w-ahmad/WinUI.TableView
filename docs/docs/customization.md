## Customization

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