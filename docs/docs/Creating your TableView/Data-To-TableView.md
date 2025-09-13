# Adding data to your TableView
This doc will guide you through adding dynamic data to your TableView as fields, rows, or columns.

## Understanding TableView columns
There are different types of TableView columns like `TableViewTextColumn`, `TableViewCheckBoxColumn`, `TableViewComboBoxColumn` and more.

When dynamically adding columns, some columns are assigned their type automatically. For example, if you have an `int` value for a column the column will automatically become a `TableViewNumberColumn`. The same goes for a TextColumn with `string` and CheckBoxColumn with `bool`

### About `ObservableCollection`
The data that goes in TableView columns can be stored in an `ObservableCollection`, not a normal class. If you're new to adding items in an `ObservableCollection`, here is what you need to do.

Let's say we have this class, which is what you would normally expect:
```csharp
public class Person
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public int Age { get; set; }
}
```

Here is how it would look as an `ObservableCollection`:
```csharp
public class Person : INotifyPropertyChanged
{
    private string _name;
    private bool _isActive;
    private int _age;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
    }

    public int Age
    {
        get => _age
        set { _age = value; OnPropertyChanged(nameof(Age)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

#### Here is a copy-ready class to edit for your own data:
```csharp
public class <The name of your class> : INotifyPropertyChanged
{
    private <your variable's type> _<your variable's name>;

    public <your variable's type> <your variable's name>
    {
        get => _<your variable's name>;
        set { _<your variable's name> = value; OnPropertyChanged(nameof(<your variable's name>))}
    }

    // ^ Repeat the above for every variable you have ^

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```
Leave everything outside of `<>` unchanged.


## Creating TableView columns
### 1. Auto-Generated columns
If you bind a list of objects (like an `ObservableCollection<MyItem>`) to a TableView, it will automatically create a column for each public property in your item `ObservableCollection`. The type of column depends on the property type:
- `string` → Text column
- `bool` → Checkbox column
- `int`, `double`, etc. → Number column
- `DateOnly` → Date column
- `TimeOnly` → Time column

**You do not need to do anything special for these types.**

To add the items, in your page or window's class, first assign your `ObservableCollection` to a variable (Let's use the example from earlier for this):
```csharp
public ObservableCollection<Person> People { get; } = new();
```
and then add that to the TableView:
```csharp
People.Add(new MyItem() { Name = "George", Age = "30", IsActive = true });
```

Make sure you have binded the TableView to the `ObservableCollection` you just created:
```xml
<tv:TableView ItemsSource="{x:Bind People}">
```

If you bind a list of `Person` (from our previous example) to the TableView, it will show three columns: Name (text), IsActive (checkbox), and Age (number). These columns are automatically changed to a text, checkbox, and number column depending on the type of the variables for each one, so you don't need to do anything else.

-----

### 2. Customizing or Replacing Columns (ComboBox, ToggleSwitch, Template, etc.)

Some columns, like ComboBox or custom templates, require extra steps. TableView does **not** automatically create a ComboBox just because you have a list or set of values. You must tell TableView to use a ComboBox column for a property.

This is done by handling the `AutoGeneratingColumn` event. In this event, you can:
* Change the type of column (e.g., from text to ComboBox)

* Set extra properties (like ItemsSource for ComboBox)

#### Example: Customizing a Column to be a ComboBox
Suppose you have a property called `Gender` and you want it to be a ComboBox with a list of genders.

##### 1. Define your model:
For this example, we're going to use the People class from before. Let's add this new variable to it:
```csharp
...
private string _gender;

