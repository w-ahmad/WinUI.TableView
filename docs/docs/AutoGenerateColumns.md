# About `AutoGenerateColumns`
`AutoGenerateColumns` is used when you want each item of the TableView to be automatically generated. When it is set to `true`, TableView will look at your values and assign them a column depending on their type. Some examples include:
- **`TableViewTextColumn`** for `string`
- **`TableViewNumberColumn`** for `int`, `float`, or `double`
- **`TableViewDateColumn`** and `TableViewTimeColumn` for `DateOnly` and `TimeOnly` repsectively
- **`TableViewCheckBoxColumn`** for `bool`

Other columns like `TableViewComboBoxColumn` or `TableViewTemplateColumn` need to be handled in code-behind and are not automatically generated.

---

#### `AutoGenerateColumns` also has a related function.
**`OnAutoGeneratingColumns`** is called when the TableView is generating the columns. Inside it you can write code that executes when the TableView is loading the columns. This is great for things like adding `ComboBoxColumns` by checking if the current column that's being loaded should be a `ComboBoxColumn` like so:

```csharp
// Assign this method to the TableView.AutoGeneratingColumn event
void OnAutoGeneratingColumns(object sender, TableViewAutoGeneratingColumnEventArgs e)
{
    if (sender is not TableView tableView) return;

    // Try to get your view model from the TableView's DataContext
    var viewModel = tableView.DataContext as MainViewModel; // replace MainViewModel with your VM type

    // The column that TableView would have used for this property
    if (e.Column is not TableViewBoundColumn originalColumn) return;

    // Gander is a ComboBoxColumn
    if (e.PropertyName == nameof(Person.Gender) && viewModel != null)
    {
        e.Column = new TableViewComboBoxColumn
        {
            // Keep the same binding so the column still reads/writes the Gender property
            Binding = originalColumn.Binding,
            Header = originalColumn.Header,

            // Set the source for the options to the Genders collection (so the options for this example will be "male", "female", etc.)
            ItemsSource = viewModel.Genders
        };
    }
}
```