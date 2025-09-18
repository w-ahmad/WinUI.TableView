# Adding data to your TableView
This doc will guide you through adding dynamic data to your TableView as fields, rows, or columns.

## Understanding TableView columns
There are different types of TableView columns like `TableViewTextColumn`, `TableViewCheckBoxColumn`, `TableViewComboBoxColumn` and more.

When dynamically adding columns, some columns are assigned their type automatically. For example, if you have an `int` value for a column the column will automatically become a `TableViewNumberColumn`. The same goes for a TextColumn with `string` and CheckBoxColumn with `bool`. The value of the columns are stored in an **ObservableCollection**

### About `ObservableCollection`
The data shown by a TableView is typically bound to an `ObservableCollection<T>` where `T` is your item type (for example, `Person`). If you want edits to update the UI immediately, the item type should implement `INotifyPropertyChanged`.

Let's say we have this class, which is what you would normally expect:
```csharp
public class Person
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public int Age { get; set; }
}
```

Here is how the item class would look if you implement `INotifyPropertyChanged` so edits are reflected in the UI:
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
        get => _age;
        set { _age = value; OnPropertyChanged(nameof(Age)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

#### Here is a copy-ready class template to edit for your own data:
```csharp
public class MyItem : INotifyPropertyChanged
{
    private <your variable's type> _<yourVariableName>;

    public <your variable's type> <YourPropertyName>
    {
        get => _<yourVariableName>;
        set { _<yourVariableName> = value; OnPropertyChanged(nameof(<YourPropertyName>)); }
    }

    // Repeat the above pattern for each property you need.

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```
###### Leave everything outside of `<>` unchanged.


## Creating TableView columns
There are two ways to add columns to your TableView:
1. Auto-Generated columns (for text, numbers, date and time, and boolean values)
2. Custom columns (for more control over the column type and values (ComboBox, etc.))

### 1. Auto-Generated columns
If you bind a list of objects (like an `ObservableCollection<MyItem>`) to a TableView, it will automatically create a column for each public property on the item type. The type of column depends on the property type:
- `string` → Text column
- `bool` → Checkbox column
- `int`, `double`, etc. → Number column
- `DateOnly` → Date column
- `TimeOnly` → Time column

**You do not need to do anything special for these types.**

To add the items, in your page or viewmodel class, first declare your `ObservableCollection`. This has all the variables from the `People` collection from before:
```csharp
public ObservableCollection<Person> People { get; } = new();
```

and then add items to it:
```csharp
People.Add(new Person { Name = "George", Age = 30, IsActive = true });
```

Finally, make your TableView is bound to your collection:
```xml
<tv:TableView ItemsSource="{x:Bind People}" />
```

If you bind a list of `Person` to the TableView, it will show three columns: Name (text), IsActive (checkbox), and Age (number). These columns are created automatically based on the property types.

<details>

<summary>Full code example</summary>

In your XAML:
```xml
<Grid>
    <!-- This just creates a new TableView and binds its items to People, so it is updated dynamically. -->
    <tv:TableView Padding="20" ItemsSource="{x:Bind People}" />
</Grid>
```

In C# (Using MainWindow as an example here):
```cs
// This is the Person class, where all the data of a single person is stored. Think of this as its own row, and the variables are its own items or columns.
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
        get => _age;
        set { _age = value; OnPropertyChanged(nameof(Age)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed partial class MainWindow : Window // This could also be a page. Doesn't really matter.
{
    public ObservableCollection<Person> People { get; } = new(); // Declare a new ObserableCollection of the type Person so you can use it in code to add columns.

    public MainWindow()
    {
        InitializeComponent();

        // You can put this code anywhere, for example in a function. It's just here for the sake of simplicity.
        // This creates a new row for a Person with the new columns of Name, Age, and IsActive.
        People.Add(new Person { Name = "George", Age = 30, IsActive = true });
    }
}
```
</details>

-----

### 2. Customizing or Replacing Columns (ComboBox, ToggleSwitch, Template, etc.)

Some columns, like ComboBox or custom templates, require extra steps. TableView does **not** automatically create a ComboBox just because you have a list or set of values. You must tell TableView to use a ComboBox column for a property.

This is done by handling the `AutoGeneratingColumn` event. In this event you can:
- Change the type of column (e.g., from text to ComboBox)
- Set extra properties (like ItemsSource for ComboBox)

### Example: Customizing a Column to be a ComboBox
Suppose you have a property called `Gender` and you want it to be a ComboBox with a list of genders.

##### 1. Define your model:
Add the `Gender` property to your item type (or create your own item type similar to `Person` above), just like before:
```csharp
private string _gender;
public string Gender
{
    get => _gender;
    set { _gender = value; OnPropertyChanged(nameof(Gender)); }
}
```

##### 2. Create a ViewModel
A ViewModel is a class that you can use to manage your data and dynamically add items to your TableView, allowing for more advanced options:
```cs
public class MainViewModel
{
    public ObservableCollection<string> Genders { get; } = new ObservableCollection<string> { "Male", "Female" };

    public MainViewModel()
    {
        Tasks = new ObservableCollection<Person>
        {
            new Person { Name = "George", Age = 30, IsActive = true, Gender = "Male" }
        };
    }
}
```

> [!TIP]
> You can also make changes or add items to your collection later like this:
> ```cs
> var viewModel = MyTableView.DataContext as MainViewModel;
> viewModel.Genders.Add("NewGender");
> ```

##### 3. Declare your ViewModel so you can use it in your page:
```cs
 public MainViewModel ViewModel { get; } = new MainViewModel();
```

##### 4. Handle the AutoGeneratingColumn event:
This event is called for each property when TableView creates columns. You can replace the default column with a ComboBox column. Use safe casts and obtain your viewmodel from the TableView's DataContext so the handler works in other projects.

```csharp
// Assign this method to the TableView.AutoGeneratingColumn event
void OnAutoGeneratingColumns(object sender, TableViewAutoGeneratingColumnEventArgs e)
{
    if (sender is not TableView tableView) return;

    // Try to get your view model from the TableView's DataContext
    var viewModel = tableView.DataContext as MainViewModel; // replace MainViewModel with your VM type

    // The column that TableView would have used for this property
    if (e.Column is not TableViewBoundColumn originalColumn) return;

    // Replace Gender column with your ComboBox column
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

##### 5. Bind everything in your TableView:
```xml
<tv:TableView ItemsSource="{x:Bind ViewModel.People}" AutoGeneratingColumn="OnAutoGeneratingColumns" DataContext="{x:Bind ViewModel}" ... />
```

**Explanation:**
- `originalColumn.Binding` keeps the new column bound to the same property on each row item (e.g., `Person.Gender`).
- `ItemsSource` is the collection of allowed values shown in the ComboBox. It is independent from the cell Binding.
- Use the TableView's DataContext to access your ViewModel so the handler adapts to other projects.

<details>
<summary>Full code example</summary>

In your XAML:
```xml
<!-- This just creates a new TableView and binds its items to People, so it is updated dynamically. -->
<tv:TableView Padding="20" ItemsSource="{x:Bind ViewModel.People}" AutoGeneratingColumn="OnAutoGeneratingColumns" DataContext="{x:Bind ViewModel}" />
```

In C#:
```cs
public class Person : INotifyPropertyChanged
{
    private string _name;
    private bool _isActive;
    private int _age;
    private string _gender = "None Selected";

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
        get => _age;
        set { _age = value; OnPropertyChanged(nameof(Age)); }
    }

    public string Gender
    {
        get => _gender;
        set { _gender = value; OnPropertyChanged(nameof(Gender)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MainViewModel // This is our ViewModel class
{
    public ObservableCollection<Person> People { get; set; } // Declare your Person class so you can use it here to add items
    public ObservableCollection<string> Genders { get; } = new ObservableCollection<string> { "Male", "Female" }; // Creates the Genders collection and adds the options

    public MainViewModel()
    {
        People = new ObservableCollection<Person>
        {
            new Person { Name = "George", Age = 30, IsActive = true } // Adds an item with these values. You can duplicate this to add more itesm
        };
    }
}

public sealed partial class MainWindow : Window // This could also be a page. Doesn't really matter.
{
    public MainViewModel ViewModel { get; } = new MainViewModel(); // Declare your ViewModel here so you can use it

    public MainWindow()
    {
        InitializeComponent();
    }

    void OnAutoGeneratingColumns(object sender, TableViewAutoGeneratingColumnEventArgs e)
    {
        if (sender is not TableView tableView) return;

        // Try to get your view model from the TableView's DataContext
        var viewModel = tableView.DataContext as MainViewModel; // replace MainViewModel with your VM type

        // The column that TableView would have used for this property
        if (e.Column is not TableViewBoundColumn originalColumn) return;

        // Replace Gender column with your ComboBox column
        if (e.PropertyName is nameof(Person.Gender))
        {
            e.Column = new TableViewComboBoxColumn
            {
                // Keep the same binding so the column still reads/writes the Gender property
                Binding = originalColumn.Binding,
                Header = originalColumn.Header,

                // Set the source for the options to the Genders collection (so the options for this example will be "male", "female", etc.)
                ItemsSource = viewModel.Genders,
                IsEditable = false
            };
        }
    }
}
```

</details>

---

## FAQs

#### Q: Does TableView automatically use a ComboBox if I use a collection?
A: **No.** TableView will not create a ComboBox column automatically just because you have a collection. You must replace the column in the `AutoGeneratingColumn` event and set the `ItemsSource`.

#### Q: What is a Binding and why do I use it?
A: `Binding` tells the column which property of the row item it should display and edit (for example, `Person.Gender`). `ItemsSource` (for ComboBox columns) is the list of options the user can pick. When replacing a generated column, copy the original column's `Binding` to keep the row-level connection:
```csharp
Binding = originalColumn.Binding
```

Have another question? Please create a post in [**Discussions**](https://github.com/w-ahmad/WinUI.TableView/discussions)

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

---

By following these steps, you can add or customize any supported column type in TableView, whether you want to use simple auto-generated columns or advanced custom columns. You do not need to use the sample's model or code. Just follow the patterns above with your own data classes and collections.