public string Gender
{
    get => _gender;
    set { _gender = value; OnPropertyChanged(nameof(Gender))}
}
...
```

##### 2. Prepare the list of genders:
You need a collection (like a `List<string>`, `ObservableCollection<string>`, or `SortedSet<string>`) with all possible values. For example:
```csharp
public List<string> Genders { get; } = new() { "Male", "Female" };
```

##### 3. Handle the AutoGeneratingColumn event:
This event is called for each property when TableView creates columns. You can replace the default column with a ComboBox column.

```csharp
// This method should be assigned to the TableView's AutoGeneratingColumn event
void OnAutoGeneratingColumns(object sender, TableViewAutoGeneratingColumnEventArgs e)
{
    // The column TableView was going to use
    var originalColumn = (TableViewBoundColumn)e.Column;

    // Check if this is the property you want to customize
    if (e.PropertyName == nameof(Person.Gender))
    {
        // Replace with a ComboBox column
        e.Column = new TableViewComboBoxColumn
        {
            // Use the same binding as the original column (Keep the original values as they are. You can of course change this)
            Binding = originalColumn.Binding,
            Header = originalColumn.Header,
            ItemsSource = Genders, // The list of possible values
            // ^ This will add all the strings in Genders to the Items of the ComboBox (the options) ^
            Width = new GridLength(120)
        };
    }
}
```
**Explanation:**
- `originalColumn.Binding` ensures the ComboBox is bound to the `Gender` property of your item.
- `ItemsSource = Genders` tells the ComboBox what values to show.
- You can set `Header`, `Width`, and other properties as needed, although this is optional.

**You must add this event handler to your TableView for this to work:**
```xml
<TableView AutoGeneratingColumn="OnAutoGeneratingColumns" ... />
```

## FAQs

### Q: How do I add items to Genders (or any collection)?
A: Just add them like any normal list:
```csharp
Genders.Add("NewGender");
```
Or, if you want to collect unique values from your data:
```csharp
foreach (var person in people)
{
    if (!Genders.Contains(person.Gender))
        Genders.Add(person.Gender);
}
```

### Q: Does TableView automatically use a ComboBox if I use a collection?
A: **No.** TableView only uses ComboBox columns if you explicitly replace the column in the `AutoGeneratingColumn` event and set the `ItemsSource`.

### Q: What is a Binding and why do I use it?
A: The `Binding` property tells the column which property of your item it should display and edit. When you replace a column, always copy the original column's binding:
```csharp
Binding = originalColumn.Binding
```
This ensures the ComboBox (or other custom column) is still connected to your data.

### Q: Can I add columns that are not in my model?
A: Yes! You can add columns directly in XAML or code, such as a button column or a template column. See below.

---

## Adding a Custom Column (Not in Your Model)

### In XAML:
```xml
<TableView.Columns>
    <tableView:TableViewTemplateColumn Header="Actions" Width="100">
        <tableView:TableViewTemplateColumn.CellTemplate>
            <DataTemplate>
                <Button Content="Click" Command="{Binding DataContext.MyCommand, RelativeSource={RelativeSource AncestorType=TableView}}" />
            </DataTemplate>
        </tableView:TableViewTemplateColumn.CellTemplate>
    </tableView:TableViewTemplateColumn>
</TableView.Columns>
```

### In Code:
```csharp
var templateColumn = new TableViewTemplateColumn
{
    Header = "Actions",
    Width = new GridLength(100),
    CellTemplate = (DataTemplate)Resources["MyButtonTemplate"]
};
myTableView.Columns.Add(templateColumn);
```

-----

## Summary Table: When to Customize Columns
| Property Type         | Auto-Generated? | Needs Manual Customization? | How to Customize                |
|----------------------|-----------------|----------------------------|---------------------------------|
| string, int, bool    | Yes             | No                         | N/A                             |
| ComboBox (choices)   | No              | Yes                        | Replace in AutoGeneratingColumn |
| ToggleSwitch         | No              | Yes                        | Replace in AutoGeneratingColumn |
| Template (custom UI) | No              | Yes                        | Add to Columns collection       |

---

## Tips
- Always copy the original column's `Binding` and `Header` when replacing columns.
- For custom columns not tied to a model property, add them directly to the TableView.Columns collection.
- You can set properties like `IsReadOnly`, `Width`, `CellStyle`, etc., on any column type.

> [!NOTE]
> Check out the [Samples App](https://github.com/w-ahmad/WinUI.TableView.SampleApp) to try out customization options and more.

---

By following these steps, you can add or customize any supported column type in TableView, whether you want to use simple auto-generated columns or advanced custom columns. You do not need to use the sample's model or code. Just follow the patterns above with your own data classes and collections.
